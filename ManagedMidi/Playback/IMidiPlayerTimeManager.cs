namespace ManagedMidi.Playback;

/// <summary>
/// Used by MidiPlayer to manage time progress.
/// </summary>
public interface IMidiPlayerTimeManager
{
    void WaitBy(int addedMilliseconds);
}
