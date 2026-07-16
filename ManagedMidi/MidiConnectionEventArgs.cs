namespace ManagedMidi;

public class MidiConnectionEventArgs : EventArgs
{
    public IMidiPortDetails Port { get; private set; }
}
