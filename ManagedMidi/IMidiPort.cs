namespace ManagedMidi;

// TODO: Implement IAsyncDisposable, if we can do that in netstandard2.0
public interface IMidiPort
{
    IMidiPortDetails Details { get; }
    MidiPortConnectionState Connection { get; }
    Task CloseAsync();
}
