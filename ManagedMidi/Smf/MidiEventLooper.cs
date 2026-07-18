namespace ManagedMidi.Smf;

// Event loop implementation.
internal class MidiEventLooper : IDisposable
{
    public MidiEventAction EventReceived;

    public event Action Starting;
    public event Action Finished;
    public event Action PlaybackCompletedToEnd;

    internal double TempoRatio { get; set; } = 1.0;
    internal PlayerState State { get; private set; }
    internal byte[] CurrentTimeSignature { get; private set; } = new byte[4];
    internal int CurrentTempo { get; private set; } = MidiMetaType.DefaultTempo;
    internal int PlayDeltaTime { get; private set; }

    private readonly IMidiPlayerTimeManager timeManager;
    private readonly IList<MidiMessage> messages;
    private readonly int deltaTimeSpec;

    // FIXME (from managed-midi): I prefer ManualResetEventSlim (but it causes some regressions)
    private readonly ManualResetEvent pauseHandle = new ManualResetEvent(false);
    private bool doPause;
    private bool doStop;
    private int eventIdx = 0;

    public MidiEventLooper(IList<MidiMessage> messages, IMidiPlayerTimeManager timeManager, int deltaTimeSpec)
    {
        if (messages == null)
        {
            throw new ArgumentNullException("messages");
        }
        if (deltaTimeSpec < 0)
        {
            throw new NotSupportedException("SMPTe-based delta time is not implemented in this player.");
        }

        this.deltaTimeSpec = deltaTimeSpec;
        this.timeManager = timeManager;

        this.messages = messages;
        State = PlayerState.Stopped;
    }
    public virtual void Dispose()
    {
        if (State != PlayerState.Stopped)
        {
            Stop();
        }
        Mute();
    }

    public void Play()
    {
        pauseHandle.Set();
        State = PlayerState.Playing;
    }

    void Mute()
    {
        for (int i = 0; i < 16; i++)
        {
            OnEvent(new MidiEvent((byte) (i + 0xB0), 0x78, 0, null, 0, 0));
        }
    }

    public void Pause()
    {
        doPause = true;
        Mute();
    }

    public void PlayerLoop()
    {
        Starting?.Invoke();
        eventIdx = 0;
        PlayDeltaTime = 0;
        while (true)
        {
            pauseHandle.WaitOne();
            if (doStop)
            {
                break;
            }
            if (doPause)
            {
                pauseHandle.Reset();
                doPause = false;
                State = PlayerState.Paused;
                continue;
            }
            if (eventIdx == messages.Count)
            {
                break;
            }
            ProcessMessage(messages[eventIdx++]);
        }
        doStop = false;
        Mute();
        State = PlayerState.Stopped;
        if (eventIdx == messages.Count)
        {
            PlaybackCompletedToEnd?.Invoke();
        }
        Finished?.Invoke();
    }

    int GetContextDeltaTimeInMilliseconds(int deltaTime) => (int) (CurrentTempo / 1000 * deltaTime / deltaTimeSpec / TempoRatio);

    void ProcessMessage(MidiMessage m)
    {
        if (seekProcessor != null)
        {
            var result = seekProcessor.FilterMessage(m);
            switch (result)
            {
                case SeekFilterResult.PassAndTerminate:
                case SeekFilterResult.BlockAndTerminate:
                    seekProcessor = null;
                    break;
            }

            switch (result)
            {
                case SeekFilterResult.Block:
                case SeekFilterResult.BlockAndTerminate:
                    return; // ignore this event
            }
        }
        else if (m.DeltaTime != 0)
        {
            var ms = GetContextDeltaTimeInMilliseconds(m.DeltaTime);
            timeManager.WaitBy(ms);
            PlayDeltaTime += m.DeltaTime;
        }

        if (m.Event.StatusByte == 0xFF)
        {
            if (m.Event.Msb == MidiMetaType.Tempo)
            {
                CurrentTempo = MidiMetaType.GetTempo(m.Event.ExtraData, m.Event.ExtraDataOffset);
            }
            else if (m.Event.Msb == MidiMetaType.TimeSignature && m.Event.ExtraDataLength == 4)
            {
                Array.Copy(m.Event.ExtraData, CurrentTimeSignature, 4);
            }
        }

        OnEvent(m.Event);
    }

    void OnEvent(MidiEvent m)
    {
        if (EventReceived != null)
        {
            EventReceived(m);
        }
    }

    public void Stop()
    {
        if (State != PlayerState.Stopped)
        {
            doStop = true;
            pauseHandle?.Set();
            Finished?.Invoke();
        }
    }

    private ISeekProcessor seekProcessor;

    // Note: this previously had an ISeekProcessor parameter, but the argument
    // (from MidiPlayer) was always null.
    internal void Seek(int ticks)
    {
        seekProcessor ??= new SimpleSeekProcessor(ticks);
        eventIdx = 0;
        PlayDeltaTime = ticks;
        Mute();
    }

    // not sure about the interface, so make it non-public yet.
    private interface ISeekProcessor
    {
        SeekFilterResult FilterMessage(MidiMessage message);
    }

    private enum SeekFilterResult
    {
        Pass,
        Block,
        PassAndTerminate,
        BlockAndTerminate,
    }

    private class SimpleSeekProcessor : ISeekProcessor
    {
        private readonly int seekTo;
        private int current;

        public SimpleSeekProcessor(int ticks)
        {
            seekTo = ticks;
        }

        public SeekFilterResult FilterMessage(MidiMessage message)
        {
            current += message.DeltaTime;
            if (current >= seekTo)
            {
                return SeekFilterResult.PassAndTerminate;
            }
            switch (message.Event.EventType)
            {
                case MidiEvent.NoteOn:
                case MidiEvent.NoteOff:
                    return SeekFilterResult.Block;
            }
            return SeekFilterResult.Pass;
        }
    }
}

