namespace ManagedMidi;

public interface IMidiAccess
{
    IEnumerable<IMidiPortDetails> Inputs { get; }
    IEnumerable<IMidiPortDetails> Outputs { get; }

    Task<IMidiInput> OpenInputAsync(string portId);
    Task<IMidiOutput> OpenOutputAsync(string portId);
    MidiAccessExtensionManager ExtensionManager { get; }
}
