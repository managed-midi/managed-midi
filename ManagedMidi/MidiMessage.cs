namespace ManagedMidi;

public struct MidiMessage
{
    public MidiMessage(int deltaTime, MidiEvent evt)
    {
        DeltaTime = deltaTime;
        Event = evt;
    }

    public readonly int DeltaTime;
    public readonly MidiEvent Event;

    public override string ToString()
    {
        return string.Format("[dt{0}]{1}", DeltaTime, Event);
    }
}
