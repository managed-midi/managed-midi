using ManagedMidi.AlsaSharp;

namespace ManagedMidi.Alsa;

internal class AlsaMidiPortDetails : IMidiPortDetails
{
    AlsaPortInfo port;

    internal AlsaMidiPortDetails(AlsaPortInfo port)
    {
        this.port = port;
    }

    internal AlsaPortInfo PortInfo => port;

    public string Id => port.Id;

    public string Manufacturer => port.Manufacturer;

    public string Name => port.Name;

    public string Version => port.Version;
}


