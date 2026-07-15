using System.IO;
using System.Linq;
using NUnit.Framework;

namespace AlbaWorld.Tests;

public sealed class AlbaWorldEditorTests
{
    [Test]
    public void DemoSceneIsIncludedInTheProject()
    {
        Assert.That(File.Exists("Assets/Scenes/Main.unity"), Is.True);
    }

    [Test]
    public void PackageIdentityIsStable()
    {
        var projectSettings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");
        StringAssert.Contains("com.albaworldgames.albaworld", projectSettings);
    }

    [Test]
    public void InitialCatalogHasAtLeastFortyItemsAndEightRewardedItems()
    {
        var itemLines = File.ReadAllLines("Assets/Scripts/Runtime/RuntimeCatalog.cs")
            .Where(line => line.TrimStart().StartsWith("Add(\""))
            .ToArray();

        Assert.That(itemLines.Length, Is.GreaterThanOrEqualTo(40));
        Assert.That(itemLines.Count(line => line.Contains(", false,")), Is.GreaterThanOrEqualTo(8));
    }
}
