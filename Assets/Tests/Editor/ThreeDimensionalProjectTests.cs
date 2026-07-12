using System.IO;
using NUnit.Framework;

namespace AlbaWorld.Tests;

public sealed class ThreeDimensionalProjectTests
{
    [Test]
    public void UrpAndThreeDimensionalSettingsArePresent()
    {
        var manifest = File.ReadAllText("Packages/manifest.json");
        StringAssert.Contains("\"com.unity.render-pipelines.universal\": \"17.3.0\"", manifest);
        Assert.That(File.Exists("Assets/Settings/AlbaWorldURP.asset"), Is.True);
        Assert.That(File.Exists("Assets/Settings/AlbaWorldRenderer.asset"), Is.True);
        Assert.That(File.Exists("Assets/Scenes/Main.unity"), Is.True);
    }
}
