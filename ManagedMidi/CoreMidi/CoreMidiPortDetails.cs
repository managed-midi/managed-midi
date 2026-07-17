namespace ManagedMidi.CoreMidi;

internal class CoreMidiPortDetails : IMidiPortDetails, IDisposable
{
    public CoreMidiPortDetails(MidiEndpoint src)
    {
        Endpoint = src;
        Id = src.Name + "__" + src.EndpointName;
        Manufacturer = src.Manufacturer;
        Name = string.IsNullOrEmpty(src.DisplayName) ? src.Name : src.DisplayName;

        try
        {
            Version = src.DriverVersion.ToString();
        }
        catch
        {
            Version = "N/A";
        }
    }

    public MidiEndpoint Endpoint { get; }
    public string Id { get; }
    public string Manufacturer { get; }
    public string Name { get; }
    public string Version { get; }

    public void Dispose() => Endpoint.Dispose();
}
