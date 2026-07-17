namespace ManagedMidi.Smf;

// TODO: Refactor this to avoid state.
internal class SmfReader
{
    private Stream stream;
    public MidiMusic Music { get; private set; }
    private int currentTrackSize;
    private byte runningStatus;
    private int peekByte = -1;
    private int streamPosition;

    public void Read(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        this.stream = stream;
        Music = new MidiMusic();
        try
        {
            DoParse();
        }
        finally
        {
            this.stream = null;
        }
    }

    private void DoParse()
    {
        if (ReadByte() != 'M'
            || ReadByte() != 'T'
            || ReadByte() != 'h'
            || ReadByte() != 'd')
        {
            throw ParseError("MThd is expected");
        }
        if (ReadInt32() != 6)
        {
            throw ParseError("Unexpected data size (should be 6)");
        }
        Music.Format = (byte) ReadInt16();
        int tracks = ReadInt16();
        Music.DeltaTimeSpec = ReadInt16();
        try
        {
            for (int i = 0; i < tracks; i++)
            {
                Music.Tracks.Add(ReadTrack());
            }
        }
        catch (FormatException ex)
        {
            throw ParseError("Unexpected data error", ex);
        }
    }

    private MidiTrack ReadTrack()
    {
        var tr = new MidiTrack();
        if (ReadByte() != 'M'
            || ReadByte() != 'T'
            || ReadByte() != 'r'
            || ReadByte() != 'k')
        {
            throw ParseError("MTrk is expected");
        }
        int trackSize = ReadInt32();
        currentTrackSize = 0;
        int total = 0;
        while (currentTrackSize < trackSize)
        {
            int delta = ReadVariableLength();
            tr.Messages.Add(ReadMessage(delta));
            total += delta;
        }
        if (currentTrackSize != trackSize)
        {
            throw ParseError("Size information mismatch");
        }
        return tr;
    }

    private MidiMessage ReadMessage(int deltaTime)
    {
        byte b = PeekByte();
        runningStatus = b < 0x80 ? runningStatus : ReadByte();
        int len;
        switch (runningStatus)
        {
            case MidiEvent.SysEx1:
            case MidiEvent.SysEx2:
            case MidiEvent.Meta:
                byte metaType = runningStatus == MidiEvent.Meta ? ReadByte() : (byte) 0;
                len = ReadVariableLength();
                byte[] args = new byte[len];
                if (len > 0)
                {
                    ReadBytes(args);
                }
                return new MidiMessage(deltaTime, new MidiEvent(runningStatus, metaType, 0, args, 0, args.Length));
            default:
                int value = runningStatus;
                value += ReadByte() << 8;
                if (MidiEvent.FixedDataSize(runningStatus) == 2)
                {
                    value += ReadByte() << 16;
                }
                return new MidiMessage(deltaTime, new MidiEvent(value));
        }
    }

    private void ReadBytes(byte[] args)
    {
        currentTrackSize += args.Length;
        int start = 0;
        if (peekByte >= 0)
        {
            args[0] = (byte) peekByte;
            peekByte = -1;
            start = 1;
        }
        int len = stream.Read(args, start, args.Length - start);
        try
        {
            if (len < args.Length - start)
            {
                throw ParseError($"The stream is insufficient to read {args.Length} bytes specified in the SMF message. Only {len} bytes read.");
            }
        }
        finally
        {
            streamPosition += len;
        }
    }

    private int ReadVariableLength()
    {
        int val = 0;
        for (int i = 0; i < 4; i++)
        {
            byte b = ReadByte();
            val = (val << 7) + b;
            if (b < 0x80)
            {
                return val;
            }
            val -= 0x80;
        }
        throw ParseError("Delta time specification exceeds the 4-byte limitation.");
    }

    private byte PeekByte()
    {
        if (peekByte < 0)
        {
            peekByte = stream.ReadByte();
        }
        if (peekByte < 0)
        {
            throw ParseError("Insufficient stream. Failed to read a byte.");
        }
        return (byte) peekByte;
    }

    private byte ReadByte()
    {
        try
        {
            currentTrackSize++;
            if (peekByte >= 0)
            {
                byte b = (byte) peekByte;
                peekByte = -1;
                return b;
            }
            int ret = stream.ReadByte();
            if (ret < 0)
            {
                throw ParseError("Insufficient stream. Failed to read a byte.");
            }
            return (byte) ret;
        }
        finally
        {
            streamPosition++;
        }
    }

    private short ReadInt16() => (short) ((ReadByte() << 8) + ReadByte());

    private int ReadInt32() => (((ReadByte() << 8) + ReadByte() << 8) + ReadByte() << 8) + ReadByte();

    private Exception ParseError(string msg) => ParseError(msg, null);

    private Exception ParseError(string msg, Exception innerException) => new SmfParserException($"{msg} (at {streamPosition})", innerException);
}
