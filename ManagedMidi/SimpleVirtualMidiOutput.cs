namespace ManagedMidi;

public class SimpleVirtualMidiOutput : SimpleVirtualMidiPort, IMidiOutput
{
    public SimpleVirtualMidiOutput(IMidiPortDetails details, Action onDispose)
    : base(details, onDispose)
    {
    }

    public MidiPortCreatorExtension.SendDelegate OnSend { get; set; }

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        if (OnSend != null)
            OnSend(mevent, offset, length, timestamp);
    }
}
