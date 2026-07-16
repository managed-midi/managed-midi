namespace ManagedMidi;

public interface IMidiInput : IMidiPort, IDisposable
{
    event EventHandler<MidiReceivedEventArgs> MessageReceived;
}
