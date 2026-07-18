# Migration from managed-midi

## Introduction

If you aren't familiar with the background of why this fork of `managed-midi` exists, please
start with [this blog post](https://codeblog.jonskeet.uk/2026/07/11/forking-an-open-source-project/).
Some of the plans described the blog post have changed over time, but the basic
ideas are still the same. It's also worth noting that the original *urgency* a
fork - creating a release to address an issue with MIDI in early-2026 Windows 11 - has
now abated, as it seems that Microsoft has fixed the issue. However, the fork continues
as it's better (in a global sense) to have a minimally-maintained project than an unmaintained one.

The hope is that most `managed-midi` users can migrate to `ManagedMidi` reasonably easily,
but there are definitely breaking changes. They broadly fall into one of a few types:

- Functionality which has simply moved, most obviously in terms of namespaces
- Addressing previously-obsolete aspects of the public API
- Making previously-public members internal or private
- Minor tweaks of the public API
- Removing functionality on an "until it's requested" basis
- Removing platform/backend support (which may or may not be temporary)

## Namespaces

Most code has moved namespace from `Commons.Music.Midi` to `ManagedMidi` or to a namespace under that.
In most cases, only `ManagedMidi` will be needed; nested namespaces are primarily used for `IMidiAccess`
implementations, which are currently internal.

However, a new namespace of `ManagedMidi.Smf` has been created for everything to do with
reading, writing and playback of SMF (Standard MIDI Files) - i.e. `.mid` files. This is to provide
a cleaner separation between that specific area of functionality and the core of `ManagedMidi`.

## `IMidiAccess` and obsolete markers

For some time, `managed-midi` was in a transitional state, with the `IMidiAccess` marked as obsolete
in favour of `IMidiAccess2`. `IMidiAccess2` introduced a new member (`ExtensionManager`) and the
`StateChanged` event was explicitly obsolete within `IMidiAccess`.

In `ManagedMidi`, there's just `IMidiAccess`. It's not obsolete, it includes `ExtensionManager`, and
doesn't include `StateChanged`.

Most users will be able to simply change any use of `IMidiAccess2` to `IMidiAccess` and remove any
warning-disablement for the use of `IMidiAccess`.

Some obsolete members have been removed, notably the `*Async` methods in `MidiPlayer` (just use the
methods without the `Async` suffix) and `WinMMMidiPlayer` has been removed (as it provided no
additional functionality).

## Public API tweaks

`MidiEvent.Convert` used to mutate global running state. The new `MidiEventConverter` type now keeps that
running state on a per-instance basis. Calls to `MidiEvent.Convert` should be migrated by creating a new
`MidiEventConverter` and using the same instance for all conversions which are logically part of the same
stream (so should maintain the same state).

## Removed functionality

A few types which were not used within the rest of the code have been removed for now.
The most prominent of these are `GeneralMidi`, `MidiMachine` and `MidiModuleDatabase`.

These could be restored relatively easily, but may undergo a bit of API modernization if this happens.
Please [file an issue](https://github.com/managed-midi/managed-midi/issues/new) if you use any of
this functionality and wish it to be restored.

Some read/write properties have become read-only.

## Internalized types

Many types which were previously public are now internal, including all the `IMidiAccess` implementations.
A smaller public API surface makes refactoring easier.

Some of the types in `ManagedMidi.Smf` which were public are now internal, but the functionality can usually
be achieved using other aspects of the public API surface. In particular, `SmfReader` is now internal,
but the existing static method `MidiMusic.Read(Stream)` provides the same functionality.
Likewise `SmfWriter` is now internal, but the new instance method `MidiMusic.WriteTo(Stream)` provides
the same functionality.

Where possible, migration should use the public API surface. If this proves impossible, please
[file an issue](https://github.com/managed-midi/managed-midi/issues/new) and we can at least *consider* making
some of the now-internal types public again. In some cases we may instead provide a new public method on an
existing public type to avoid having to add *too* much to the public API surface.

## Removed platform/backend support

The heart of the cross-platform functionality in `managed-midi` and `ManagedMidi` is via implementations of
`IMidiAccess`, also known as "backends".

`ManagedMidi` initially supports:

- Windows via WinMM
- MacOS via CoreMidi
- Linux via Alsa

The `RtMidiSharp` and `PortMidiSharp` implementations have been removed without changing the set of available
platforms. We do not expect to restore these unless there is significant demand for them.

Support for UWP and mobile platform has initially been dropped for simplicity.
The .NET support for these platforms has been volatile over the last decade, which makes them impractical for
a "minimal maintenance" project.

The reduction in the number of platforms has significantly simplified the build and packaging, which now
*only* targets .NET Standard 2.0.

Please [file an issue](https://github.com/managed-midi/managed-midi/issues/new) to request support for
mobile platforms, and we can consider the best course of action. It is very unlikely that we'd restore support
for now-obsolete approaches to supporting these platforms, but we could look at more modern options. These
*may* involve restoring the "bait and switch" approach to packaging.
