using System;

namespace ManagedMidi.WinMM;

[Obsolete("This class does not do anything special. Just use MidiPlayer.")]
internal class WinMMMidiPlayer : MidiPlayer
{
    public WinMMMidiPlayer(MidiMusic music)
        : base(music)
    {
    }
}
