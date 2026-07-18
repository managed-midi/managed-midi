using ManagedMidi.Alsa;
using ManagedMidi.CoreMidi;
using ManagedMidi.Empty;
using ManagedMidi.WinMM;
using System.Runtime.InteropServices;

namespace ManagedMidi;

/// <summary>
/// Static class providing access to default (platform-specific) and empty implementations of <see cref="IMidiAccess"/>.
/// </summary>
public static class MidiAccessManager
{
    public static IMidiAccess Default => DefaultImpl.Default;
    public static IMidiAccess Empty { get; } = new EmptyMidiAccess();

    // Separate class to achieve laziness in a simple thread-safe way.
    private static class DefaultImpl
    {
        internal static IMidiAccess Default { get; } =
            Environment.OSVersion.Platform != PlatformID.Unix ? new WinMMMidiAccess()
            : IsRunningOnMac() ? new CoreMidiAccess()
            : new AlsaMidiAccess();

        //From Managed.Windows.Forms/XplatUI
        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        static bool IsRunningOnMac()
        {
            IntPtr buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                // This is a hacktastic way of getting sysname from uname ()
                if (uname(buf) == 0)
                {
                    string os = Marshal.PtrToStringAnsi(buf);
                    if (os == "Darwin")
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (buf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
            return false;
        }
    }
}
