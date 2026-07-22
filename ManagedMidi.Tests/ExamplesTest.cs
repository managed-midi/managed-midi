using ManagedMidi.Smf;

namespace ManagedMidi.Tests;

// These aren't real tests, but just check that the sample code in README.md will
// compile. The code is duplicated - there's no "extra code from here to put it in the README" yet.
internal class ExamplesTest
{
    private static async void PlayFile()
    {
        var access = MidiAccessManager.Default;
        using var output = await access.OpenOutputAsync(access.Outputs.Last().Id);
        var music = MidiMusic.Read(System.IO.File.OpenRead("mysong.mid"));
        using var player = new MidiPlayer(music, output);
        player.EventReceived += (MidiEvent e) =>
        {
            if (e.EventType == MidiEvent.Program)
            {
                Console.WriteLine($"Program changed: Channel:{e.Channel} Instrument:{e.Msb}");
            }
        };
        player.Play();
        Console.WriteLine("Type [CR] to stop.");
        Console.ReadLine();
    }

    private static async void PlayNotes()
    {
        var access = MidiAccessManager.Default;
        using var output = await access.OpenOutputAsync(access.Outputs.Last().Id);
        output.Send(new byte[] { 0xC0, 0x00 }, 0, 2, 0); // General MIDI accoustic grand piano
        output.Send(new byte[] { MidiEvent.NoteOn, 0x40, 0x70 }, 0, 3, 0); // There are constant fields for each MIDI event
        output.Send(new byte[] { MidiEvent.NoteOff, 0x40, 0x70 }, 0, 3, 0);
        output.Send(new byte[] { MidiEvent.Program, 0x30 }, 0, 2, 0); // Strings Ensemble
        output.Send(new byte[] { 0x90, 0x40, 0x70 }, 0, 3, 0);
        output.Send(new byte[] { 0x80, 0x40, 0x70 }, 0, 3, 0);
    }
}
