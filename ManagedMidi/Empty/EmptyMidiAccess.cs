namespace ManagedMidi.Empty;

internal class EmptyMidiAccess : IMidiAccess
{
    public IEnumerable<IMidiPortDetails> Inputs
    {
        get { yield return EmptyMidiInput.Instance.Details; }
    }

    public IEnumerable<IMidiPortDetails> Outputs
    {
        get { yield return EmptyMidiOutput.Instance.Details; }
    }

    public MidiAccessExtensionManager ExtensionManager { get; } = new();

    public Task<IMidiInput> OpenInputAsync(string portId)
    {
        if (portId != EmptyMidiInput.Instance.Details.Id)
            throw new ArgumentException(string.Format("Port ID {0} does not exist.", portId));
        return Task.FromResult<IMidiInput>(EmptyMidiInput.Instance);
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        if (portId != EmptyMidiOutput.Instance.Details.Id)
            throw new ArgumentException(string.Format("Port ID {0} does not exist.", portId));
        return Task.FromResult<IMidiOutput>(EmptyMidiOutput.Instance);
    }
}
