namespace ManagedMidi.Empty;

internal class EmptyMidiOutput : EmptyMidiPort, IMidiOutput
{
    public static EmptyMidiOutput Instance { get; } = new EmptyMidiOutput();

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        // do nothing.
    }

    internal override IMidiPortDetails CreateDetails()
    {
        return new EmptyMidiPortDetails("dummy_out", "Dummy MIDI Output");
    }
}