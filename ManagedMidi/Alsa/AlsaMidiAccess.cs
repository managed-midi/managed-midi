using ManagedMidi.AlsaSharp;

namespace ManagedMidi.Alsa;

internal class AlsaMidiAccess : IMidiAccess
{
    internal class AlsaMidiAccessExtensionManager : MidiAccessExtensionManager
    {
        private readonly AlsaMidiPortCreatorExtension portCreator;

        public AlsaMidiAccessExtensionManager(AlsaMidiAccess access)
        {
            Access = access;
            portCreator = new AlsaMidiPortCreatorExtension(this);
        }

        public AlsaMidiAccess Access { get; private set; }

        public override T GetInstance<T>()
        {
            if (typeof(T) == typeof(MidiPortCreatorExtension))
                return (T) (object) portCreator;
            return null;
        }
    }

    internal class AlsaMidiPortCreatorExtension : MidiPortCreatorExtension
    {
        private readonly AlsaMidiAccessExtensionManager manager;

        public AlsaMidiPortCreatorExtension(AlsaMidiAccessExtensionManager extensionManager)
        {
            manager = extensionManager;
        }

        public override IMidiOutput CreateVirtualInputSender(PortCreatorContext context)
        {
            var seq = new AlsaSequencer(AlsaIOType.Duplex, AlsaIOMode.NonBlocking);
            var portNumber = seq.CreateSimplePort(
                context.PortName ?? "managed-midi virtual in",
                AlsaMidiAccess.VirtualInputConnectedCap,
                AlsaMidiAccess.midi_port_type);
            seq.SetClientName(context.ApplicationName ?? "managed-midi input port creator");
            var port = seq.GetPort(seq.CurrentClientId, portNumber);
            var details = new AlsaMidiPortDetails(port);
            SendDelegate send = (buffer, start, length, timestamp) =>
                seq.Send(portNumber, buffer, start, length);
            return new SimpleVirtualMidiOutput(details, () => seq.DeleteSimplePort(portNumber)) { OnSend = send };
        }

        public override IMidiInput CreateVirtualOutputReceiver(PortCreatorContext context)
        {
            var seq = new AlsaSequencer(AlsaIOType.Duplex, AlsaIOMode.NonBlocking);
            var portNumber = seq.CreateSimplePort(
                context.PortName ?? "managed-midi virtual out",
                AlsaMidiAccess.VirtualOutputConnectedCap,
                AlsaMidiAccess.midi_port_type);
            seq.SetClientName(context.ApplicationName ?? "managed-midi output port creator");
            var port = seq.GetPort(seq.CurrentClientId, portNumber);
            var details = new AlsaMidiPortDetails(port);
            return new SimpleVirtualMidiInput(details, () => seq.DeleteSimplePort(portNumber));
        }
    }

    private const AlsaPortType midi_port_type = AlsaPortType.MidiGeneric | AlsaPortType.Application;

    private readonly AlsaSequencer systemWatcher;

    public AlsaMidiAccess()
    {
        ExtensionManager = new AlsaMidiAccessExtensionManager(this);
        systemWatcher = new AlsaSequencer(AlsaIOType.Duplex, AlsaIOMode.NonBlocking);
    }

    private const AlsaPortCapabilities InputRequirements = AlsaPortCapabilities.Read | AlsaPortCapabilities.SubsRead;
    private const AlsaPortCapabilities OutputRequirements = AlsaPortCapabilities.Write | AlsaPortCapabilities.SubsWrite;
    private const AlsaPortCapabilities OutputConnectedCap = AlsaPortCapabilities.Read | AlsaPortCapabilities.NoExport;
    private const AlsaPortCapabilities InputConnectedCap = AlsaPortCapabilities.Write | AlsaPortCapabilities.NoExport;
    private const AlsaPortCapabilities VirtualOutputConnectedCap = AlsaPortCapabilities.Write | AlsaPortCapabilities.SubsWrite;
    private const AlsaPortCapabilities VirtualInputConnectedCap = AlsaPortCapabilities.Read | AlsaPortCapabilities.SubsRead;

    public MidiAccessExtensionManager ExtensionManager { get; private set; }

    private IEnumerable<AlsaPortInfo> EnumerateMatchingPorts(AlsaSequencer seq, AlsaPortCapabilities cap)
    {
        var cinfo = new AlsaClientInfo { Client = -1 };
        while (seq.QueryNextClient(cinfo))
        {
            var pinfo = new AlsaPortInfo { Client = cinfo.Client, Port = -1 };
            while (seq.QueryNextPort(pinfo))
            {
                if ((pinfo.PortType & midi_port_type) != 0 &&
                    (pinfo.Capabilities & cap) == cap)
                {
                    yield return pinfo.Clone();
                }
            }
        }
    }

    private IEnumerable<AlsaPortInfo> EnumerateAvailableInputPorts() => EnumerateMatchingPorts(systemWatcher, InputRequirements);

    private IEnumerable<AlsaPortInfo> EnumerateAvailableOutputPorts() => EnumerateMatchingPorts(systemWatcher, OutputRequirements);

    // [input device port] --> [RETURNED PORT] --> app handles messages
    private AlsaPortInfo CreateInputConnectedPort(AlsaSequencer seq, AlsaPortInfo pinfo, string portName = "alsa-sharp input")
    {
        var portId = seq.CreateSimplePort(portName, InputConnectedCap, midi_port_type);
        var sub = new AlsaPortSubscription();
        sub.Destination.Client = (byte) seq.CurrentClientId;
        sub.Destination.Port = (byte) portId;
        sub.Sender.Client = (byte) pinfo.Client;
        sub.Sender.Port = (byte) pinfo.Port;
        seq.SubscribePort(sub);
        return seq.GetPort(sub.Destination.Client, sub.Destination.Port);
    }

    // app generates messages --> [RETURNED PORT] --> [output device port]
    private AlsaPortInfo CreateOutputConnectedPort(AlsaSequencer seq, AlsaPortInfo pinfo, string portName = "alsa-sharp output")
    {
        var portId = seq.CreateSimplePort(portName, OutputConnectedCap, midi_port_type);
        var sub = new AlsaPortSubscription
        {
            Sender =
            {
                Client = (byte) seq.CurrentClientId,
                Port = (byte) portId
            },
            Destination =
            {
                Client = (byte) seq.CurrentClientId,
                Port = (byte) portId
            }
        };
        seq.SubscribePort(sub);
        return seq.GetPort(sub.Sender.Client, sub.Sender.Port);
    }

    public IEnumerable<IMidiPortDetails> Inputs => EnumerateAvailableInputPorts().Select(p => new AlsaMidiPortDetails(p));

    public IEnumerable<IMidiPortDetails> Outputs => EnumerateAvailableOutputPorts().Select(p => new AlsaMidiPortDetails(p));

    public Task<IMidiInput> OpenInputAsync(string portId)
    {
        var sourcePort = (AlsaMidiPortDetails) Inputs.FirstOrDefault(p => p.Id == portId);
        if (sourcePort == null)
            throw new ArgumentException($"Port '{portId}' does not exist.");
        var seq = new AlsaSequencer(AlsaIOType.Input, AlsaIOMode.NonBlocking);
        var appPort = CreateInputConnectedPort(seq, sourcePort.PortInfo);
        return Task.FromResult<IMidiInput>(new AlsaMidiInput(seq, new AlsaMidiPortDetails(appPort), sourcePort));
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        var destPort = (AlsaMidiPortDetails) Outputs.FirstOrDefault(p => p.Id == portId);
        if (destPort == null)
            throw new ArgumentException($"Port '{portId}' does not exist.");
        var seq = new AlsaSequencer(AlsaIOType.Output, AlsaIOMode.None);
        var appPort = CreateOutputConnectedPort(seq, destPort.PortInfo);
        return Task.FromResult<IMidiOutput>(new AlsaMidiOutput(seq, new AlsaMidiPortDetails(appPort), destPort));
    }
}
