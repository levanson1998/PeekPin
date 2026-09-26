namespace PeekPin;

public sealed class SingleInstance : IDisposable
{
    public const string MutexName = @"Local\PeekPin";
    private readonly Mutex _mutex;
    private readonly bool _owned;

    private SingleInstance(Mutex mutex, bool owned)
    {
        _mutex = mutex;
        _owned = owned;
    }

    public bool Acquired => _owned;

    public static SingleInstance Acquire()
    {
        var mutex = new Mutex(true, MutexName, out var created);
        return new SingleInstance(mutex, created);
    }

    public void Dispose()
    {
        if (_owned)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
