namespace ManagedMidi.Smf;

public class MidiTrack
{
    public MidiTrack()
        : this(new List<MidiMessage>())
    {
    }

    public MidiTrack(IList<MidiMessage> messages)
    {
        if (messages == null)
            throw new ArgumentNullException("messages");
        Messages = messages as List<MidiMessage> ?? new List<MidiMessage>(messages);
    }

    public IList<MidiMessage> Messages { get; }
}
