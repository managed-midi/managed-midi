# Release history

This is the release history just for the `ManagedMidi` NuGet package, *not* the original `managed-midi` package.

## v1.0.0-alpha.2 (2026-07-22)

- First pass at migration documentation
- Overhauled README.md
- Removed `MidiMachine` (for now)
- Made some functionality in the `ManagedMidi.Smf` namespace internal
- Moved playback functionality into `ManagedMidi.Smf`
- Moved `MidiEvent.Convert` to a new `MidiEventConverter` class to avoid global mutable state
 
## v1.0.0-alpha.1 (2026-07-16)

First alpha of the `ManagedMidi` package, primarily to make it easy to test with other projects which currently consume `managed-midi`.

Migration documentation is yet to be written.
