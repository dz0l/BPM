using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrintMaestro.Core.IPC;

public static class WorkerPipeHost
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task RunServerAsync(
        string pipeName,
        Func<WorkerMessage, CancellationToken, Task<WorkerResponse>> handler,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var server = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var request = await ReadMessageAsync<WorkerMessage>(server, cancellationToken).ConfigureAwait(false);
                if (request is null)
                {
                    continue;
                }

                if (request.Command == WorkerCommandType.Shutdown)
                {
                    await WriteMessageAsync(server, WorkerResponse.Ok(), cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (request.Command == WorkerCommandType.Ping)
                {
                    await WriteMessageAsync(server, WorkerResponse.Ok(), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var response = await handler(request, cancellationToken).ConfigureAwait(false);
                await WriteMessageAsync(server, response, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                try
                {
                    await WriteMessageAsync(
                        server,
                        WorkerResponse.Fail("WORKER_ERROR", ex.Message),
                        cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore secondary failures while reporting the primary error.
                }
            }
        }
    }

    public static async Task<WorkerResponse> SendRequestAsync(
        string pipeName,
        WorkerMessage request,
        TimeSpan connectTimeout,
        TimeSpan responseTimeout,
        CancellationToken cancellationToken)
    {
        await using var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectCts.CancelAfter(connectTimeout);

        await client.ConnectAsync(connectCts.Token).ConfigureAwait(false);

        using var responseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        responseCts.CancelAfter(responseTimeout);

        await WriteMessageAsync(client, request, responseCts.Token).ConfigureAwait(false);
        var response = await ReadMessageAsync<WorkerResponse>(client, responseCts.Token).ConfigureAwait(false);

        return response ?? WorkerResponse.Fail("INVALID_RESPONSE", "Worker returned an empty response.");
    }

    private static async Task<T?> ReadMessageAsync<T>(PipeStream stream, CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];
        await ReadExactAsync(stream, lengthBuffer, cancellationToken).ConfigureAwait(false);
        var length = BitConverter.ToInt32(lengthBuffer);

        if (length <= 0 || length > 1024 * 1024)
        {
            throw new InvalidDataException("Invalid IPC payload length.");
        }

        var payload = new byte[length];
        await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(payload, JsonOptions);
    }

    private static async Task WriteMessageAsync<T>(PipeStream stream, T message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        var lengthBuffer = BitConverter.GetBytes(payload.Length);
        await stream.WriteAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;

        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken)
                .ConfigureAwait(false);

            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of IPC stream.");
            }

            offset += read;
        }
    }
}
