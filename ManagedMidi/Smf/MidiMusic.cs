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

    // Note: the methods below which call GetMergedMessages() are cheap when the format is 0,
    // GetMergedMessages() jjust returns "this".
    public IEnumerable<MidiMessage> GetMetaEventsOfType(byte metaType) => GetMetaEventsOfType(GetMergedMessages().Tracks[0].Messages, metaType);

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

    public int GetTotalTicks() => GetMergedMessages().Tracks[0].Messages.Sum(m => m.DeltaTime);
    public int GetTotalPlayTimeMilliseconds() => GetTotalPlayTimeMilliseconds(GetMergedMessages().Tracks[0].Messages, DeltaTimeSpec);
    public int GetTimePositionInMillisecondsForTick(int ticks) => GetPlayTimeMillisecondsAtTick(GetMergedMessages().Tracks[0].Messages, ticks, DeltaTimeSpec);

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

    internal MidiMusic GetMergedMessages()
    {
        if (Format == 0)
        {
            return this;
        }

        IList<MidiMessage> l = new List<MidiMessage>();

        foreach (var track in Tracks)
        {
            int delta = 0;
            foreach (var mev in track.Messages)
            {
                delta += mev.DeltaTime;
                l.Add(new MidiMessage(delta, mev.Event));
            }
        }

        if (l.Count == 0)
        {
            return new MidiMusic() { DeltaTimeSpec = DeltaTimeSpec }; // empty (why did you need to sort your song file?)
        }

        // Usual Sort() over simple list of MIDI events does not work as expected.
        // For example, it does not always preserve event 
        // orders on the same channels when the delta time
        // of event B after event A is 0. It could be sorted
        // either as A->B or B->A.
        //
        // To resolve this issue, we have to sort "chunk"
        // of events, not all single events themselves, so
        // that order of events in the same chunk is preserved
        // i.e. [AB] at 48 and [CDE] at 0 should be sorted as
        // [CDE] [AB].

        var idxl = new List<int>(l.Count);
        idxl.Add(0);
        int prev = 0;
        for (int i = 0; i < l.Count; i++)
        {
            if (l[i].DeltaTime != prev)
            {
                idxl.Add(i);
                prev = l[i].DeltaTime;
            }
        }

        idxl.Sort(delegate (int i1, int i2)
        {
            return l[i1].DeltaTime - l[i2].DeltaTime;
        });

        // now build a new event list based on the sorted blocks.
        var l2 = new List<MidiMessage>(l.Count);
        int idx;
        for (int i = 0; i < idxl.Count; i++)
        {
            for (idx = idxl[i], prev = l[idx].DeltaTime; idx < l.Count && l[idx].DeltaTime == prev; idx++)
            {
                l2.Add(l[idx]);
            }
        }
        l = l2;

        // now messages should be sorted correctly.

        var waitToNext = l[0].DeltaTime;
        for (int i = 0; i < l.Count - 1; i++)
        {
            if (l[i].Event.Value != 0)
            { // if non-dummy
                var tmp = l[i + 1].DeltaTime - l[i].DeltaTime;
                l[i] = new MidiMessage(waitToNext, l[i].Event);
                waitToNext = tmp;
            }
        }
        l[l.Count - 1] = new MidiMessage(waitToNext, l[l.Count - 1].Event);

        return new MidiMusic
        {
            DeltaTimeSpec = DeltaTimeSpec,
            Format = 0,
            Tracks = { new MidiTrack(l) }
        };
    }
}
