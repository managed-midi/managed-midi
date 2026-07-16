namespace ManagedMidi.WinMM;

class WinMMPortDetails : IMidiPortDetails
{
    public WinMMPortDetails(uint deviceId, string name, int version)
    {
        Id = deviceId.ToString();
        Name = name;
        Version = version.ToString();
    }

    public string Id { get; private set; }

    public string Manufacturer { get; private set; }

    public string Name { get; private set; }

    public string Version { get; private set; }
}
