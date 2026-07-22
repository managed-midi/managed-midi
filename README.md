# Note: this fork is a work in progress

This is a fork from https://github.com/atsushieno/managed-midi. It is intended to *eventually* become the new home for the codebase currently in the `managed-midi` NuGet package, although current plans are to create a new package called `ManagedMidi`, with a `ManagedMidi` namespace.

See https://codeblog.jonskeet.uk/2026/07/11/forking-an-open-source-project/ for more details.

Many things are still to-do, including:

- Copyright and licensing details
- Credit for alsa-sharp now that the code is in this repo

A [migration guide](docs/migration.md) exists, but is a work in progress.

# ManagedMidi

ManagedMidi aims to provide C#/.NET API For almost-raw access to MIDI devices in cross-platform manner. It supports
Linux, Mac and Windows; the mobile support present in [the original library](https://github.com/atsushieno/managed-midi)
has initially been dropped, but we'll be happy to restore it (in a modernized fashion) if there's sufficient demand.

## API

ManagedMidi follows semantic versioning:

- While we're in v1 alpha, expect the public API to change a lot, including functionality being removed.
- In v2 beta, we expect the API to be *more* stable, but it might still change a bit. We don't expect to remove functionality at this stage.
- Once v1.0.0 has been hit, there should be no breaking changes to the public API without a major version bump.

## Quick feature survey

Here is the list of the base library features:

- `MidiEvent`, `MidiMessage`, `MidiTrack` and `MidiMusic` to store sequence of events, tracks, up to a song.
  - No strongly-typed message types (something like NoteOnMessage, NoteOffMessage, and so on). There is no point of defining strongly-typed messages for each mere MIDI status byte - you wouldn't need message type abstraction.
  - No worries, there are `MidiCC`, `MidiRpnType`, `MidiMetaType` and `MidiEvent` fields (of type System.Byte) so that you don't have to remember the actual numbers.
- SMF files can be read using `MidiMusic.Read()` and written using `MidiMusic.WriteTo()`
- `IMidiAccess`: raw MIDI Access abstraction, to create `IMidiInput` and `IMidiOutput` channels that are used to receive or send MIDI messages to and from the actual MIDI devices.
  - This is the core abstraction of access to underlying platform-specific MIDI libraries.
  - Use `MidiAccessManager.Default` to obtain the default implementation for the current system.
- `MidiPlayer` allows `MidiMusic` 
  - It supports play/pause/stop and fast-forwarding.
  - MIDI messages are sent to its event `EventReceived`. If you don't pass a MIDI Access instance or a MIDI Output instance, it will do nothing.
  - `IMidiTimeManager`: Time manager is abstract. You can define your actual behavior for "advance by X seconds". By default it (of course) waits for the specified time, using `Task.Delay()`. It's like `IScheduler` in Rx.

## MIDI Access API implementations

The current implemenations of `IMidiAccess` are:

- ALSA: Used by default on Linux; the underlying access is implemented via code inlined from [alsa-sharp](https://github.com/atsushieno/alsa-sharp)
- WinMM: Used by default on Windows.
- Core MIDI: Used by default on mac OS.

## Quick Examples

### Play notes

Make sure that you have active and audible (i.e. non-thru) MIDI output device.

```csharp
using ManagedMidi;

var access = MidiAccessManager.Default;
using var output = await access.OpenOutputAsync(access.Outputs.Last().Id);
output.Send(new byte [] {0xC0, 0x00}, 0, 2, 0); // General MIDI accoustic grand piano
output.Send(new byte [] {MidiEvent.NoteOn, 0x40, 0x70}, 0, 3, 0); // There are constant fields for each MIDI event
output.Send(new byte [] {MidiEvent.NoteOff, 0x40, 0x70}, 0, 3, 0);
output.Send(new byte [] {MidiEvent.Program, 0x30}, 0, 2, 0); // Strings Ensemble
output.Send(new byte [] {0x90, 0x40, 0x70}, 0, 3, 0);
output.Send(new byte [] {0x80, 0x40, 0x70}, 0, 3, 0);
```

### Play MIDI song file (SMF), detecting specific events

```csharp
using ManagedMidi;

var access = MidiAccessManager.Default;
using var output = await access.OpenOutputAsync(access.Outputs.Last().Id);
var music = MidiMusic.Read(System.IO.File.OpenRead("mysong.mid"));
using var player = new MidiPlayer(music, output);
player.EventReceived += (MidiEvent e) =>
{
    if (e.EventType == MidiEvent.Program)
    {
        Console.WriteLine ($"Program changed: Channel:{e.Channel} Instrument:{e.Msb}");
    }
};
player.Play();
Console.WriteLine("Type [CR] to stop.");
Console.ReadLine();
```

### Implementation notes

There are couple of design note docs placed under [docs](./docs) directory.
