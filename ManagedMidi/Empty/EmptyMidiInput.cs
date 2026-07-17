namespace ManagedMidi.Empty;

internal class EmptyMidiInput : EmptyMidiPort, IMidiInput
{
    public static EmptyMidiInput Instance { get; } = new EmptyMidiInput();

#pragma warning disable 0067
    // will never be fired.
    public event EventHandler<MidiReceivedEventArgs> MessageReceived;
#pragma warning restore 0067

    internal override IMidiPortDetails CreateDetails() =>
        new EmptyMidiPortDetails("dummy_in", "Dummy MIDI Input");
}
