using System.Runtime.InteropServices;

namespace ManagedMidi.CoreMidi;

internal class CoreMidiInput : IMidiInput
{
    private readonly CoreMidiPortDetails details;
    private readonly MidiClient client;
    private readonly MidiPort port;
    private byte[] dispatchBytes = new byte[100];

    public CoreMidiInput(CoreMidiPortDetails details)
    {
        this.details = details;
        client = new MidiClient("inputclient");
        port = client.CreateInputPort("inputport");
        port.ConnectSource(details.Endpoint);
        port.MessageReceived += OnMessageReceived;
    }

    public IMidiPortDetails Details => details;

    public MidiPortConnectionState Connection => throw new NotImplementedException();

    public event EventHandler<MidiReceivedEventArgs> MessageReceived;

    void OnMessageReceived(object sender, MidiPacketsEventArgs e)
    {
        if (MessageReceived is null)
        {
            return;
        }
        foreach (var p in e.Packets)
        {
            if (dispatchBytes.Length < p.Length)
            {
                dispatchBytes = new byte[p.Length];
            }
            Marshal.Copy(p.Bytes, dispatchBytes, 0, p.Length);
            MessageReceived(this, new MidiReceivedEventArgs() { Data = dispatchBytes, Start = 0, Length = p.Length, Timestamp = p.TimeStamp });
        }
    }

    public Task CloseAsync()
    {
        port.Disconnect(details.Endpoint);
        port.Dispose();
        client.Dispose();
        details.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => CloseAsync().Wait();
}
