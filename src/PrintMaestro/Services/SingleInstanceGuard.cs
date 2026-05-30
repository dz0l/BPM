namespace PrintMaestro.Services;

public static class SingleInstanceGuard
{
    private const string MutexName = @"Global\dz0l.PrintMaestro.SingleInstance";

    private static Mutex? _mutex;

    public static bool TryAcquire()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        return createdNew;
    }

    public static void Release()
    {
        if (_mutex is null)
        {
            return;
        }

        try
        {
            _mutex.ReleaseMutex();
        }
        catch
        {
            // Ignore if already released.
        }

        _mutex.Dispose();
        _mutex = null;
    }
}
