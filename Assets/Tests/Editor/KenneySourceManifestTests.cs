using NUnit.Framework;
using AlbaWorld.Editor;
using AlbaWorld.Pets;
using System;
using System.IO;

namespace AlbaWorld.Tests;

public sealed class KenneySourceManifestTests
{
    [Test]
    public void KenneyManifestContainsAllTwentyFourAnimals()
    {
        var manifest = KenneySourceManifest.LoadForTests();

        Assert.That(manifest.AnimalIds, Is.EquivalentTo(KenneyPetIds.All));
        Assert.That(manifest.License, Does.Contain("CC0"));
        Assert.That(manifest.SourcesById.Count, Is.EqualTo(KenneyPetIds.All.Length));
        Assert.That(manifest.SourceFor("pet.cat"), Is.EqualTo("animal-cat.fbx"));
    }

    [Test]
    public void ManifestLoadsWhenCurrentDirectoryIsNotTheProjectRoot()
    {
        var originalDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = Path.GetTempPath();
            var manifest = KenneySourceManifest.LoadForTests();
            Assert.That(manifest.SourceFor("pet.tiger"), Is.EqualTo("animal-tiger.fbx"));
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
        }
    }
}
