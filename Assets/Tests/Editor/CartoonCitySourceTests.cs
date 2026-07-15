#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class CartoonCitySourceTests
{
    private const string SourceRoot = "Assets/Art3D/Characters/Source/RGPolyCartoonCity";
    private const string ManifestPath = SourceRoot + "/manifest.json";
    private const string LicensePath = SourceRoot + "/License.txt";

    [Test]
    public void ApprovedFreeSourceManifestAndLicenseAreCommitted()
    {
        Assert.That(File.Exists(ManifestPath), Is.True, "Cartoon City source manifest is missing");
        Assert.That(File.Exists(LicensePath), Is.True, "Cartoon City license file is missing");
        Assert.That(File.ReadAllText(LicensePath), Does.Contain("CC0"));
        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath), Is.Not.Null);

        Assert.That(File.Exists(SourceRoot + "/FBX/Unity FBX/Character_1_2_2.fbx"), Is.True);
        Assert.That(File.Exists(SourceRoot + "/FBX/Unity FBX/Character_2_1_3.fbx"), Is.True);
        Assert.That(File.Exists(SourceRoot + "/FBX/Unity FBX/Animations/Idle_A.fbx"), Is.True);
    }
}
#endif
