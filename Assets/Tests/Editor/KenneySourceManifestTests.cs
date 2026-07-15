using NUnit.Framework;
using AlbaWorld.Editor;
using AlbaWorld.Pets;

namespace AlbaWorld.Tests;

public sealed class KenneySourceManifestTests
{
    [Test]
    public void KenneyManifestContainsAllTwentyFourAnimals()
    {
        var manifest = KenneySourceManifest.LoadForTests();

        Assert.That(manifest.AnimalIds, Is.EquivalentTo(KenneyPetIds.All));
        Assert.That(manifest.License, Does.Contain("CC0"));
    }
}
