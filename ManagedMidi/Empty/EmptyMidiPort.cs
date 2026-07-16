namespace ManagedMidi.Empty;

internal abstract class EmptyMidiPort : IMidiPort
{
    public IMidiPortDetails Details
    {
        get { return CreateDetails(); }
    }
    internal abstract IMidiPortDetails CreateDetails();

    public MidiPortConnectionState Connection { get; private set; }

    public Task CloseAsync() => Task.CompletedTask;

    public void Dispose()
    {
    }
}
