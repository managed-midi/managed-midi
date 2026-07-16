namespace ManagedMidi;

public interface IMidiPortDetails
{
    string Id { get; }
    string Manufacturer { get; }
    string Name { get; }
    string Version { get; }
}
