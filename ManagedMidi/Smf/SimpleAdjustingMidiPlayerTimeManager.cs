namespace ManagedMidi.Smf;

public class SimpleAdjustingMidiPlayerTimeManager : IMidiPlayerTimeManager
{
    private DateTime lastStarted = default;
    private long nominalTotalMillis = 0;

    public void WaitBy(int addedMilliseconds)
    {
        if (addedMilliseconds > 0)
        {
            long delta = addedMilliseconds;
            if (lastStarted != default)
            {
                var actualTotalMillis = (long) (DateTime.Now - lastStarted).TotalMilliseconds;
                delta -= actualTotalMillis - nominalTotalMillis;
            }
            else
            {
                lastStarted = DateTime.Now;
            }
            if (delta > 0)
            {
                var t = Task.Delay((int) delta);
                t.Wait();
            }
            nominalTotalMillis += addedMilliseconds;
        }
    }
}
