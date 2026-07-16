namespace ManagedMidi;

public interface IMidiPort
{
    IMidiPortDetails Details { get; }
    MidiPortConnectionState Connection { get; }
    Task CloseAsync();
}
