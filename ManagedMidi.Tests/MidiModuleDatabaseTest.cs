using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ManagedMidi.Tests;

[TestFixture]
public class MidiModuleDatabaseTest
{
    [Test]
    public void EnsureResourcesAreEmbedded()
    {
        var mods = MidiModuleDatabase.Default.All();
        Assert.IsTrue(mods.Any(m => m.Name == "Microsoft GS Wavetable SW Synth"), "ms-gs-synth");
        Assert.IsTrue(mods.Any(m => m.Name == "Roland SC-8820"), "sc-8820");
        Assert.IsTrue(mods.Any(m => m.Name == "YAMAHA MOTIF-RACK(Multi Mode)"), "motif-rack");
    }
}