namespace ManagedMidi;

public class SimpleVirtualMidiInput : SimpleVirtualMidiPort, IMidiInput
{
    public SimpleVirtualMidiInput(IMidiPortDetails details, Action onDispose)
        : base(details, onDispose)
    {
    }

    event EventHandler<MidiReceivedEventArgs> IMidiInput.MessageReceived
    {
        add { }
        remove { }
    }
}
