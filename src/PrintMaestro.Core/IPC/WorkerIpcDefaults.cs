namespace PrintMaestro.Core.IPC;

public static class WorkerIpcDefaults
{
    public static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(15);

    public static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
}
