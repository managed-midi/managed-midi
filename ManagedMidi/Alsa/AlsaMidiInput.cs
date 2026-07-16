using ManagedMidi.AlsaSharp;

namespace ManagedMidi.Alsa;

internal class AlsaMidiInput : IMidiInput
{
    AlsaSequencer seq;
    AlsaMidiPortDetails port, source_port;

    internal AlsaMidiInput(AlsaSequencer seq, AlsaMidiPortDetails appPort, AlsaMidiPortDetails sourcePort)
    {
        this.seq = seq;
        port = appPort;
        source_port = sourcePort;
        byte[] buffer = new byte[0x200];
        seq.StartListening(port.PortInfo.Port, buffer, (buf, start, len) =>
        {
            var args = new MidiReceivedEventArgs() { Data = buf, Start = start, Length = len, Timestamp = 0 };
            MessageReceived(this, args);
        });
    }

    public IMidiPortDetails Details => source_port;

    public MidiPortConnectionState Connection { get; private set; }

    public event EventHandler<MidiReceivedEventArgs> MessageReceived;

    public Task CloseAsync()
    {
        Dispose();
        return Task.FromResult(string.Empty);
    }

    public void Dispose()
    {
        // unsubscribe the app port from the MIDI input, and then delete the port.
        var q = new AlsaSubscriptionQuery { Type = AlsaSubscriptionQueryType.Write, Index = 0 };
        q.Address.Client = (byte) port.PortInfo.Client;
        q.Address.Port = (byte) port.PortInfo.Port;
        if (seq.QueryPortSubscribers(q))
            seq.DisconnectTo(port.PortInfo.Port, q.Address.Client, q.Address.Port);
        seq.DeleteSimplePort(port.PortInfo.Port);
    }
}


