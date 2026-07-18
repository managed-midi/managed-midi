namespace ManagedMidi;

public readonly struct MidiMessage
{
    public readonly int DeltaTime;
    public readonly MidiEvent Event;

    public MidiMessage(int deltaTime, MidiEvent evt)
    {
        DeltaTime = deltaTime;
        Event = evt;
    }

    public override string ToString() => $"[dt{DeltaTime}]{Event}";
}
