namespace ManagedMidi.Smf;

// TODO: Refactor this to a single static method (or similar).
internal class SmfTrackSplitter
{
    private readonly IList<MidiMessage> source;
    private readonly short deltaTimeSpec;
    private readonly Dictionary<int, SplitTrack> tracks = new Dictionary<int, SplitTrack>();

    private SmfTrackSplitter(IList<MidiMessage> source, short deltaTimeSpec)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }
        this.source = source;
        this.deltaTimeSpec = deltaTimeSpec;
        var mtr = new SplitTrack(-1);
        tracks.Add(-1, mtr);
    }

    public static MidiMusic Split(IList<MidiMessage> source, short deltaTimeSpec) =>
        new SmfTrackSplitter(source, deltaTimeSpec).Split();

    private class SplitTrack
    {
        public int TrackID { get; }
        public int TotalDeltaTime { get; private set; }
        public MidiTrack Track { get; }

        public SplitTrack(int trackID)
        {
            TrackID = trackID;
            Track = new MidiTrack();
        }

        public void AddMessage(int deltaInsertAt, MidiMessage e)
        {
            e = new MidiMessage(deltaInsertAt - TotalDeltaTime, e.Event);
            Track.Messages.Add(e);
            TotalDeltaTime = deltaInsertAt;
        }
    }

    private SplitTrack GetTrack(int track)
    {
        SplitTrack t;
        if (!tracks.TryGetValue(track, out t))
        {
            t = new SplitTrack(track);
            tracks[track] = t;
        }
        return t;
    }

    // Override it to customize track dispatcher. It would be
    // useful to split note messages out from non-note ones,
    // to ease data reading.
    public virtual int GetTrackID(MidiMessage e)
    {
        switch (e.Event.EventType)
        {
            case MidiEvent.Meta:
            case MidiEvent.SysEx1:
            case MidiEvent.SysEx2:
                return -1;
            default:
                return e.Event.Channel;
        }
    }

    public MidiMusic Split()
    {
        int totalDeltaTime = 0;
        foreach (var e in source)
        {
            totalDeltaTime += e.DeltaTime;
            int id = GetTrackID(e);
            GetTrack(id).AddMessage(totalDeltaTime, e);
        }

        var m = new MidiMusic
        {
            DeltaTimeSpec = deltaTimeSpec
        };
        foreach (var t in tracks.Values)
        {
            m.Tracks.Add(t.Track);
        }
        return m;
    }
}
