namespace ManagedMidi.WinMM;

class WinMMPortDetails : IMidiPortDetails
{
    public string Id { get; }
    public string Manufacturer { get; }
    public string Name { get; }
    public string Version { get; }

    public WinMMPortDetails(uint deviceId, string name, int version)
    {
        Id = deviceId.ToString();
        Name = name;
        Version = version.ToString();
    }
}
