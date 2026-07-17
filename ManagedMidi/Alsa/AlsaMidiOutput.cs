using ManagedMidi.AlsaSharp;

namespace ManagedMidi.Alsa;

internal class AlsaMidiOutput : IMidiOutput
{
    private readonly AlsaSequencer seq;
    private readonly AlsaMidiPortDetails port;
    private readonly AlsaMidiPortDetails targetPort;

    internal AlsaMidiOutput(AlsaSequencer seq, AlsaMidiPortDetails appPort, AlsaMidiPortDetails targetPort)
    {
        this.seq = seq;
        port = appPort;
        this.targetPort = targetPort;
    }

    public IMidiPortDetails Details => targetPort;

    public MidiPortConnectionState Connection { get; private set; }

    public Task CloseAsync()
    {
        Dispose();
        return Task.FromResult(string.Empty);
    }

    public void Dispose()
    {
        // unsubscribe the app port from the MIDI output, and then delete the port.
        var q = new AlsaSubscriptionQuery { Type = AlsaSubscriptionQueryType.Read, Index = 0 };
        q.Address.Client = (byte) port.PortInfo.Client;
        q.Address.Port = (byte) port.PortInfo.Port;
        if (seq.QueryPortSubscribers(q))
        {
            seq.DisconnectTo(port.PortInfo.Port, q.Address.Client, q.Address.Port);
        }
        seq.DeleteSimplePort(port.PortInfo.Port);
    }

    public void Send(byte[] mevent, int offset, int length, long timestamp) =>
        seq.Send(port.PortInfo.Port, mevent, offset, length);
}


