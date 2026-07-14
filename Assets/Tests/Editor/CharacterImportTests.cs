using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlbaWorld.Tests;

public sealed class CharacterImportTests
{
    private const string GirlModelPath = "Assets/Art3D/Characters/Models/body-girl.fbx";
    private const string BoyModelPath = "Assets/Art3D/Characters/Models/body-boy.fbx";
    private const string GirlPrefabPath = "Assets/Art3D/Characters/Prefabs/BodyGirl.prefab";
    private const string BoyPrefabPath = "Assets/Art3D/Characters/Prefabs/BodyBoy.prefab";
    private const string SkinAtlasPath = "Assets/Art3D/Characters/Textures/character-skin-atlas.png";
    private const string SkinMaterialPath = "Assets/Art3D/Characters/Materials/CharacterSkin.mat";
    private static string InEngineReviewPath => Path.Combine(
        Path.GetDirectoryName(Application.dataPath)!, "Art", "Reviews", "Task5", "character-bases-in-engine.png");

    private static readonly string[] RequiredBones =
    {
        "Root", "Hips", "Spine", "Chest", "Neck", "Head",
        "UpperArm.L", "LowerArm.L", "Hand.L", "UpperArm.R", "LowerArm.R", "Hand.R",
        "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R",
    };

    [TestCase(GirlModelPath)]
    [TestCase(BoyModelPath)]
    public void CharacterModelsImportAsHumanoid(string modelPath)
    {
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;

        Assert.That(importer, Is.Not.Null, $"Missing model importer for {modelPath}");
        Assert.That(importer!.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
        Assert.That(importer.avatarSetup, Is.EqualTo(ModelImporterAvatarSetup.CreateFromThisModel));
    }

    [TestCase(GirlPrefabPath, "BodyGirl")]
    [TestCase(BoyPrefabPath, "BodyBoy")]
    public void CharacterPrefabsHaveAnimatorAndExpectedRoot(string prefabPath, string expectedName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        Assert.That(prefab, Is.Not.Null, $"Missing prefab {prefabPath}");
        Assert.That(prefab!.name, Is.EqualTo(expectedName));
        Assert.That(prefab.GetComponentInChildren<Animator>(true), Is.Not.Null);
    }

    [Test]
    public void CharacterModelsShareExactRigNamesAndRestPose()
    {
        var girl = LoadModel(GirlModelPath);
        var boy = LoadModel(BoyModelPath);
        var girlRig = BoneMap(girl);
        var boyRig = BoneMap(boy);

        Assert.That(girlRig.Keys.OrderBy(name => name), Is.EqualTo(RequiredBones.OrderBy(name => name)));
        Assert.That(boyRig.Keys.OrderBy(name => name), Is.EqualTo(RequiredBones.OrderBy(name => name)));
        foreach (var boneName in RequiredBones)
        {
            Assert.That(Vector3.Distance(boyRig[boneName].localPosition, girlRig[boneName].localPosition), Is.LessThan(0.0001f), boneName);
            Assert.That(Quaternion.Angle(boyRig[boneName].localRotation, girlRig[boneName].localRotation), Is.LessThan(0.01f), boneName);
        }
    }

    [Test]
    public void CharacterPrefabBoundsMatchApprovedScale()
    {
        var girlHeight = RendererHeight(LoadPrefab(GirlPrefabPath));
        var boyHeight = RendererHeight(LoadPrefab(BoyPrefabPath));

        Assert.That(
            Mathf.Abs(girlHeight - boyHeight),
            Is.LessThanOrEqualTo(0.02f),
            $"Imported renderer bounds must match (girl={girlHeight:F4}m, boy={boyHeight:F4}m). Source height is validated in Blender.");
    }

    [Test]
    public void CharacterSkinMaterialUsesTheSixSwatchAtlas()
    {
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(SkinAtlasPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(SkinMaterialPath);

        Assert.That(atlas, Is.Not.Null);
        Assert.That(atlas!.width, Is.EqualTo(1024));
        Assert.That(atlas.height, Is.EqualTo(1024));
        Assert.That(material, Is.Not.Null);
        Assert.That(material!.shader.name, Is.EqualTo("Universal Render Pipeline/Simple Lit"));
        Assert.That(material.GetTexture("_BaseMap"), Is.SameAs(atlas));
        Assert.That(Vector2.Distance(material.GetTextureOffset("_BaseMap"), Vector2.zero),
            Is.LessThan(0.001f), "The default character skin must sample the light lower atlas row");
        foreach (var prefabPath in new[] { GirlPrefabPath, BoyPrefabPath })
        {
            var prefab = LoadPrefab(prefabPath);
            Assert.That(
                prefab.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Any(assigned => assigned == material),
                Is.True,
                $"{prefabPath} must use the external CharacterSkin material");
        }
    }

    [Test]
    public void CharacterPaletteMaterialsPreserveDistinctAuthoredColors()
    {
        var expected = new Dictionary<string, Color>
        {
            ["CocoaHair"] = new(0.15f, 0.045f, 0.018f, 1f),
            ["EyeWhite"] = new(0.93f, 0.91f, 0.84f, 1f),
            ["GirlBaseSuit"] = new(0.57f, 0.32f, 0.78f, 1f),
            ["BoyBaseSuit"] = new(0.22f, 0.68f, 0.55f, 1f),
            ["GirlHairBow"] = new(0.53f, 0.26f, 0.74f, 1f),
        };
        foreach (var pair in expected)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art3D/Characters/Materials/{pair.Key}.mat");
            Assert.That(material, Is.Not.Null, pair.Key);
            Assert.That(Vector4.Distance(material!.GetColor("_BaseColor"), pair.Value), Is.LessThan(0.01f), pair.Key);
        }
        Assert.That(Vector4.Distance(expected["GirlBaseSuit"], expected["BoyBaseSuit"]), Is.GreaterThan(0.4f));
    }

    [Test]
    public void RebuildingCharacterAssetsPreservesMaterialGuids()
    {
        var materialPaths = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art3D/Characters/Materials" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .ToArray();
        var before = materialPaths.ToDictionary(path => path, AssetDatabase.AssetPathToGUID);

        var setupType = Type.GetType("AlbaWorld.Editor.CharacterAssetSetup, Assembly-CSharp-Editor");
        Assert.That(setupType, Is.Not.Null);
        setupType!.GetMethod("Build")!.Invoke(null, null);

        var rebuiltMaterialPaths = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art3D/Characters/Materials" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .ToArray();
        Assert.That(rebuiltMaterialPaths, Is.EqualTo(materialPaths),
            "Build must preserve the exact material asset set without adding or removing entries");
        Assert.That(rebuiltMaterialPaths, Has.Length.EqualTo(11));
        foreach (var path in rebuiltMaterialPaths)
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(before[path]), path);
    }

    [Test]
    public void CaptureBatchingScopeRestoresBothFlagsWhenRenderingThrows()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        var batchingProperty = pipeline?.GetType().GetProperty("useSRPBatcher");
        Assert.That(pipeline, Is.Not.Null);
        Assert.That(batchingProperty, Is.Not.Null);
        var previousAssetBatching = (bool)batchingProperty!.GetValue(pipeline)!;
        var previousGlobalBatching = GraphicsSettings.useScriptableRenderPipelineBatching;
        var setupType = Type.GetType("AlbaWorld.Editor.CharacterAssetSetup, Assembly-CSharp-Editor");
        var captureMethod = setupType?.GetMethod(
            "RenderCameraWithTemporarySrpBatchingDisabled",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(captureMethod, Is.Not.Null,
            "Capture must isolate SRP batching changes in a dedicated render-only scope");
        var renderFailure = Assert.Throws<TargetInvocationException>(
            () => captureMethod!.Invoke(null, new object?[] { null }));
        Assert.That(renderFailure!.InnerException, Is.TypeOf<NullReferenceException>(),
            "The controlled failure must occur at camera.Render after both batching setters run");
        Assert.That((bool)batchingProperty.GetValue(pipeline)!, Is.EqualTo(previousAssetBatching));
        Assert.That(GraphicsSettings.useScriptableRenderPipelineBatching, Is.EqualTo(previousGlobalBatching));
    }

    [Test]
    public void RenderReviewRestoresBatchingAndWritesTheApprovedCapture()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        var batchingProperty = pipeline?.GetType().GetProperty("useSRPBatcher");
        Assert.That(pipeline, Is.Not.Null);
        Assert.That(batchingProperty, Is.Not.Null);
        var previousAssetBatching = (bool)batchingProperty!.GetValue(pipeline)!;
        var previousGlobalBatching = GraphicsSettings.useScriptableRenderPipelineBatching;
        var setupType = Type.GetType("AlbaWorld.Editor.CharacterAssetSetup, Assembly-CSharp-Editor");

        setupType!.GetMethod("RenderReview")!.Invoke(null, null);

        Assert.That((bool)batchingProperty.GetValue(pipeline)!, Is.EqualTo(previousAssetBatching));
        Assert.That(GraphicsSettings.useScriptableRenderPipelineBatching, Is.EqualTo(previousGlobalBatching));
        Assert.That(new FileInfo(InEngineReviewPath).Length, Is.GreaterThan(100_000));
    }

    [TestCase(GirlPrefabPath)]
    [TestCase(BoyPrefabPath)]
    public void CharacterPrefabsUseCompactLitRendererSets(string prefabPath)
    {
        var prefab = LoadPrefab(prefabPath);
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        var materials = renderers
            .SelectMany(renderer => renderer.sharedMaterials)
            .Where(material => material != null)
            .Distinct()
            .ToArray();

        Assert.That(renderers.Length, Is.LessThanOrEqualTo(30),
            $"{prefabPath} is too fragmented for a continuous premium-toy silhouette");
        Assert.That(materials, Is.Not.Empty);
        Assert.That(materials.All(material => material.shader.name == "Universal Render Pipeline/Simple Lit"), Is.True,
            $"{prefabPath} review materials must all use the mobile-friendly persisted URP lighting setup");
    }

    [TestCase(GirlPrefabPath)]
    [TestCase(BoyPrefabPath)]
    public void FacialOverlayRenderersDoNotCastArtifactShadows(string prefabPath)
    {
        var overlays = LoadPrefab(prefabPath).GetComponentsInChildren<Renderer>(true)
            .Where(renderer => new[] { "Whites", "Irises", "Pupils", "Glints", "Brows", "Cheeks", "Smile" }
                .Any(token => renderer.name.Contains(token, StringComparison.Ordinal)))
            .ToArray();

        Assert.That(overlays, Is.Not.Empty);
        Assert.That(overlays.All(renderer => renderer.shadowCastingMode == ShadowCastingMode.Off), Is.True);
    }

    [Test]
    public void InEngineReviewContainsMeaningfulPrefabCoverage()
    {
        Assert.That(File.Exists(InEngineReviewPath), Is.True);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.That(texture.LoadImage(File.ReadAllBytes(InEngineReviewPath)), Is.True);
            Assert.That(texture.width, Is.EqualTo(1024));
            Assert.That(texture.height, Is.EqualTo(1024));
            var sampled = 0;
            var meaningful = 0;
            var foreground = 0;
            var background = texture.GetPixel(8, 8).linear;
            for (var y = 0; y < texture.height; y += 8)
            for (var x = 0; x < texture.width; x += 8)
            {
                sampled++;
                var pixel = texture.GetPixel(x, y).linear;
                if (Mathf.Max(pixel.r, pixel.g, pixel.b) > 0.08f)
                    meaningful++;
                if (Mathf.Abs(pixel.r - background.r) + Mathf.Abs(pixel.g - background.g) +
                    Mathf.Abs(pixel.b - background.b) > 0.08f)
                    foreground++;
            }
            Assert.That(meaningful / (float)sampled, Is.GreaterThan(0.03f));
            Assert.That(foreground / (float)sampled, Is.GreaterThan(0.03f),
                "In-engine review contains only the clear color and no visible prefab geometry");
            var pixels = texture.GetPixels32();
            var mintPixels = pixels.Count(pixel => pixel.g > pixel.r * 1.12f && pixel.g > pixel.b * 1.05f);
            var lilacPixels = pixels.Count(pixel => pixel.b > pixel.g * 1.12f && pixel.r > pixel.g * 1.12f);
            Assert.That(mintPixels / (float)pixels.Length, Is.GreaterThan(0.01f), "Mint suit is not legible in the prefab capture");
            Assert.That(lilacPixels / (float)pixels.Length, Is.GreaterThan(0.01f), "Lilac suit/bow is not legible in the prefab capture");

            var cornerLuminance = new[]
            {
                texture.GetPixel(8, 8).linear,
                texture.GetPixel(texture.width - 9, 8).linear,
                texture.GetPixel(8, texture.height - 9).linear,
                texture.GetPixel(texture.width - 9, texture.height - 9).linear,
            }.Average(pixel => (pixel.r + pixel.g + pixel.b) / 3f);
            Assert.That(cornerLuminance, Is.GreaterThan(0.08f),
                "In-engine review background/exposure is too dark to judge the persistent prefab materials");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static GameObject LoadModel(string path)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(model, Is.Not.Null, $"Missing model {path}");
        return model!;
    }

    private static GameObject LoadPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null, $"Missing prefab {path}");
        return prefab!;
    }

    private static IReadOnlyDictionary<string, Transform> BoneMap(GameObject root)
    {
        var armatureRoots = root.GetComponentsInChildren<Transform>(true)
            .Where(transform => transform.name == "AlbaHumanoidRig")
            .ToArray();
        Assert.That(armatureRoots, Has.Length.EqualTo(1), root.name);
        return armatureRoots[0].GetComponentsInChildren<Transform>(true)
            .Where(transform => transform != armatureRoots[0])
            .ToDictionary(transform => transform.name, StringComparer.Ordinal);
    }

    private static float RendererHeight(GameObject prefab)
    {
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty, prefab.name);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return bounds.size.y;
    }
}
