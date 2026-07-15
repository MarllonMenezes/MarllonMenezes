#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AlbaWorld.Editor;

namespace AlbaWorld.Tests;

public sealed class KenneyFurnitureSourceTests
{
    [Test]
    public void ApprovedFurnitureManifestAndLicenseAreCommitted()
    {
        var manifestPath = "Assets/Art3D/Furniture/Source/KenneyFurnitureKit/manifest.json";
        var licensePath = "Assets/Art3D/Furniture/Source/KenneyFurnitureKit/License.txt";
        Assert.That(File.Exists(manifestPath), Is.True);
        Assert.That(File.Exists(licensePath), Is.True);
        Assert.That(File.ReadAllText(licensePath), Does.Contain("Creative Commons Zero"));

        foreach (var id in KenneyFurnitureAssetSetup.AllIds)
        {
            Assert.That(File.Exists(KenneyFurnitureAssetSetup.SourcePathFor(id)), Is.True, id);
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath), Is.Not.Null);
        }

        var prefabFiles = Directory.GetFiles("Assets/Art3D/Furniture/Prefabs", "*.prefab");
        Assert.That(prefabFiles.Count(path => path.EndsWith(".prefab")), Is.GreaterThanOrEqualTo(KenneyFurnitureAssetSetup.AllIds.Length));
    }
}
#endif
