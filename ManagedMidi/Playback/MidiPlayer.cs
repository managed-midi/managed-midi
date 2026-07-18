using ManagedMidi.Smf;

namespace ManagedMidi.Playback;

// Provides asynchronous player control.
public class MidiPlayer : IDisposable
{
    private MidiEventLooper looper;
    // FIXME: it is still awkward to have it here. Move it into MidiEventLooper.
    private Task syncPlayerTask;
    private IMidiOutput output;
    private IList<MidiMessage> messages;
    private MidiMusic music;

    private bool shouldDisposeOutput;
    private byte[] buffer = new byte[0x100];
    private bool[] channelMask;

    public event Action Finished
    {
        add { looper.Finished += value; }
        remove { looper.Finished -= value; }
    }

    public event Action PlaybackCompletedToEnd
    {
        add { looper.PlaybackCompletedToEnd += value; }
        remove { looper.PlaybackCompletedToEnd -= value; }
    }

    public event MidiEventAction EventReceived
    {
        add { looper.EventReceived += value; }
        remove { looper.EventReceived -= value; }
    }

    public PlayerState State => looper.State;

    public double TempoChangeRatio
    {
        get => looper.TempoRatio;
        set => looper.TempoRatio = value;
    }

    public int Tempo => looper.CurrentTempo;
    public int Bpm => (int) (60.0 / Tempo * 1000000.0);

    // You can break the data at your own risk but I take performance precedence.
    public byte[] TimeSignature => looper.CurrentTimeSignature;

    public int PlayDeltaTime => looper.PlayDeltaTime;

    public TimeSpan PositionInTime => TimeSpan.FromMilliseconds(music.GetTimePositionInMillisecondsForTick(PlayDeltaTime));

    public MidiPlayer(MidiMusic music)
        : this(music, MidiAccessManager.Empty)
    {
    }

    public MidiPlayer(MidiMusic music, IMidiAccess access)
        : this(music, access, new SimpleAdjustingMidiPlayerTimeManager())
    {
    }

    public MidiPlayer(MidiMusic music, IMidiOutput output)
        : this(music, output, new SimpleAdjustingMidiPlayerTimeManager())
    {
    }

    public MidiPlayer(MidiMusic music, IMidiPlayerTimeManager timeManager)
        : this(music, MidiAccessManager.Empty, timeManager)
    {
    }

    public MidiPlayer(MidiMusic music, IMidiAccess access, IMidiPlayerTimeManager timeManager)
        : this(music, access.OpenOutputAsync(access.Outputs.First().Id).Result, timeManager)
    {
        shouldDisposeOutput = true;
    }

    public MidiPlayer(MidiMusic music, IMidiOutput output, IMidiPlayerTimeManager timeManager)
    {
        if (music == null)
        {
            throw new ArgumentNullException("music");
        }
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }
        if (timeManager == null)
        {
            throw new ArgumentNullException("timeManager");
        }

        this.music = music;
        this.output = output;

        messages = SmfTrackMerger.Merge(music).Tracks[0].Messages;
        looper = new MidiEventLooper(messages, timeManager, music.DeltaTimeSpec);
        looper.Starting += () =>
        {
            // all control reset on all channels.
            for (int i = 0; i < 16; i++)
            {
                buffer[0] = (byte) (i + 0xB0);
                buffer[1] = 0x79;
                buffer[2] = 0;
                output.Send(buffer, 0, 3, 0);
            }
        };
        EventReceived += (m) =>
        {
            switch (m.EventType)
            {
                case MidiEvent.NoteOn:
                case MidiEvent.NoteOff:
                    if (channelMask != null && channelMask[m.Channel])
                    {
                        return; // ignore messages for the masked channel.
                    }
                    goto default;
                case MidiEvent.SysEx1:
                case MidiEvent.SysEx2:
                    if (buffer.Length <= m.ExtraDataLength)
                    {
                        buffer = new byte[buffer.Length * 2];
                    }
                    buffer[0] = m.StatusByte;
                    Array.Copy(m.ExtraData, m.ExtraDataOffset, buffer, 1, m.ExtraDataLength);
                    output.Send(buffer, 0, m.ExtraDataLength + 1, 0);
                    break;
                case MidiEvent.Meta:
                    // do nothing.
                    break;
                default:
                    var size = MidiEvent.FixedDataSize(m.StatusByte);
                    buffer[0] = m.StatusByte;
                    buffer[1] = m.Msb;
                    buffer[2] = m.Lsb;
                    output.Send(buffer, 0, size + 1, 0);
                    break;
            }
        };
    }

    public int GetTotalPlayTimeMilliseconds() => MidiMusic.GetTotalPlayTimeMilliseconds(messages, music.DeltaTimeSpec);

    public virtual void Dispose()
    {
        looper.Stop();
        if (shouldDisposeOutput)
        {
            output.Dispose();
        }
    }

    public void Play()
    {
        switch (State)
        {
            case PlayerState.Playing:
                return; // do nothing
            case PlayerState.Paused:
                looper.Play();
                return;
            case PlayerState.Stopped:
                if (syncPlayerTask == null || syncPlayerTask.Status != TaskStatus.Running)
                {
                    syncPlayerTask = Task.Run(looper.PlayerLoop);
                }
                looper.Play();
                return;
        }
    }

    public void Pause()
    {
        switch (State)
        {
            case PlayerState.Playing:
                looper.Pause();
                return;
            default: // do nothing
                return;
        }
    }

    public void Stop()
    {
        switch (State)
        {
            case PlayerState.Paused:
            case PlayerState.Playing:
                looper.Stop();
                break;
        }
    }

    public void Seek(int ticks) => looper.Seek(ticks);

    public void SetChannelMask(bool[] channelMask)
    {
        if (channelMask != null && channelMask.Length != 16)
        {
            throw new ArgumentException("Unexpected length of channelMask array; it must be an array of 16 elements.");
        }
        this.channelMask = channelMask;
        // additionally send all sound off for the muted channels.
        for (int ch = 0; ch < channelMask.Length; ch++)
        {
            if (channelMask[ch])
            {
                output.Send([(byte) (0xB0 + ch), 120, 0], 0, 3, 0);
            }
        }
    }
}
