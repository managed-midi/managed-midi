namespace ManagedMidi.Smf;

public class MidiMusic
{
    public IList<MidiTrack> Tracks { get; } = new List<MidiTrack>();
    public short DeltaTimeSpec { get; set; }
    public byte Format { get; set; }

    public MidiMusic()
    {
        Format = 1;
    }

    public static MidiMusic Read(Stream stream) => SmfReader.ReadMusic(stream);

    public void WriteTo(Stream stream) => new SmfWriter(stream).WriteMusic(this);

    public void AddTrack(MidiTrack track) => Tracks.Add(track);


    public IEnumerable<MidiMessage> GetMetaEventsOfType(byte metaType) =>
        Format != 0
        ? SmfTrackMerger.Merge(this).GetMetaEventsOfType(metaType)
        : GetMetaEventsOfType(Tracks[0].Messages, metaType);

    public static IEnumerable<MidiMessage> GetMetaEventsOfType(IEnumerable<MidiMessage> messages, byte metaType)
    {
        int v = 0;
        foreach (var m in messages)
        {
            v += m.DeltaTime;
            if (m.Event.EventType == MidiEvent.Meta && m.Event.Msb == metaType)
            {
                yield return new MidiMessage(v, m.Event);
            }
        }
    }

    public int GetTotalTicks() =>
        Format != 0
        ? SmfTrackMerger.Merge(this).GetTotalTicks()
        : Tracks[0].Messages.Sum(m => m.DeltaTime);

    public int GetTotalPlayTimeMilliseconds() =>
        Format != 0
        ? SmfTrackMerger.Merge(this).GetTotalPlayTimeMilliseconds()
        : GetTotalPlayTimeMilliseconds(Tracks[0].Messages, DeltaTimeSpec);

    public int GetTimePositionInMillisecondsForTick(int ticks) =>
        Format != 0
        ? SmfTrackMerger.Merge(this).GetTimePositionInMillisecondsForTick(ticks)
        : GetPlayTimeMillisecondsAtTick(Tracks[0].Messages, ticks, DeltaTimeSpec);

    public static int GetTotalPlayTimeMilliseconds(IList<MidiMessage> messages, int deltaTimeSpec) =>
        GetPlayTimeMillisecondsAtTick(messages, messages.Sum(m => m.DeltaTime), deltaTimeSpec);

    public static int GetPlayTimeMillisecondsAtTick(IList<MidiMessage> messages, int ticks, int deltaTimeSpec)
    {
        if (deltaTimeSpec < 0)
        {
            throw new NotSupportedException("non-tick based DeltaTime");
        }
        int tempo = MidiMetaType.DefaultTempo;
        int t = 0;
        double v = 0;
        foreach (var m in messages)
        {
            var deltaTime = t + m.DeltaTime < ticks ? m.DeltaTime : ticks - t;
            v += (double) tempo / 1000 * deltaTime / deltaTimeSpec;
            if (deltaTime != m.DeltaTime)
            {
                break;
            }
            t += m.DeltaTime;
            if (m.Event.EventType == MidiEvent.Meta && m.Event.Msb == MidiMetaType.Tempo)
            {
                tempo = MidiMetaType.GetTempo(m.Event.ExtraData, m.Event.ExtraDataOffset);
            }
        }
        return (int) v;
    }
}
