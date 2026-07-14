#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace AlbaWorld.Editor;

public static class CharacterAssetSetup
{
    private const string GirlModelPath = "Assets/Art3D/Characters/Models/body-girl.fbx";
    private const string BoyModelPath = "Assets/Art3D/Characters/Models/body-boy.fbx";
    private const string GirlPrefabPath = "Assets/Art3D/Characters/Prefabs/BodyGirl.prefab";
    private const string BoyPrefabPath = "Assets/Art3D/Characters/Prefabs/BodyBoy.prefab";
    private const string AtlasPath = "Assets/Art3D/Characters/Textures/character-skin-atlas.png";
    private const string SkinMaterialPath = "Assets/Art3D/Characters/Materials/CharacterSkin.mat";

    private static readonly (string humanName, string boneName)[] HumanoidBones =
    {
        ("Hips", "Hips"),
        ("Spine", "Spine"),
        ("Chest", "Chest"),
        ("Neck", "Neck"),
        ("Head", "Head"),
        ("LeftUpperArm", "UpperArm.L"),
        ("LeftLowerArm", "LowerArm.L"),
        ("LeftHand", "Hand.L"),
        ("RightUpperArm", "UpperArm.R"),
        ("RightLowerArm", "LowerArm.R"),
        ("RightHand", "Hand.R"),
        ("LeftUpperLeg", "UpperLeg.L"),
        ("LeftLowerLeg", "LowerLeg.L"),
        ("LeftFoot", "Foot.L"),
        ("RightUpperLeg", "UpperLeg.R"),
        ("RightLowerLeg", "LowerLeg.R"),
        ("RightFoot", "Foot.R"),
    };

    [MenuItem("Alba World/Build Character Base Assets")]
    public static void Build()
    {
        RequireFile(GirlModelPath);
        RequireFile(BoyModelPath);
        RequireFile(AtlasPath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureHumanoid(GirlModelPath);
        ConfigureHumanoid(BoyModelPath);
        CreateOrUpdateSkinMaterial();
        CreateOrUpdatePaletteMaterials();
        CreatePrefab(GirlModelPath, GirlPrefabPath, "BodyGirl");
        CreatePrefab(BoyModelPath, BoyPrefabPath, "BodyBoy");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Alba World character base assets configured.");
    }

    public static void RenderReview()
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath)
            ?? throw new InvalidOperationException("Unity project root could not be resolved from Application.dataPath");
        var outputPath = Path.Combine(projectRoot, "Art", "Reviews", "Task5", "character-bases-in-engine.png");
        var girlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GirlPrefabPath);
        var boyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoyPrefabPath);
        if (girlPrefab == null || boyPrefab == null)
            throw new InvalidOperationException("Character prefabs must be built before rendering the review image");
        var previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        var previousUrpSrpBatching = urpAsset != null && urpAsset.useSRPBatcher;
        var previousSrpBatching = GraphicsSettings.useScriptableRenderPipelineBatching;
        if (urpAsset != null)
            urpAsset.useSRPBatcher = false;
        GraphicsSettings.useScriptableRenderPipelineBatching = false;
        Scene scene = default;
        RenderTexture target = null;
        Camera camera = null;
        try
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            target = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                antiAliasing = 1,
            };
            var girl = (GameObject)PrefabUtility.InstantiatePrefab(girlPrefab, scene);
            var boy = (GameObject)PrefabUtility.InstantiatePrefab(boyPrefab, scene);
            girl.transform.position = new Vector3(-0.27f, 0f, 0f);
            boy.transform.position = new Vector3(0.27f, 0f, 0f);
            PrepareReviewInstance(girl);
            PrepareReviewInstance(boy);

            var renderers = girl.GetComponentsInChildren<Renderer>(true)
                .Concat(boy.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Character prefabs contain no renderers");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            Debug.Log($"Task5 review renderers={renderers.Length} bounds center={bounds.center} size={bounds.size}");
            Debug.Log("Task5 review materials=" + string.Join(", ", renderers
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Select(material => $"{material.name}:{material.shader.name}")
                .Distinct()
                .OrderBy(value => value)));
            Debug.Log("Task5 review assignments=" + string.Join("; ", renderers
                .Select(renderer => $"{renderer.name}={string.Join("+", renderer.sharedMaterials.Where(material => material != null).Select(material => material.name))}")
                .OrderBy(value => value)));

            var cameraObject = new GameObject("Task5 In-Engine Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.43f, 0.50f, 0.66f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 50f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            var direction = new Vector3(0.28f, 0.10f, 1f).normalized;
            var halfSize = Mathf.Max(bounds.extents.x, bounds.extents.y);
            var distance = halfSize / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) + bounds.extents.z + 0.35f;
            cameraObject.transform.position = bounds.center + direction * distance;
            cameraObject.transform.LookAt(bounds.center + Vector3.up * 0.01f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.31f, 0.34f, 1f);
            CreateReviewLight(scene, "Key", new Vector3(-1.8f, 2.8f, 2.4f), 0.78f, new Color(1f, 0.97f, 0.94f));
            CreateReviewLight(scene, "Fill", new Vector3(2.1f, 1.7f, 1.2f), 0.30f, new Color(0.86f, 0.93f, 1f));
            CreateReviewLight(scene, "Rim", new Vector3(0f, 2.4f, -2.0f), 0.30f, new Color(0.94f, 0.90f, 1f));

            if (!target.Create())
                throw new InvalidOperationException("Failed to create the Task5 review RenderTexture");
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var screenshot = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            try
            {
                screenshot.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                screenshot.Apply();
                var pixels = screenshot.GetPixels32();
                var meaningful = pixels.Count(pixel => Mathf.Max(pixel.r, pixel.g, pixel.b) > 72);
                var coverage = meaningful / (float)pixels.Length;
                if (coverage <= 0.03f)
                    throw new InvalidOperationException($"In-engine review is empty or nearly black (coverage={coverage:P2})");
                var background = pixels[8 * 1024 + 8];
                var foreground = pixels.Count(pixel =>
                    Mathf.Abs(pixel.r - background.r) + Mathf.Abs(pixel.g - background.g) +
                    Mathf.Abs(pixel.b - background.b) > 24);
                var foregroundCoverage = foreground / (float)pixels.Length;
                if (foregroundCoverage <= 0.03f)
                    throw new InvalidOperationException($"In-engine review contains only the clear color (foreground={foregroundCoverage:P2})");
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
                Debug.Log($"Character in-engine review rendered to {Path.GetFullPath(outputPath)} (coverage={coverage:P2}, foreground={foregroundCoverage:P2})");
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(screenshot);
            }
        }
        finally
        {
            if (urpAsset != null)
                urpAsset.useSRPBatcher = previousUrpSrpBatching;
            GraphicsSettings.useScriptableRenderPipelineBatching = previousSrpBatching;
            if (camera != null)
                camera.targetTexture = null;
            if (previousSceneSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);
            else if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }

    private static void PrepareReviewInstance(GameObject instance)
    {
        instance.SetActive(true);
        var animator = instance.GetComponent<Animator>();
        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
        }
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            renderer.forceRenderingOff = false;
            if (renderer is SkinnedMeshRenderer skinned)
                skinned.updateWhenOffscreen = true;
        }
    }

    private static void CreateReviewLight(Scene scene, string name, Vector3 position, float intensity, Color color)
    {
        var lightObject = new GameObject($"Task5 {name} Light");
        SceneManager.MoveGameObjectToScene(lightObject, scene);
        lightObject.transform.position = position;
        lightObject.transform.LookAt(new Vector3(0f, 0.55f, 0f));
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = color;
        light.shadows = LightShadows.Soft;
    }

    private static void ConfigureHumanoid(string modelPath)
    {
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException($"No ModelImporter found for {modelPath}");

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.isReadable = false;
        importer.addCollider = false;
        importer.importBlendShapes = false;
        importer.importVisibility = false;
        importer.importConstraints = false;
        importer.preserveHierarchy = true;
        importer.useFileUnits = true;
        importer.globalScale = 1f;

        var description = importer.humanDescription;
        description.human = HumanoidBones.Select(pair => new HumanBone
        {
            humanName = pair.humanName,
            boneName = pair.boneName,
            limit = new HumanLimit { useDefaultValues = true },
        }).ToArray();
        description.upperArmTwist = 0.5f;
        description.lowerArmTwist = 0.5f;
        description.upperLegTwist = 0.5f;
        description.lowerLegTwist = 0.5f;
        description.armStretch = 0.05f;
        description.legStretch = 0.05f;
        description.feetSpacing = 0f;
        description.hasTranslationDoF = false;
        importer.humanDescription = description;
        importer.SaveAndReimport();

        var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException($"Humanoid avatar is invalid for {modelPath}");
    }

    private static void CreateOrUpdateSkinMaterial()
    {
        EnsureAssetFolder(Path.GetDirectoryName(SkinMaterialPath)!.Replace('\\', '/'));
        var shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null)
            throw new InvalidOperationException("Universal Render Pipeline/Simple Lit shader is unavailable");
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (atlas == null)
            throw new InvalidOperationException($"Skin atlas did not import: {AtlasPath}");

        var material = AssetDatabase.LoadAssetAtPath<Material>(SkinMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "CharacterSkin" };
            AssetDatabase.CreateAsset(material, SkinMaterialPath);
        }
        else
        {
            material.shader = shader;
            material.name = "CharacterSkin";
        }
        ConfigureOpaqueLitMaterial(material);
        material.SetTexture("_BaseMap", atlas);
        material.SetTexture("_MainTex", atlas);
        material.SetTextureScale("_BaseMap", new Vector2(1f / 3f, 0.5f));
        material.SetTextureOffset("_BaseMap", Vector2.zero);
        material.SetTextureScale("_MainTex", new Vector2(1f / 3f, 0.5f));
        material.SetTextureOffset("_MainTex", Vector2.zero);
        material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
        material.SetColor("_Color", Color.white);
        material.color = Color.white;
        material.SetColor("_EmissionColor", Color.black);
        material.DisableKeyword("_EMISSION");
        material.SetFloat("_Smoothness", 0.48f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
    }

    private static void CreateOrUpdatePaletteMaterials()
    {
        CreateOrUpdateSolidMaterial("SkinHighlight", new Color(0.95f, 0.60f, 0.38f, 1f), 0.46f);
        CreateOrUpdateSolidMaterial("EyeWhite", new Color(0.93f, 0.91f, 0.84f, 1f), 0.56f);
        CreateOrUpdateSolidMaterial("WarmBrownIris", new Color(0.22f, 0.075f, 0.025f, 1f), 0.58f);
        CreateOrUpdateSolidMaterial("SoftBlackPupil", new Color(0.004f, 0.003f, 0.006f, 1f), 0.72f);
        CreateOrUpdateSolidMaterial("CocoaHair", new Color(0.15f, 0.045f, 0.018f, 1f), 0.52f);
        CreateOrUpdateSolidMaterial("SoftBlush", new Color(0.92f, 0.24f, 0.30f, 1f), 0.34f);
        CreateOrUpdateSolidMaterial("WarmSmile", new Color(0.58f, 0.045f, 0.070f, 1f), 0.42f);
        CreateOrUpdateSolidMaterial("GirlBaseSuit", new Color(0.57f, 0.32f, 0.78f, 1f), 0.44f);
        CreateOrUpdateSolidMaterial("BoyBaseSuit", new Color(0.22f, 0.68f, 0.55f, 1f), 0.44f);
        CreateOrUpdateSolidMaterial("GirlHairBow", new Color(0.53f, 0.26f, 0.74f, 1f), 0.46f);
    }

    private static void CreateOrUpdateSolidMaterial(string name, Color color, float smoothness)
    {
        var path = $"Assets/Art3D/Characters/Materials/{name}.mat";
        var shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null)
            throw new InvalidOperationException("Universal Render Pipeline/Simple Lit shader is unavailable");
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
            material.name = name;
        }
        ConfigureOpaqueLitMaterial(material);
        material.SetTexture("_BaseMap", null);
        material.SetTexture("_MainTex", null);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.color = color;
        material.SetColor("_EmissionColor", Color.black);
        material.DisableKeyword("_EMISSION");
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
    }

    private static void ConfigureOpaqueLitMaterial(Material material)
    {
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cull", 2f);
        material.SetFloat("_ZWrite", 1f);
        material.renderQueue = -1;
        material.SetOverrideTag("RenderType", "Opaque");
    }

    private static void CreatePrefab(string modelPath, string prefabPath, string rootName)
    {
        EnsureAssetFolder(Path.GetDirectoryName(prefabPath)!.Replace('\\', '/'));
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
            throw new InvalidOperationException($"Model did not import: {modelPath}");
        var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isHuman)
            throw new InvalidOperationException($"Humanoid avatar is unavailable: {modelPath}");

        var instance = UnityEngine.Object.Instantiate(model);
        try
        {
            instance.name = rootName;
            var animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (IsFacialOverlay(renderer.name))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var index = 0; index < materials.Length; index++)
                {
                    var external = ExternalMaterialFor(materials[index]);
                    if (external == null)
                        continue;
                    materials[index] = external;
                    changed = true;
                }
                if (changed)
                    renderer.sharedMaterials = materials;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to create prefab: {prefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static Material ExternalMaterialFor(Material source)
    {
        if (source == null)
            return null;
        var names = new[]
        {
            "CharacterSkin", "SkinHighlight", "EyeWhite", "WarmBrownIris", "SoftBlackPupil",
            "CocoaHair", "SoftBlush", "WarmSmile", "GirlBaseSuit", "BoyBaseSuit", "GirlHairBow",
        };
        var match = names.FirstOrDefault(name => source.name.Contains(name, StringComparison.OrdinalIgnoreCase));
        if (match == null)
            return null;
        var path = match == "CharacterSkin"
            ? SkinMaterialPath
            : $"Assets/Art3D/Characters/Materials/{match}.mat";
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private static bool IsFacialOverlay(string rendererName)
    {
        var tokens = new[] { "Whites", "Irises", "Pupils", "Glints", "Brows", "Cheeks", "Smile" };
        return tokens.Any(token => rendererName.Contains(token, StringComparison.Ordinal));
    }

    private static void EnsureAssetFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            Directory.CreateDirectory(folder);
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Required character source asset is missing", path);
    }
}
#endif
