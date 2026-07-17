namespace ManagedMidi.CoreMidi;

internal class CoreMidiOutput : IMidiOutput
{
    private readonly MidiClient client;
    private readonly CoreMidiPortDetails details;
    private readonly MidiPort port;
    private readonly MidiPacket[] sendBuffer = new MidiPacket[1];

    public CoreMidiOutput(CoreMidiPortDetails details)
    {
        this.details = details;
        client = new MidiClient("outputclient");
        port = client.CreateOutputPort("outputport");
        Connection = MidiPortConnectionState.Open;
    }

    public IMidiPortDetails Details => details;

    public MidiPortConnectionState Connection { get; private set; }

    public Task CloseAsync()
    {
        port.Disconnect(details.Endpoint);
        port.Dispose();
        client.Dispose();
        details.Dispose();
        Connection = MidiPortConnectionState.Closed;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        CloseAsync().Wait();
    }

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        unsafe
        {
            fixed (byte* ptr = mevent)
            {
                sendBuffer[0] = new MidiPacket(timestamp, (ushort) length, (IntPtr) (ptr + offset));
                port.Send(details.Endpoint, sendBuffer);
            }
        }
    }
}
