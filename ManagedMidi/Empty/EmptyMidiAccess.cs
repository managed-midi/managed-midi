namespace ManagedMidi.Empty;

internal class EmptyMidiAccess : IMidiAccess
{
    public IEnumerable<IMidiPortDetails> Inputs => [EmptyMidiInput.Instance.Details];
    public IEnumerable<IMidiPortDetails> Outputs => [EmptyMidiOutput.Instance.Details];
    public MidiAccessExtensionManager ExtensionManager { get; } = new();

    public Task<IMidiInput> OpenInputAsync(string portId)
    {
        if (portId != EmptyMidiInput.Instance.Details.Id)
        {
            throw new ArgumentException($"Port ID {portId} does not exist.", nameof(portId));
        }
        return Task.FromResult<IMidiInput>(EmptyMidiInput.Instance);
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        if (portId != EmptyMidiOutput.Instance.Details.Id)
        {
            throw new ArgumentException($"Port ID {portId} does not exist.", nameof(portId));
        }
        return Task.FromResult<IMidiOutput>(EmptyMidiOutput.Instance);
    }
}
