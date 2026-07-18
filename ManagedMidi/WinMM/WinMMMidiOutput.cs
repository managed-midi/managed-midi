using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ManagedMidi.WinMM;

class WinMMMidiOutput : IMidiOutput
{
    private readonly IntPtr handle;
    private readonly MidiEventConverter eventConverter = new();
    public IMidiPortDetails Details { get; }
    public MidiPortConnectionState Connection { get; private set; }

    public WinMMMidiOutput(IMidiPortDetails details)
    {
        Details = details;
        WinMMNatives.midiOutOpen(out handle, uint.Parse(Details.Id), null, IntPtr.Zero, MidiOutOpenFlags.Null);
        Connection = MidiPortConnectionState.Open;
    }

    // TODO: Work out whether we really need to use Task.Run here.
    public Task CloseAsync() => Task.Run(() =>
    {
        Connection = MidiPortConnectionState.Pending;
        WinMMNatives.midiOutClose(handle);
        Connection = MidiPortConnectionState.Closed;
    });

    public void Dispose() => CloseAsync().Wait();

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        foreach (var evt in eventConverter.Convert(mevent, offset, length))
        {
            if (evt.StatusByte < 0xF0 || evt.ExtraData == null)
            {
                DieOnError(WinMMNatives.midiOutShortMsg(handle, (uint) (evt.StatusByte + (evt.Msb << 8) + (evt.Lsb << 16))));
            }
            else
            {
                var header = new MidiHdr();
                bool prepared = false;
                IntPtr ptr = IntPtr.Zero;
                var hdrSize = Marshal.SizeOf(typeof(MidiHdr));

                try
                {
                    // allocate unmanaged memory and hand ownership over to the device driver

                    header.Data = Marshal.AllocHGlobal(evt.ExtraDataLength);
                    header.BufferLength = evt.ExtraDataLength;
                    Marshal.Copy(evt.ExtraData, evt.ExtraDataOffset, header.Data, header.BufferLength);

                    ptr = Marshal.AllocHGlobal(hdrSize);
                    Marshal.StructureToPtr(header, ptr, false);

                    DieOnError(WinMMNatives.midiOutPrepareHeader(handle, ptr, hdrSize));
                    prepared = true;

                    DieOnError(WinMMNatives.midiOutLongMsg(handle, ptr, hdrSize));
                }

                finally
                {
                    // reclaim ownership and free

                    if (prepared)
                    {
                        DieOnError(WinMMNatives.midiOutUnprepareHeader(handle, ptr, hdrSize));
                    }

                    if (header.Data != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(header.Data);
                    }

                    if (ptr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }
        }
    }

    static void DieOnError(int code)
    {
        if (code != 0)
        {
            throw new Win32Exception(code, $"{WinMMNatives.GetMidiOutErrorText(code)} ({code})");
        }
    }
}
