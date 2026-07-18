namespace ManagedMidi;

public class VirtualMidiPlayerTimeManager : IMidiPlayerTimeManager, IDisposable
{
    private AutoResetEvent waitHandle = new AutoResetEvent(false);
    private long totalWaitedMillis, totalProceededMillis;
    private bool shouldTerminate, disposed;

    public void Dispose() => Abort();

    public void Abort()
    {
        if (disposed)
        {
            return;
        }
        shouldTerminate = true;
        waitHandle.Set();
        waitHandle.Dispose();
        disposed = true;
    }

    public virtual void WaitBy(int addedMilliseconds)
    {
        while (!shouldTerminate && totalWaitedMillis + addedMilliseconds > totalProceededMillis)
        {
            waitHandle.WaitOne();
        }
        totalWaitedMillis += addedMilliseconds;
    }

    public virtual void ProceedBy(int addedMilliseconds)
    {
        if (addedMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(addedMilliseconds), "Argument must be non-negative integer");
        }
        totalProceededMillis += addedMilliseconds;
        waitHandle.Set();
    }
}
