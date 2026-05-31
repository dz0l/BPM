namespace PrintMaestro.Core.Threading;

public static class StaThreadRunner
{
    public static T Run<T>(Func<T> func, CancellationToken cancellationToken = default, bool useBackgroundThread = true)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            return func();
        }

        T? result = default;
        Exception? error = null;
        using var completed = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            try
            {
                result = func();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                completed.Set();
            }
        })
        {
            IsBackground = useBackgroundThread
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        while (!completed.Wait(100))
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (error is not null)
        {
            throw error;
        }

        return result!;
    }

    public static void Run(Action action, CancellationToken cancellationToken = default, bool useBackgroundThread = true) =>
        Run<object?>(() =>
        {
            action();
            return null;
        }, cancellationToken, useBackgroundThread);
}
