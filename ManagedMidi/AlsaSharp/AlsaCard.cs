using System.Runtime.InteropServices;

namespace ManagedMidi.AlsaSharp;

internal class AlsaCard
{
    public static string GetCardName(int card)
    {
        unsafe
        {
            IntPtr ptr = IntPtr.Zero;
            var pref = &ptr;
            Natives.snd_card_get_name(card, (IntPtr) pref);
            return Marshal.PtrToStringAnsi(ptr);
        }
    }
}
