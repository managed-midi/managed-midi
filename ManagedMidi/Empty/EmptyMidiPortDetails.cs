namespace ManagedMidi.Empty;

internal class EmptyMidiPortDetails : IMidiPortDetails
{
    public EmptyMidiPortDetails(string id, string name)
    {
        Id = id;
        Manufacturer = "dummy project";
        Name = name;
        Version = "0.0";
    }

    public string Id { get; }
    public string Manufacturer { get; }
    public string Name { get; }
    public string Version { get; }
}
