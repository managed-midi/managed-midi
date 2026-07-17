namespace ManagedMidi.CoreMidi;

internal class CoreMidiAccess : IMidiAccess
{
    private class CoreMidiAccessExtensionManager : MidiAccessExtensionManager
    {
        private readonly CoreMidiPortCreatorExtension portCreator = new CoreMidiPortCreatorExtension();
        public override T GetInstance<T>()
        {
            if (typeof(T) == typeof(MidiPortCreatorExtension))
            {
                return (T) (object) portCreator;
            }
            return null;
        }
    }

    private class CoreMidiPortCreatorExtension : MidiPortCreatorExtension
    {
        public override IMidiOutput CreateVirtualInputSender(PortCreatorContext context)
        {
            var nclient = new MidiClient(context.ApplicationName ?? "managed-midi virtual in");
            MidiError error;
            var portName = context.PortName ?? "managed-midi virtual in port";
            var nendpoint = nclient.CreateVirtualSource(portName, out error);
            nendpoint.Manufacturer = context.Manufacturer;
            nendpoint.DisplayName = portName;
            nendpoint.Name = portName;
            var details = new CoreMidiPortDetails(nendpoint);
            return new CoreMidiOutput(details);
        }

        public override IMidiInput CreateVirtualOutputReceiver(PortCreatorContext context)
        {
            var nclient = new MidiClient(context.ApplicationName ?? "managed-midi virtual out");
            MidiError error;
            var portName = context.PortName ?? "managed-midi virtual out port";
            var nendpoint = nclient.CreateVirtualDestination(portName, out error);
            nendpoint.Manufacturer = context.Manufacturer;
            nendpoint.DisplayName = portName;
            nendpoint.Name = portName;
            var details = new CoreMidiPortDetails(nendpoint);
            return new CoreMidiInput(details);
        }
    }

    public MidiAccessExtensionManager ExtensionManager { get; } = new CoreMidiAccessExtensionManager();

    public IEnumerable<IMidiPortDetails> Inputs => Enumerable.Range(0, (int) Midi.SourceCount).Select(i => new CoreMidiPortDetails(MidiEndpoint.GetSource(i)));

    public IEnumerable<IMidiPortDetails> Outputs => Enumerable.Range(0, (int) Midi.DestinationCount).Select(i => new CoreMidiPortDetails(MidiEndpoint.GetDestination(i)));

    public Task<IMidiInput> OpenInputAsync(string portId)
    {
        var details = Inputs.Cast<CoreMidiPortDetails>().FirstOrDefault(i => i.Id == portId);
        if (details == null)
        {
            throw new InvalidOperationException($"Device specified as port '{portId}' is not found.");
        }
        return Task.FromResult((IMidiInput) new CoreMidiInput(details));
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        var details = Outputs.Cast<CoreMidiPortDetails>().FirstOrDefault(i => i.Id == portId);
        if (details == null)
        {
            throw new InvalidOperationException($"Device specified as port '{portId}' is not found.");
        }
        return Task.FromResult((IMidiOutput) new CoreMidiOutput(details));
    }
}
