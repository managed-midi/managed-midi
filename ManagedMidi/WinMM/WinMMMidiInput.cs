using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ManagedMidi.WinMM;

class WinMMMidiInput : IMidiInput
{
    MidiInProc midiInProc;

    public WinMMMidiInput(IMidiPortDetails details)
    {
        Details = details;

        // prevent garbage collection of the delegate
        midiInProc = HandleMidiInProc;

        DieOnError(WinMMNatives.midiInOpen(out handle, uint.Parse(Details.Id), midiInProc,
            IntPtr.Zero, MidiInOpenFlags.Function | MidiInOpenFlags.MidiIoStatus));

        DieOnError(WinMMNatives.midiInStart(handle));

        while (lmBuffers.Count < LONG_BUFFER_COUNT)
        {
            var buffer = new LongMessageBuffer(handle);

            buffer.PrepareHeader();
            buffer.AddBuffer();

            lmBuffers.Add(buffer.Ptr, buffer);
        }

        Connection = MidiPortConnectionState.Open;
    }

    const int LONG_BUFFER_COUNT = 16;

    Dictionary<IntPtr, LongMessageBuffer> lmBuffers = new Dictionary<IntPtr, LongMessageBuffer>();

    IntPtr handle;
    object lockObject = new object();

    byte[] data1b = new byte[1];
    byte[] data2b = new byte[2];
    byte[] data3b = new byte[3];

    void HandleData(IntPtr param1, IntPtr param2)
    {
        var status = (byte) ((int) param1 & 0xFF);
        var msb = (byte) (((int) param1 & 0xFF00) >> 8);
        var lsb = (byte) (((int) param1 & 0xFF0000) >> 16);
        var size = MidiEvent.FixedDataSize(status);
        var data = size == 1 ? data2b : size == 2 ? data3b : data1b;
        data[0] = status;
        if (data.Length >= 2)
            data[1] = msb;
        if (data.Length >= 3)
            data[2] = lsb;

        MessageReceived(this, new MidiReceivedEventArgs() { Data = data, Start = 0, Length = data.Length, Timestamp = (long) param2 });
    }

    void HandleLongData(IntPtr param1, IntPtr param2)
    {
        byte[] data = null;

        lock (lockObject)
        {
            var buffer = lmBuffers[param1];
            // FIXME: this is a nasty workaround for https://github.com/atsushieno/managed-midi/issues/49
            // We have no idea when/how this message is sent (midi in proc is not well documented).
            if (buffer.Header.BytesRecorded == 0)
            {
                if (Connection != MidiPortConnectionState.Open)
                {
                    FreeBuffer(buffer);
                }
                return;
            }

            data = new byte[buffer.Header.BytesRecorded];

            Marshal.Copy(buffer.Header.Data, data, 0, buffer.Header.BytesRecorded);

            if (Connection == MidiPortConnectionState.Open)
            {
                buffer.Recycle();
            }
            else
            {
                FreeBuffer(buffer);
            }
        }

        if (data != null && data.Length != 0)
            MessageReceived(this, new MidiReceivedEventArgs() { Data = data, Start = 0, Length = data.Length, Timestamp = (long) param2 });
    }

    void HandleMidiInProc(IntPtr midiIn, MidiInMessage msg, IntPtr instance, IntPtr param1, IntPtr param2)
    {
        if (MessageReceived != null)
        {
            switch (msg)
            {
                case MidiInMessage.Data:
                    HandleData(param1, param2);
                    break;

                case MidiInMessage.LongData:
                    HandleLongData(param1, param2);
                    break;

                case MidiInMessage.MoreData:
                    // TODO input too slow, handle.
                    break;

                case MidiInMessage.Error:
                    throw new InvalidOperationException($"Invalid MIDI message: {param1}");

                case MidiInMessage.LongError:
                    throw new InvalidOperationException("Invalid SysEx message.");

                default:
                    break;
            }
        }
        else if (Connection != MidiPortConnectionState.Open && msg == MidiInMessage.LongData)
        {
            lock (lockObject)
            {
                var buffer = lmBuffers[param1];
                FreeBuffer(buffer);
            }
        }
    }

    void FreeBuffer(LongMessageBuffer buffer)
    {
        lmBuffers.Remove(buffer.Ptr);
        buffer.Dispose();
    }

    public IMidiPortDetails Details { get; private set; }

    public MidiPortConnectionState Connection { get; private set; }

    public event EventHandler<MidiReceivedEventArgs> MessageReceived;

    public Task CloseAsync()
    {
        return Task.Run(() =>
        {
            lock (lockObject)
            {
                Connection = MidiPortConnectionState.Pending;

                DieOnError(WinMMNatives.midiInReset(handle));
                DieOnError(WinMMNatives.midiInStop(handle));
                DieOnError(WinMMNatives.midiInClose(handle));
            }

            // wait for the device driver to hand back the long buffers through HandleMidiInProc

            for (int i = 0; i < 1000; i++)
            {
                lock (lockObject)
                {
                    if (lmBuffers.Count < 1)
                        break;
                }

                Thread.Sleep(10);
            }

            Connection = MidiPortConnectionState.Closed;
        });
    }

    public void Dispose()
    {
        CloseAsync().Wait();
    }

    static void DieOnError(int code)
    {
        if (code != 0)
            throw new Win32Exception(code, $"{WinMMNatives.GetMidiInErrorText(code)} ({code})");
    }

    class LongMessageBuffer : IDisposable
    {
        public IntPtr Ptr { get; set; } = IntPtr.Zero;
        public MidiHdr Header => (MidiHdr) Marshal.PtrToStructure(Ptr, typeof(MidiHdr));

        IntPtr inputHandle;
        static int midiHdrSize = Marshal.SizeOf(typeof(MidiHdr));

        bool prepared = false;

        public LongMessageBuffer(IntPtr inputHandle, int bufferSize = 4096)
        {
            this.inputHandle = inputHandle;

            var header = new MidiHdr()
            {
                Data = Marshal.AllocHGlobal(bufferSize),
                BufferLength = bufferSize,
            };

            try
            {
                Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MidiHdr)));
                Marshal.StructureToPtr(header, Ptr, false);
            }
            catch
            {
                Free();
                throw;
            }
        }

        public void PrepareHeader()
        {
            if (!prepared)
                DieOnError(WinMMNatives.midiInPrepareHeader(inputHandle, Ptr, midiHdrSize));

            prepared = true;
        }

        public void UnPrepareHeader()
        {
            if (prepared)
                DieOnError(WinMMNatives.midiInUnprepareHeader(inputHandle, Ptr, midiHdrSize));

            prepared = false;
        }

        public void AddBuffer() =>
            DieOnError(WinMMNatives.midiInAddBuffer(inputHandle, Ptr, midiHdrSize));

        public void Dispose()
        {
            Free();
        }

        public void Recycle()
        {
            UnPrepareHeader();
            PrepareHeader();
            AddBuffer();
        }

        void Free()
        {
            UnPrepareHeader();

            if (Ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Header.Data);
                Marshal.FreeHGlobal(Ptr);
            }
        }
    }
}
