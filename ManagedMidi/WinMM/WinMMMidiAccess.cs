using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ManagedMidi.WinMM;

internal class WinMMMidiAccess : IMidiAccess
{
    public MidiAccessExtensionManager ExtensionManager { get; } = new();

    public IEnumerable<IMidiPortDetails> Inputs
    {
        get
        {
            int devs = WinMMNatives.midiInGetNumDevs();
            for (uint i = 0; i < devs; i++)
            {
                MidiInCaps caps;
                WinMMNatives.midiInGetDevCaps((UIntPtr) i, out caps, (uint) Marshal.SizeOf<MidiInCaps>());
                yield return new WinMMPortDetails(i, caps.Name, caps.DriverVersion);
            }
        }
    }

    public IEnumerable<IMidiPortDetails> Outputs
    {
        get
        {
            int devs = WinMMNatives.midiOutGetNumDevs();
            for (uint i = 0; i < devs; i++)
            {
                MidiOutCaps caps;
                var err = WinMMNatives.midiOutGetDevCaps((UIntPtr) i, out caps, (uint) Marshal.SizeOf<MidiOutCaps>());
                if (err != 0)
                    throw new Win32Exception(err);
                yield return new WinMMPortDetails(i, caps.Name, caps.DriverVersion);
            }
        }
    }

    public Task<IMidiInput> OpenInputAsync(string portId)
    {
        var details = Inputs.FirstOrDefault(d => d.Id == portId);
        if (details == null)
            throw new InvalidOperationException($"The device with ID {portId} is not found.");
        return Task.FromResult((IMidiInput) new WinMMMidiInput(details));
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        var details = Outputs.FirstOrDefault(d => d.Id == portId);
        if (details == null)
            throw new InvalidOperationException($"The device with ID {portId} is not found.");
        return Task.FromResult((IMidiOutput) new WinMMMidiOutput(details));
    }
}
