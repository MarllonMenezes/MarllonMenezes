# Alba World 3D MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the current Alba World 2D prototype into a complete offline 3D mobile MVP with original rounded Chibi Pop characters, recognizable pets and furniture, third-person exploration, room decoration, and photo mode.

**Architecture:** Build one vertical slice at a time on Unity 6.3 URP: approved concept art feeds a Blender-to-FBX pipeline, modular prefabs are registered in the existing immutable-ID catalog, and focused runtime systems replace the current monolithic 2D app. Versioned JSON remains the source of local state, with schema migration preserving the current selections and unlocked items.

**Tech Stack:** Unity 6.3.19f1, Universal Render Pipeline 17.3.0, C#/.NET, Unity Test Framework, Blender 4.5 LTS, FBX, PNG textures, Android IL2CPP/ARM64.

## Global Constraints

- Package name stays `com.albaworldgames.albaworld` and publisher stays **Alba World Games**.
- Android minimum API stays 25, target API stays 35 until submission requirements are rechecked, and the build stays ARM64/IL2CPP.
- The game remains landscape, offline, bilingual (`pt-BR` and `en`), with no account, chat, multiplayer, analytics, purchases, or direct social sharing.
- Visual direction is original **Alba Chibi Pop**: approximately 2.7 heads tall, smooth rounded meshes, recognizable anatomy and objects, and no copied PK XD characters, UI, silhouettes, or assets.
- Two child body bases share one humanoid skeleton; pets have species-specific rigs.
- A fully equipped character stays between 8,000 and 12,000 triangles; a pet stays between 4,000 and 7,000 triangles.
- At least 32 options are free and at least 8 are permanently unlockable by optional rewarded video; daily reward limit remains 2.
- The final Android package stays below 150 MB and targets stable 30 FPS on a device with 2 GB RAM.
- Existing item IDs are never reused; removed IDs require explicit migration.
- Every art asset keeps its approved concept PNG, source `.blend`, exported `.fbx`, textures, Unity prefab, and prompt/license record.

---

## Delivery Partitions

This specification spans independent subsystems. Execute it through these reviewable partitions instead of one unbroken rewrite:

1. Visual concepts and asset pipeline.
2. URP foundation, data model, and one playable character vertical slice.
3. Complete modular characters and third-person exploration.
4. Pets and pet following.
5. Rooms, furniture, and decoration.
6. UI, photo mode, migration, performance, and Android release validation.

Each task below ends in a testable deliverable and a commit. Do not begin the next art-heavy task until the user approves the visual checkpoint named in the current task.

## File and Directory Map

- `Art/Concepts/`: approved visual reference sheets and generation manifest; not loaded at runtime.
- `Art/Blender/Characters/`: body, face, hair, clothing, shoe, and accessory source `.blend` files.
- `Art/Blender/Pets/`: cat, dog, and pet-accessory source `.blend` files.
- `Art/Blender/Rooms/`: room shells, furniture, and decor source `.blend` files.
- `Tools/Blender/export_alba_asset.py`: deterministic FBX export from Blender source files.
- `Tools/Blender/validate_alba_asset.py`: headless topology, rig, naming, and material validation.
- `Assets/Art3D/Characters/`: imported character FBX files, textures, materials, and prefabs.
- `Assets/Art3D/Pets/`: imported pet FBX files, textures, materials, and prefabs.
- `Assets/Art3D/Rooms/`: imported rooms, furniture, textures, materials, and prefabs.
- `Assets/Settings/`: URP pipeline, renderer, lighting, and quality assets.
- `Assets/Scripts/Core/World3DModels.cs`: JSON-safe character, player-transform, and room-layout data.
- `Assets/Scripts/Catalog/`: 3D item metadata, catalog loading, and validation.
- `Assets/Scripts/Characters/`: modular character assembly and animation binding.
- `Assets/Scripts/Player/`: movement, input, camera, and interaction.
- `Assets/Scripts/Pets/`: pet assembly and follow behavior.
- `Assets/Scripts/Decoration/`: room loading, selection, placement, snapping, and persistence.
- `Assets/Scripts/Photo/`: photo camera, poses, UI hiding, and export coordination.
- `Assets/Scripts/Flow/`: boot and transitions between exploration, customization, decoration, and photo modes.
- `Assets/Tests/Editor/`: catalog, import, schema, and asset validation tests.
- `Assets/Tests/PlayMode/`: movement, pet, decoration, flow, and restore tests.

### Task 1: Approve the Production Concept Sheets

**Files:**
- Create: `Art/Concepts/character-girl-turnaround.png`
- Create: `Art/Concepts/character-boy-turnaround.png`
- Create: `Art/Concepts/pets-turnaround.png`
- Create: `Art/Concepts/clothing-and-accessories-board.png`
- Create: `Art/Concepts/rooms-and-furniture-board.png`
- Create: `Art/Concepts/generation-manifest.json`

**Interfaces:**
- Consumes: the approved design in `docs/superpowers/specs/2026-07-12-alba-world-3d-mvp-design.md`.
- Produces: five user-approved visual references that every Blender asset must match.

- [ ] **Step 1: Generate the girl turnaround with the image generation skill**

Use this exact production prompt and request a clean 4-view character sheet:

```text
Use case: stylized-concept
Asset type: original 3D game character turnaround reference
Primary request: design the original Alba World girl base character, child-safe and joyful, shown front, three-quarter, side, and back
Subject: super-chibi child character, exactly 2.7 heads tall, large expressive oval eyes, small nose, rounded cheeks, slightly oversized hands and shoes, smooth recognizable human anatomy
Style/medium: polished stylized 3D animation concept, rounded premium toy realism, smooth organic forms, production reference sheet
Color palette: lavender, rose, mint, cream, warm skin tones
Materials/textures: soft skin, visible cloth seams and gentle folds, smooth hair clumps, no blocky geometry
Constraints: neutral A-pose; consistent proportions in every view; plain light background; no text; no watermark; fully original design
Avoid: cubes, voxel art, photoreal adult anatomy, copied PK XD silhouettes, logos, weapons, makeup, high heels
```

- [ ] **Step 2: Generate the boy turnaround with locked shared proportions**

Use the girl sheet as a style reference and this prompt:

```text
Use case: stylized-concept
Asset type: original 3D game character turnaround reference
Primary request: design the original Alba World boy base character using the exact same height, 2.7-head proportion, hand size, shoe size, eye language, and joint placement as the approved girl
Subject: child-safe super-chibi boy shown front, three-quarter, side, and back in a neutral A-pose
Style/medium: polished stylized 3D animation concept, rounded premium toy realism, smooth recognizable human anatomy
Color palette: mint, sunny yellow, blue, cream, warm skin tones
Materials/textures: visible cloth construction, smooth hair clumps, soft skin, no blocky geometry
Constraints: shared skeleton compatibility; plain light background; no text; no watermark; fully original design
Avoid: cubes, voxel art, photoreal adult anatomy, copied game characters, logos, weapons
```

- [ ] **Step 3: Generate pets, clothing, and room boards**

Run three separate image-generation calls, one per asset, using the shared phrase `Alba Chibi Pop, rounded premium toy realism, smooth recognizable forms, original design` and these exact subjects:

```text
pets-turnaround.png: recognizable kitten and puppy, front/side/back, large heads, short legs, clear muzzles, paws, ears, tails and painted fur patterns; include bow, cap and bandana fit guides.

clothing-and-accessories-board.png: four complete child-safe outfits, four hairstyles, two pairs of shoes and four oversized accessories; show construction seams, collars, sleeves, pockets and soft fabric folds; every item isolated and compatible with 2.7-head chibi bodies.

rooms-and-furniture-board.png: sunny bedroom and garden living room plus sixteen recognizable rounded furniture/decor objects; show bed, sofa, table, chair, shelf, lamp, rug, cushion, books, picture, clock, plant and small surface decor with wood, fabric and ceramic materials.
```

- [ ] **Step 4: Record provenance and constraints**

Create `Art/Concepts/generation-manifest.json` with one object per image:

```json
{
  "project": "Alba World",
  "studio": "Alba World Games",
  "visualDirection": "Alba Chibi Pop",
  "images": [
    {
      "file": "character-girl-turnaround.png",
      "tool": "OpenAI built-in image generation",
      "role": "concept reference only",
      "approved": false,
      "constraints": ["original design", "2.7 heads tall", "smooth rounded anatomy", "no third-party assets"]
    }
  ]
}
```

Repeat the image entry for all five files; paste each final prompt into its matching entry as a JSON string.

- [ ] **Step 5: Validate and obtain the visual approval checkpoint**

Inspect all five files with `view_image`. Confirm consistent proportions, recognizable pets/furniture, no visible text or watermark, and no square placeholder forms. Show the five sheets to the user and stop until the user explicitly approves them. After approval, set every manifest entry to `"approved": true`.

- [ ] **Step 6: Commit the approved concepts**

```powershell
git add Art/Concepts
git commit -m "art: approve Alba Chibi Pop production concepts"
```

### Task 2: Install the 3D Toolchain and Configure URP

**Files:**
- Modify: `Packages/manifest.json`
- Modify: `Assets/Editor/ProjectSetup.cs`
- Create: `Assets/Editor/UrpProjectSetup.cs`
- Create: `Assets/Settings/AlbaWorldURP.asset`
- Create: `Assets/Settings/AlbaWorldRenderer.asset`
- Create: `Assets/Settings/AlbaWorldPostProcess.asset`
- Modify: `Assets/Scenes/Main.unity`
- Create: `Assets/Tests/Editor/ThreeDimensionalProjectTests.cs`

**Interfaces:**
- Consumes: Unity 6.3.19f1 installed at `D:\Unity\Hub\Editor\6000.3.19f1`.
- Produces: `UrpProjectSetup.Configure()` and a bootable URP 3D scene used by every later task.

- [ ] **Step 1: Install Blender 4.5 LTS on drive D**

Install Blender so the executable is exactly `D:\Blender\4.5\blender.exe`. Verify:

```powershell
& 'D:\Blender\4.5\blender.exe' --version
```

Expected: output starts with `Blender 4.5` and exit code is 0.

- [ ] **Step 2: Write the failing URP editor test**

Create `Assets/Tests/Editor/ThreeDimensionalProjectTests.cs`:

```csharp
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
```

- [ ] **Step 3: Run the test and verify the red state**

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -runTests -testPlatform editmode -testResults "$PWD\work\urp-red.xml" -logFile "$PWD\work\urp-red.log"
```

Expected: `UrpAndThreeDimensionalSettingsArePresent` fails because the package and settings assets are missing.

- [ ] **Step 4: Add URP 17.3.0 and create deterministic project setup**

Add this dependency to `Packages/manifest.json`:

```json
"com.unity.render-pipelines.universal": "17.3.0"
```

Create `Assets/Editor/UrpProjectSetup.cs` with this public entry point:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AlbaWorld.Editor;

public static class UrpProjectSetup
{
    public const string PipelinePath = "Assets/Settings/AlbaWorldURP.asset";
    public const string RendererPath = "Assets/Settings/AlbaWorldRenderer.asset";

    public static void Configure()
    {
        EnsureFolder("Assets/Settings");
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.name = "AlbaWorldURP";
            pipeline.shadowDistance = 20f;
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 2;
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;

        var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/AlbaWorldPostProcess.asset");
        if (volumeProfile == null)
        {
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(volumeProfile, "Assets/Settings/AlbaWorldPostProcess.asset");
        }
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", "Settings");
    }
}
#endif
```

Call `UrpProjectSetup.Configure()` from `ProjectSetup.EnsureDemoScene()` before saving assets. Replace the empty scene with a perspective camera tagged `MainCamera`, one directional light, a global volume using `AlbaWorldPostProcess.asset`, and a `WorldRoot` object. Do not add runtime-visible primitive placeholders.

- [ ] **Step 5: Run the URP setup and tests**

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -nographics -projectPath $PWD -executeMethod AlbaWorld.Editor.ProjectSetup.EnsureDemoScene -logFile "$PWD\work\urp-setup.log"
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -runTests -testPlatform editmode -testResults "$PWD\work\urp-green.xml" -logFile "$PWD\work\urp-green.log"
```

Expected: Unity exits 0, the test result reports all Edit Mode tests passed, and neither log contains `error CS` or `Minimum supported Android API`.

- [ ] **Step 6: Commit the URP foundation**

```powershell
git add Packages Assets/Editor Assets/Settings Assets/Scenes Assets/Tests/Editor
git commit -m "feat: configure Alba World URP 3D foundation"
```

### Task 3: Add the Versioned 3D Save Model

**Files:**
- Create: `Assets/Scripts/Core/World3DModels.cs`
- Modify: `Assets/Scripts/Core/SaveModels.cs`
- Modify: `Tools/CoreTests/AlbaWorld.CoreTests.csproj`
- Create: `Tools/CoreTests/World3DSaveTests.cs`

**Interfaces:**
- Consumes: existing `GameSaveData`, `SaveMigration.Upgrade(GameSaveData?)`, and immutable catalog IDs.
- Produces: `CharacterLoadoutData`, `SerializableVector3`, `PlayerWorldStateData`, `FurniturePlacementData`, `RoomLayoutData`, and schema version 3.

- [ ] **Step 1: Write failing migration and round-trip tests**

Create `Tools/CoreTests/World3DSaveTests.cs`:

```csharp
using AlbaWorld.Core;
using Xunit;

public sealed class World3DSaveTests
{
    [Fact]
    public void UpgradeMapsLegacySelectionsIntoThreeDimensionalLoadout()
    {
        var legacy = new GameSaveData
        {
            schemaVersion = 2,
            selectedSkinId = "skin.honey",
            selectedHairId = "hair.cloud",
            selectedOutfitId = "outfit.mint",
            selectedPetId = "pet.dog"
        };

        var upgraded = SaveMigration.Upgrade(legacy);

        Assert.Equal(3, upgraded.schemaVersion);
        Assert.Equal("body.girl", upgraded.character.bodyId);
        Assert.Equal("skin.honey", upgraded.character.skinId);
        Assert.Equal("hair.cloud", upgraded.character.hairId);
        Assert.Equal("outfit.mint", upgraded.character.outfitId);
        Assert.Equal("pet.dog", upgraded.pet.petId);
        Assert.Equal("room.sunny", upgraded.activeRoomId);
    }

    [Fact]
    public void UpgradeRemovesInvalidPlacementsWithoutDroppingTheRoom()
    {
        var save = new GameSaveData
        {
            rooms3D = new[]
            {
                new RoomLayoutData
                {
                    roomId = "room.sunny",
                    placements = new[]
                    {
                        new FurniturePlacementData { instanceId = "", itemId = "furniture.bed" },
                        new FurniturePlacementData { instanceId = "bed-1", itemId = "furniture.bed" }
                    }
                }
            }
        };

        var upgraded = SaveMigration.Upgrade(save);
        Assert.Single(upgraded.rooms3D);
        Assert.Single(upgraded.rooms3D[0].placements);
        Assert.Equal("bed-1", upgraded.rooms3D[0].placements[0].instanceId);
    }
}
```

- [ ] **Step 2: Run the .NET tests and verify the red state**

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test Tools\CoreTests\AlbaWorld.CoreTests.csproj --no-restore
```

Expected: compilation fails because the 3D data types and `GameSaveData.character` do not exist.

- [ ] **Step 3: Add JSON-safe 3D types**

Create `Assets/Scripts/Core/World3DModels.cs`:

```csharp
using System;
using System.Linq;

namespace AlbaWorld.Core;

[Serializable]
public sealed class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3() { }
    public SerializableVector3(float x, float y, float z) => (this.x, this.y, this.z) = (x, y, z);
}

[Serializable]
public sealed class CharacterLoadoutData
{
    public string bodyId = "body.girl";
    public string skinId = "skin.cream";
    public string faceId = "face.sunny";
    public string hairId = "hair.sunny";
    public string outfitId = "outfit.pink";
    public string shoesId = "shoes.sun";
    public string[] accessoryIds = Array.Empty<string>();
}

[Serializable]
public sealed class PetLoadoutData
{
    public string petId = "pet.cat";
    public string colorId = "petcolor.sunny";
    public string[] accessoryIds = Array.Empty<string>();
}

[Serializable]
public sealed class PlayerWorldStateData
{
    public SerializableVector3 position = new(0f, 0f, 0f);
    public float yaw;
}

[Serializable]
public sealed class FurniturePlacementData
{
    public string instanceId = string.Empty;
    public string itemId = string.Empty;
    public SerializableVector3 position = new(0f, 0f, 0f);
    public SerializableVector3 scale = new(1f, 1f, 1f);
    public float yaw;
    public string supportInstanceId = string.Empty;
    public string supportPointId = string.Empty;
}

[Serializable]
public sealed class RoomLayoutData
{
    public string roomId = "room.sunny";
    public FurniturePlacementData[] placements = Array.Empty<FurniturePlacementData>();

    public void Normalize() => placements = (placements ?? Array.Empty<FurniturePlacementData>())
        .Where(p => p != null && !string.IsNullOrWhiteSpace(p.instanceId) && !string.IsNullOrWhiteSpace(p.itemId))
        .GroupBy(p => p.instanceId)
        .Select(group => group.First())
        .ToArray();
}
```

Add both files to the explicit `<Compile>` list in `Tools/CoreTests/AlbaWorld.CoreTests.csproj`:

```xml
<Compile Include="..\..\Assets\Scripts\Core\World3DModels.cs" Link="World3DModels.cs" />
<Compile Include="World3DSaveTests.cs" />
```

- [ ] **Step 4: Migrate `GameSaveData` to schema 3**

Add these fields while retaining the legacy fields for one migration cycle:

```csharp
public CharacterLoadoutData character = new();
public PetLoadoutData pet = new();
public PlayerWorldStateData playerWorld = new();
public string activeRoomId = "room.sunny";
public RoomLayoutData[] rooms3D = Array.Empty<RoomLayoutData>();
```

Set `CurrentSchemaVersion = 3`. In `SaveMigration.Upgrade`, when `schemaVersion < 3`, copy the seven legacy selection fields into `character` and `pet`, create missing objects, normalize every room, remove duplicate room IDs by keeping the first, and finally set `schemaVersion` to 3.

- [ ] **Step 5: Run the core and Unity tests**

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test Tools\CoreTests\AlbaWorld.CoreTests.csproj --no-restore
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -runTests -testPlatform editmode -testResults "$PWD\work\save3d-green.xml" -logFile "$PWD\work\save3d-green.log"
```

Expected: all .NET and Unity Edit Mode tests pass.

- [ ] **Step 6: Commit the schema migration**

```powershell
git add Assets/Scripts/Core Tools/CoreTests
git commit -m "feat: add versioned 3D world save model"
```

### Task 4: Replace the Color Catalog with 3D Asset Metadata

**Files:**
- Create: `Assets/Scripts/Catalog/EquipmentEnums.cs`
- Create: `Assets/Scripts/Catalog/ItemVisual3D.cs`
- Create: `Assets/Scripts/Catalog/ItemCatalog3D.cs`
- Create: `Assets/Scripts/Catalog/CatalogValidation.cs`
- Modify: `Assets/Scripts/Core/GameContracts.cs`
- Modify: `Assets/Scripts/Runtime/RuntimeCatalog.cs`
- Create: `Assets/Editor/AlbaCatalogBuilder.cs`
- Create: `Assets/Tests/Editor/ItemCatalog3DTests.cs`

**Interfaces:**
- Consumes: the existing `ItemDefinition.itemId`, `ItemCategory`, translation keys, free/rewarded flag, and schema-3 IDs.
- Produces: `IItemCatalog3D.GetVisual(string id)`, `ItemVisual3D`, `PlacementRules`, and a generated asset at `Assets/Resources/Data/AlbaItemCatalog3D.asset`.

- [ ] **Step 1: Write failing catalog validation tests**

Create `Assets/Tests/Editor/ItemCatalog3DTests.cs`:

```csharp
using System.Linq;
using AlbaWorld.Catalog;
using NUnit.Framework;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class ItemCatalog3DTests
{
    [Test]
    public void ValidationRejectsDuplicateIds()
    {
        var catalog = ScriptableObject.CreateInstance<ItemCatalog3D>();
        catalog.items.Add(ItemVisual3D.TestOnly("hair.sunny"));
        catalog.items.Add(ItemVisual3D.TestOnly("hair.sunny"));

        var errors = CatalogValidation.Validate(catalog, requirePrefabs: false);
        Assert.That(errors.Any(error => error.Contains("Duplicate item ID: hair.sunny")), Is.True);
    }

    [Test]
    public void GeneratedCatalogKeepsFreeAndRewardedMinimums()
    {
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.items.Count(item => item.definition.free), Is.GreaterThanOrEqualTo(32));
        Assert.That(catalog.items.Count(item => !item.definition.free), Is.GreaterThanOrEqualTo(8));
    }
}
```

- [ ] **Step 2: Run the tests and verify the red state**

Run the Unity Edit Mode command from Task 2 with result file `work/catalog3d-red.xml`.

Expected: compilation fails because namespace `AlbaWorld.Catalog` does not exist.

- [ ] **Step 3: Define the exact 3D metadata types**

Create `Assets/Scripts/Catalog/EquipmentEnums.cs`:

```csharp
using System;

namespace AlbaWorld.Catalog;

public enum EquipmentSlot { None, Body, Face, Hair, Outfit, Shoes, Head, FaceAccessory, Back, Hand, Pet, PetAccessory }
public enum PlacementKind { None, Floor, Surface, Wall }

[Flags]
public enum BodyCompatibility
{
    None = 0,
    Girl = 1,
    Boy = 2,
    Both = Girl | Boy
}

[Serializable]
public sealed class PlacementRules
{
    public PlacementKind kind = PlacementKind.None;
    public float minimumScale = 0.8f;
    public float maximumScale = 1.2f;
    public float rotationStep = 45f;
    public bool canHostSmallObjects;
}
```

Extend `ItemCategory` without reordering existing serialized values by appending `Body`, `Face`, and `PetColor` after `Decor`. The builder assigns those categories to the new body, face, and pet-color IDs.

Create `Assets/Scripts/Catalog/ItemVisual3D.cs` with these serialized fields:

```csharp
using AlbaWorld.Game;
using UnityEngine;

namespace AlbaWorld.Catalog;

[CreateAssetMenu(menuName = "Alba World/3D Item Visual", fileName = "ItemVisual3D")]
public sealed class ItemVisual3D : ScriptableObject
{
    public ItemDefinition definition = null!;
    public GameObject prefab = null!;
    public GameObject girlPrefabOverride = null!;
    public GameObject boyPrefabOverride = null!;
    public EquipmentSlot equipmentSlot;
    public BodyCompatibility compatibleBodies = BodyCompatibility.Both;
    public PlacementRules placement = new();

    public string ItemId => definition == null ? string.Empty : definition.itemId;

    public GameObject PrefabForBody(string bodyId) => bodyId switch
    {
        "body.girl" when girlPrefabOverride != null => girlPrefabOverride,
        "body.boy" when boyPrefabOverride != null => boyPrefabOverride,
        _ => prefab
    };

#if UNITY_EDITOR
    public static ItemVisual3D TestOnly(string id)
    {
        var definition = CreateInstance<ItemDefinition>();
        definition.itemId = id;
        var visual = CreateInstance<ItemVisual3D>();
        visual.definition = definition;
        return visual;
    }
#endif
}
```

Create `ItemCatalog3D` implementing:

```csharp
public interface IItemCatalog3D
{
    ItemVisual3D? GetVisual(string id);
    IEnumerable<ItemVisual3D> ByCategory(ItemCategory category);
}
```

Use a lazily rebuilt ordinal dictionary and never silently overwrite duplicate IDs.

- [ ] **Step 4: Add deterministic catalog generation and validation**

Create `CatalogValidation.Validate(ItemCatalog3D catalog, bool requirePrefabs)` returning `IReadOnlyList<string>`. It must report empty IDs, duplicates, missing definitions, missing prefabs when strict, invalid scale ranges, and rotation steps that do not evenly divide 360.

Create `AlbaCatalogBuilder.Build()` as a Unity menu/command-line method. It creates or updates definitions and visuals for every ID currently added by `RuntimeCatalog`, plus `body.girl`, `body.boy`, `face.sunny`, `face.sparkle`, `face.calm`, `face.happy`, `petcolor.sunny`, and `petcolor.cocoa`. Preserve every existing ID and free/rewarded flag. Store assets under `Assets/Resources/Data/Definitions` and `Assets/Resources/Data/Visuals` and the catalog at `Assets/Resources/Data/AlbaItemCatalog3D.asset`.

- [ ] **Step 5: Generate the catalog and run tests**

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -nographics -projectPath $PWD -executeMethod AlbaWorld.Editor.AlbaCatalogBuilder.Build -logFile "$PWD\work\catalog-build.log"
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -runTests -testPlatform editmode -testResults "$PWD\work\catalog3d-green.xml" -logFile "$PWD\work\catalog3d-green.log"
```

Expected: catalog tests pass, generated definitions contain unique immutable IDs, at least 32 free items, and at least 8 rewarded items. Prefabs may remain empty until their production task, so the builder uses `requirePrefabs: false`; Android builds will later use strict validation.

- [ ] **Step 6: Commit the catalog model**

```powershell
git add Assets/Scripts/Catalog Assets/Scripts/Core/GameContracts.cs Assets/Scripts/Runtime/RuntimeCatalog.cs Assets/Editor/AlbaCatalogBuilder.cs Assets/Resources/Data Assets/Tests/Editor
git commit -m "feat: add asset-driven 3D item catalog"
```

### Task 5: Build and Validate the Shared Character Bases in Blender

**Files:**
- Create: `Art/Blender/Characters/alba-character-bases.blend`
- Create: `Tools/Blender/export_alba_asset.py`
- Create: `Tools/Blender/validate_alba_asset.py`
- Create: `Assets/Art3D/Characters/Models/body-girl.fbx`
- Create: `Assets/Art3D/Characters/Models/body-boy.fbx`
- Create: `Assets/Art3D/Characters/Textures/character-skin-atlas.png`
- Create: `Assets/Art3D/Characters/Materials/CharacterSkin.mat`
- Create: `Assets/Art3D/Characters/Prefabs/BodyGirl.prefab`
- Create: `Assets/Art3D/Characters/Prefabs/BodyBoy.prefab`
- Create: `Assets/Tests/Editor/CharacterImportTests.cs`

**Interfaces:**
- Consumes: approved girl and boy concept sheets from Task 1.
- Produces: FBX roots named `BodyGirl` and `BodyBoy`, both bound to armature `AlbaHumanoidRig` with the exact bone list consumed by Task 6.

- [ ] **Step 1: Create the Blender validation script before modeling**

Create `Tools/Blender/validate_alba_asset.py` with a command-line contract `--profile character|pet|furniture` and these character checks:

```python
import argparse
import bpy
import sys

REQUIRED_HUMANOID_BONES = {
    "Root", "Hips", "Spine", "Chest", "Neck", "Head",
    "UpperArm.L", "LowerArm.L", "Hand.L", "UpperArm.R", "LowerArm.R", "Hand.R",
    "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R"
}

def validate_character() -> list[str]:
    errors: list[str] = []
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1 or armatures[0].name != "AlbaHumanoidRig":
        errors.append("character requires exactly one AlbaHumanoidRig armature")
        return errors
    missing = REQUIRED_HUMANOID_BONES - set(armatures[0].data.bones.keys())
    if missing:
        errors.append("missing bones: " + ", ".join(sorted(missing)))
    triangles = sum(len(poly.vertices) - 2 for obj in bpy.context.scene.objects if obj.type == "MESH" for poly in obj.data.polygons)
    if not 4000 <= triangles <= 12000:
        errors.append(f"character triangle count {triangles} outside 4000..12000")
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH" and not any(mod.type == "ARMATURE" for mod in obj.modifiers):
            errors.append(f"mesh {obj.name} is not bound to an armature")
    return errors

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--profile", required=True, choices=["character", "pet", "furniture"])
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    errors = validate_character() if args.profile == "character" else []
    for error in errors:
        print("ERROR:", error)
    raise SystemExit(1 if errors else 0)
```

- [ ] **Step 2: Model the two smooth 2.7-head bodies**

In one `.blend` file, model both bodies at 1.05 m total Unity height with matching joint positions. Use subdivision-ready organic topology, smooth shading, separate eyes, and recognizable fingers represented as a mitten-like hand with a thumb. Keep the head-to-body ratio at 1:2.7, use no cube-shaped final forms, and keep genital anatomy absent. Create six skin swatches in one 1024 × 1024 atlas.

- [ ] **Step 3: Create and bind the shared rig**

Create exactly one armature data layout named `AlbaHumanoidRig` using the validator bone names. Duplicate the rig object only for export scenes; do not change rest pose between bodies. Skin both bodies, test shoulders, hips, knees, hands, and feet at the extremes used by walk and photo poses, and correct any weight crossing the center line.

- [ ] **Step 4: Add deterministic FBX export**

Create `Tools/Blender/export_alba_asset.py` exposing `export_collection(collection_name: str, output_path: str)`. Apply transforms, export selected mesh and armature only, use `-Z` forward, `Y` up, leaf bones disabled, animation disabled, and FBX unit scale 1.0. Export `BodyGirl` and `BodyBoy` collections to their exact Task file paths.

- [ ] **Step 5: Validate Blender files headlessly**

```powershell
& 'D:\Blender\4.5\blender.exe' -b 'Art\Blender\Characters\alba-character-bases.blend' --python 'Tools\Blender\validate_alba_asset.py' -- --profile character
```

Expected: exit 0, no `ERROR:` lines, and triangle count within the character budget.

- [ ] **Step 6: Write and run Unity import tests**

Create `CharacterImportTests.cs` that loads both FBX roots, asserts their `ModelImporter.animationType == ModelImporterAnimationType.Human`, verifies their prefabs have an `Animator`, verifies identical humanoid bone names, and checks renderer bounds heights differ by no more than 0.02 m. Run Edit Mode tests and expect all to pass.

- [ ] **Step 7: Obtain the 3D base approval checkpoint**

Render front, side, back, and a neutral in-engine view of both bodies. Show the renders to the user and stop until the user explicitly confirms the models are smooth, recognizable, non-blocky, and faithful to the approved concepts.

- [ ] **Step 8: Commit the character bases**

```powershell
git add Art/Blender/Characters Tools/Blender Assets/Art3D/Characters Assets/Tests/Editor/CharacterImportTests.cs
git commit -m "art: add approved shared-rig character bases"
```

### Task 6: Produce Modular Character Parts and Runtime Assembly

**Files:**
- Create: `Art/Blender/Characters/alba-character-parts.blend`
- Create: `Assets/Art3D/Characters/Models/Hair/*.fbx`
- Create: `Assets/Art3D/Characters/Models/Outfits/*.fbx`
- Create: `Assets/Art3D/Characters/Models/Shoes/*.fbx`
- Create: `Assets/Art3D/Characters/Models/Accessories/*.fbx`
- Create: `Assets/Art3D/Characters/Prefabs/Parts/*.prefab`
- Create: `Assets/Scripts/Characters/CharacterSlot.cs`
- Create: `Assets/Scripts/Characters/CharacterAssembler.cs`
- Create: `Assets/Scripts/Characters/CharacterAppearanceController.cs`
- Create: `Assets/Tests/Helpers/CharacterTestFactory.cs`
- Create: `Assets/Tests/PlayMode/CharacterAssemblerTests.cs`

**Interfaces:**
- Consumes: `ItemCatalog3D`, `CharacterLoadoutData`, `BodyCompatibility`, and the shared `AlbaHumanoidRig`.
- Produces: `CharacterAssembler.Apply(CharacterLoadoutData loadout)`, `CharacterAssembler.Equip(string itemId)`, and `CharacterAppearanceController.SetSkin(string skinId)`.

- [ ] **Step 1: Write the failing Play Mode assembly test**

```csharp
using System.Collections;
using AlbaWorld.Characters;
using AlbaWorld.Core;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AlbaWorld.Tests;

public sealed class CharacterAssemblerTests
{
    [UnityTest]
    public IEnumerator ApplyingLoadoutCreatesOneObjectPerExclusiveSlot()
    {
        var assembler = CharacterTestFactory.CreateAssembler();
        assembler.Apply(new CharacterLoadoutData
        {
            bodyId = "body.girl",
            hairId = "hair.sunny",
            outfitId = "outfit.pink",
            shoesId = "shoes.sun"
        });
        yield return null;

        Assert.That(assembler.ActiveCount(CharacterSlot.Body), Is.EqualTo(1));
        Assert.That(assembler.ActiveCount(CharacterSlot.Hair), Is.EqualTo(1));
        Assert.That(assembler.ActiveCount(CharacterSlot.Outfit), Is.EqualTo(1));
        Assert.That(assembler.ActiveCount(CharacterSlot.Shoes), Is.EqualTo(1));
    }
}
```

- [ ] **Step 2: Run Play Mode tests and verify the red state**

Run Unity with `-runTests -testPlatform playmode`, result `work/character-assembly-red.xml`.

Expected: compilation fails because `CharacterAssembler` and `CharacterSlot` do not exist.

- [ ] **Step 3: Model the approved modular parts**

Produce at least five preserved hair IDs (`hair.sunny`, `hair.bubble`, `hair.rainbow`, `hair.cloud`, `hair.mint`), five preserved outfit IDs, three preserved shoe IDs, and four preserved human accessories. Model collars, sleeves, seams, pockets, soles, and controlled fabric folds. Fit every allowed part to both body rigs without changing bone positions. Create body-specific mesh variants only when the silhouette requires them, while keeping one item ID and one catalog visual with two prefab references.

Create `CharacterTestFactory.CreateAssembler()` using temporary in-memory body/part prefabs and an in-memory `ItemCatalog3D`; the helper must not depend on production FBX files so assembly behavior remains isolated from import tests.

- [ ] **Step 4: Implement exclusive slots and shared-skeleton binding**

Define:

```csharp
namespace AlbaWorld.Characters;

public enum CharacterSlot { Body, Face, Hair, Outfit, Shoes, Head, FaceAccessory, Back, Hand }
```

`CharacterAssembler.Apply` must clear current children, resolve body first, instantiate parts allowed by `BodyCompatibility`, bind each `SkinnedMeshRenderer.bones` to the active body by bone name, and reject unknown/incompatible IDs without throwing. `Equip` replaces only the target exclusive slot; accessory slots may hold one item per named accessory subtype.

- [ ] **Step 5: Run topology, clipping, and runtime tests**

Validate the `.blend` file headlessly, then run Edit Mode and Play Mode tests. Add pose snapshots for arms down, arms raised, walk stride, sit, and both photo poses. The combined equipped triangle count must remain 8,000–12,000 and no major skin penetration may be visible at the camera distance used in gameplay.

- [ ] **Step 6: Obtain the dressed-character approval checkpoint**

Show in-engine renders of both bodies wearing every outfit family, plus a grid of hairstyles, shoes, and accessories. Stop until the user approves the recognizable clothing construction and non-blocky finish.

- [ ] **Step 7: Commit modular characters**

```powershell
git add Art/Blender/Characters Assets/Art3D/Characters Assets/Scripts/Characters Assets/Tests/PlayMode Assets/Resources/Data
git commit -m "feat: add modular 3D character customization"
```

### Task 7: Add Third-Person Movement, Camera, and Interaction

**Files:**
- Create: `Assets/Scripts/Player/IPlayerInput.cs`
- Create: `Assets/Scripts/Player/DesktopPlayerInput.cs`
- Create: `Assets/Scripts/Player/MobileJoystickInput.cs`
- Create: `Assets/Scripts/Player/ThirdPersonController.cs`
- Create: `Assets/Scripts/Player/ThirdPersonCamera.cs`
- Create: `Assets/Scripts/Player/PlayerAnimatorBridge.cs`
- Create: `Assets/Scripts/Player/InteractionTarget.cs`
- Create: `Assets/Scripts/Player/InteractionScanner.cs`
- Create: `Assets/UI/Prefabs/MobileControls.prefab`
- Create: `Assets/Tests/Helpers/PlayerTestFactory.cs`
- Create: `Assets/Tests/PlayMode/ThirdPersonControllerTests.cs`
- Modify: `Assets/Scenes/Main.unity`

**Interfaces:**
- Consumes: assembled character root with `Animator`, Unity `CharacterController`, and `PlayerWorldStateData`.
- Produces: `IPlayerInput.Move`, `IPlayerInput.Look`, `ThirdPersonController.Simulate(float deltaTime)`, `ThirdPersonCamera.SetTarget(Transform target)`, and `InteractionScanner.TryInteract()`.

- [ ] **Step 1: Write a deterministic failing movement test**

Create `ThirdPersonControllerTests.cs` using a fake input:

```csharp
using System.Collections;
using AlbaWorld.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AlbaWorld.Tests;

public sealed class ThirdPersonControllerTests
{
    private sealed class FixedInput : IPlayerInput
    {
        public Vector2 Move { get; set; }
        public Vector2 Look => Vector2.zero;
        public bool InteractPressed => false;
    }

    [UnityTest]
    public IEnumerator ForwardInputMovesCharacterAndKeepsItGrounded()
    {
        var input = new FixedInput { Move = Vector2.up };
        var controller = PlayerTestFactory.CreateController(input);
        var before = controller.transform.position;
        for (var i = 0; i < 30; i++) controller.Simulate(1f / 30f);
        yield return null;

        Assert.That(controller.transform.position.z, Is.GreaterThan(before.z + 1f));
        Assert.That(Mathf.Abs(controller.transform.position.y - before.y), Is.LessThan(0.05f));
    }
}
```

- [ ] **Step 2: Run Play Mode tests and verify the red state**

Expected: compilation fails because `IPlayerInput` and `ThirdPersonController` do not exist.

- [ ] **Step 3: Implement injectable movement input**

Create:

```csharp
using UnityEngine;

namespace AlbaWorld.Player;

public interface IPlayerInput
{
    Vector2 Move { get; }
    Vector2 Look { get; }
    bool InteractPressed { get; }
}
```

`DesktopPlayerInput` reads WASD/arrows and mouse only outside mobile builds. `MobileJoystickInput` reads two UGUI drag zones and one interaction button. Both normalize diagonal input. Do not use network or third-party controller packages.

- [ ] **Step 4: Implement character-relative motion and animation**

`ThirdPersonController` accepts an `IPlayerInput` through `Initialize(IPlayerInput input, Transform cameraTransform)`. `Simulate` converts input to camera-relative planar motion, accelerates toward 2.8 m/s walk speed or 4.2 m/s run speed, rotates at 540 degrees/second, applies gravity, and calls `CharacterController.Move`. `PlayerAnimatorBridge` writes `Speed`, `Grounded`, and `Turn` parameters without assuming a particular avatar body.

Create `PlayerTestFactory.CreateController(IPlayerInput input)` with a flat test floor, a `CharacterController`, an identity camera transform, and a controller initialized through the public interface.

- [ ] **Step 5: Implement collision-aware orbit camera**

`ThirdPersonCamera` uses yaw, pitch clamped to -15..55 degrees, target height 0.72 m, distance 3.4 m, minimum distance 1.2 m, and a sphere cast radius 0.18 m against environment layers. Camera movement uses unscaled smoothing so opening UI does not produce a jump.

- [ ] **Step 6: Add interaction targets**

`InteractionTarget` exposes an ID, localized label key, and `UnityEvent`. `InteractionScanner` performs a 1.6 m sphere overlap, filters by forward angle, selects the closest valid target, and calls it only on an input edge. Add one door target and one seat target to the vertical-slice room.

- [ ] **Step 7: Run movement tests at both frame rates**

Add a second case simulating 60 steps at 1/60 second and assert the final position differs from the 30 FPS case by less than 0.08 m. Run all Play Mode tests and manually verify WASD/mouse and mobile joystick controls in Game view at 16:9 and 20:9.

- [ ] **Step 8: Commit exploration controls**

```powershell
git add Assets/Scripts/Player Assets/UI/Prefabs Assets/Scenes/Main.unity Assets/Tests/PlayMode
git commit -m "feat: add third-person exploration controls"
```

### Task 8: Build Recognizable Pets and Following Behavior

**Files:**
- Create: `Art/Blender/Pets/alba-cat.blend`
- Create: `Art/Blender/Pets/alba-dog.blend`
- Create: `Art/Blender/Pets/alba-pet-accessories.blend`
- Create: `Assets/Art3D/Pets/Models/*.fbx`
- Create: `Assets/Art3D/Pets/Textures/pets-atlas.png`
- Create: `Assets/Art3D/Pets/Prefabs/Cat.prefab`
- Create: `Assets/Art3D/Pets/Prefabs/Dog.prefab`
- Create: `Assets/Art3D/Pets/Prefabs/Accessories/*.prefab`
- Create: `Assets/Scripts/Pets/PetAssembler.cs`
- Create: `Assets/Scripts/Pets/IWalkableArea.cs`
- Create: `Assets/Scripts/Pets/PetFollower.cs`
- Create: `Assets/Scripts/Pets/PetAnimatorBridge.cs`
- Modify: `Tools/Blender/validate_alba_asset.py`
- Create: `Assets/Tests/Helpers/PetTestFactory.cs`
- Create: `Assets/Tests/PlayMode/PetFollowerTests.cs`

**Interfaces:**
- Consumes: `PetLoadoutData`, `IItemCatalog3D`, active player transform, and room walkable bounds.
- Produces: `PetAssembler.Apply(PetLoadoutData loadout)` and `PetFollower.Initialize(Transform target, IWalkableArea area)`.

- [ ] **Step 1: Write failing follow and recovery tests**

```csharp
[UnityTest]
public IEnumerator PetApproachesTargetButStopsAtFollowDistance()
{
    var pair = PetTestFactory.Create(targetPosition: new Vector3(4f, 0f, 0f));
    for (var i = 0; i < 120; i++) pair.Follower.Simulate(1f / 30f);
    yield return null;
    var distance = Vector3.Distance(pair.Pet.position, pair.Target.position);
    Assert.That(distance, Is.InRange(1.1f, 1.7f));
}

[Test]
public void PetTeleportsToSafePointWhenTooFarAway()
{
    var pair = PetTestFactory.Create(targetPosition: Vector3.zero);
    pair.Pet.position = new Vector3(20f, 0f, 0f);
    pair.Follower.Simulate(1f / 30f);
    Assert.That(Vector3.Distance(pair.Pet.position, pair.Target.position), Is.LessThan(3f));
}
```

- [ ] **Step 2: Model cat and dog against the approved turnaround**

Create smooth, recognizable muzzles, ears, paws, leg joints, tails, chest volumes, and painted fur boundaries. Keep each equipped pet at 4,000–7,000 triangles. Use one 1024 × 1024 atlas per species or one shared atlas only if compression tests show lower memory. Add named accessory anchors `PetHead`, `PetNeck`, and `PetBack`.

- [ ] **Step 3: Rig and animate each species**

Each species gets one stable rig and clips `Idle`, `Walk`, `Run`, `Sit`, and `Happy`. Root motion stays disabled. Validate paws remain planted during idle/sit and tails do not penetrate the torso in the gameplay camera.

Extend `validate_alba_asset.py` with `validate_pet()`: require exactly one armature, require bones `Root`, `Spine`, `Neck`, `Head`, four upper/lower leg chains, and `Tail.01`, require 4,000–7,000 triangles, require `PetHead`, `PetNeck`, and `PetBack` anchor empties, and dispatch this function when `--profile pet` is selected.

Define `IWalkableArea` as `Vector3 ClosestSafePoint(Vector3 desiredPosition)` and `bool Contains(Vector3 position)`. In Task 9, `RoomBounds` implements this interface.

- [ ] **Step 4: Implement assembly and follow simulation**

`PetAssembler` resolves the pet prefab, applies the color material property, and equips only compatible anchors. `PetFollower.Simulate` starts moving beyond 1.7 m, stops below 1.3 m, runs beyond 4 m, and teleports to `IWalkableArea.ClosestSafePoint(target.position - target.forward)` beyond 12 m or after 2 seconds without progress.

Create `PetTestFactory.Create(Vector3 targetPosition)` with fake walkable bounds returning the closest point inside a 6 m square and a follower initialized through its public interface.

- [ ] **Step 5: Validate, test, and obtain pet approval**

Run Blender validation with `--profile pet`, Unity import tests, and all Play Mode tests. Show cat and dog front/side/in-engine views with every accessory and stop until the user confirms they look like real stylized animals rather than blocks.

- [ ] **Step 6: Commit pets**

```powershell
git add Art/Blender/Pets Assets/Art3D/Pets Assets/Scripts/Pets Assets/Tests/PlayMode Assets/Resources/Data
git commit -m "feat: add animated pets and follow behavior"
```

### Task 9: Build the Two Rooms and Recognizable Furniture Set

**Files:**
- Create: `Art/Blender/Rooms/sunny-bedroom.blend`
- Create: `Art/Blender/Rooms/garden-living-room.blend`
- Create: `Art/Blender/Rooms/alba-furniture.blend`
- Create: `Assets/Art3D/Rooms/Models/*.fbx`
- Create: `Assets/Art3D/Rooms/Textures/*.png`
- Create: `Assets/Art3D/Rooms/Prefabs/SunnyBedroom.prefab`
- Create: `Assets/Art3D/Rooms/Prefabs/GardenLivingRoom.prefab`
- Create: `Assets/Art3D/Rooms/Prefabs/Furniture/*.prefab`
- Create: `Assets/Scripts/Decoration/RoomDefinition.cs`
- Create: `Assets/Scripts/Decoration/PlaceableItem.cs`
- Create: `Assets/Scripts/Decoration/SupportPoint.cs`
- Create: `Assets/Scripts/Decoration/RoomBounds.cs`
- Create: `Assets/Scripts/Decoration/RoomLoader.cs`
- Modify: `Tools/Blender/validate_alba_asset.py`
- Create: `Assets/Tests/Editor/RoomAssetValidationTests.cs`

**Interfaces:**
- Consumes: approved room/furniture concept board and `ItemVisual3D.placement`.
- Produces: `RoomDefinition`, `PlaceableItem`, `SupportPoint`, `RoomBounds.Clamp(Vector3)`, and `RoomLoader.Load(string roomId)`.

- [ ] **Step 1: Write failing strict asset-validation tests**

```csharp
[Test]
public void EveryPlaceableCatalogItemHasARecognizablePrefabAndCollider()
{
    var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
    var errors = CatalogValidation.Validate(catalog, requirePrefabs: true);
    Assert.That(errors, Is.Empty);
    foreach (var visual in catalog.items.Where(v => v.placement.kind != PlacementKind.None))
    {
        Assert.That(visual.prefab.GetComponent<PlaceableItem>(), Is.Not.Null, visual.ItemId);
        Assert.That(visual.prefab.GetComponentInChildren<Collider>(), Is.Not.Null, visual.ItemId);
    }
}
```

- [ ] **Step 2: Model both room shells with mobile lighting in mind**

Create a sunny bedroom and garden living room with rounded wall transitions, doors, windows, floor, skirting, and recognizable architectural scale. Keep walkable space clear for the third-person camera. UV static meshes for baked lighting, use one 2048 atlas per room maximum, and separate collision meshes from visual meshes.

- [ ] **Step 3: Model at least sixteen furniture/decor prefabs**

Include bed, sofa, table, chair, shelf, lamp, rug, cushion, books, picture, clock, plant, side table, toy basket, floor vase, and framed photo prop. Model functional details such as legs, seams, cushions, shelves, handles, shades, leaves, pages, and frames. Use smooth normals and compact topology; no final item may be a plain primitive with only a color change.

Extend `validate_alba_asset.py` with `validate_furniture()`: require applied transforms, positive dimensions, one `COLLIDER_` object per exported furniture collection, no non-manifold visual mesh edges, no texture path outside `Assets/Art3D/Rooms/Textures`, and fewer than 4,000 triangles per furniture collection. Dispatch it when `--profile furniture` is selected.

- [ ] **Step 4: Add placement and support metadata**

`PlaceableItem` exposes immutable `ItemId`, `PlacementKind`, local footprint bounds, scale range, and rotation step. Furniture that hosts small decor gets explicitly named `SupportPoint` children with stable IDs. `RoomBounds` contains floor polygon bounds, safe player spawn, safe pet spawn, and camera limits, and implements `IWalkableArea.Contains` and `ClosestSafePoint`.

- [ ] **Step 5: Implement one-room-at-a-time loading**

`RoomLoader.Load` destroys the previous room root, loads the requested `RoomDefinition`, instantiates one room prefab, exposes its `RoomBounds`, and raises `RoomLoaded(RoomDefinition room)`. It never loads both room shells simultaneously. Unknown room IDs fall back to `room.sunny` and log one warning.

Create exactly two `RoomDefinition` assets with immutable IDs `room.sunny` and `room.garden`, prefab references, localized name keys, walkable bounds, safe spawns, and decoration-camera limits.

- [ ] **Step 6: Bake lighting and run strict validation**

Use one mixed directional light, baked indirect lighting, reflection probes only where visibly useful, and mobile shadow distance from the URP asset. Run Edit Mode tests; strict catalog validation must now pass with no missing placeable prefab or collider.

- [ ] **Step 7: Obtain the room/furniture approval checkpoint**

Show wide renders of both rooms and a catalog grid of all sixteen objects. Stop until the user confirms every object is recognizable, rounded, and sufficiently close to real furniture while retaining the Chibi Pop palette.

- [ ] **Step 8: Commit rooms and furniture**

```powershell
git add Art/Blender/Rooms Assets/Art3D/Rooms Assets/Scripts/Decoration Assets/Tests/Editor Assets/Resources/Data
git commit -m "art: add approved rooms and furniture set"
```

### Task 10: Implement the Separate Decoration Mode and Persistence

**Files:**
- Create: `Assets/Scripts/Decoration/DecorationModeController.cs`
- Create: `Assets/Scripts/Decoration/DecorationCamera.cs`
- Create: `Assets/Scripts/Decoration/DecorationSelection.cs`
- Create: `Assets/Scripts/Decoration/PlacementResult.cs`
- Create: `Assets/Scripts/Decoration/PlacementValidator.cs`
- Create: `Assets/Scripts/Decoration/PlacementSnapper.cs`
- Create: `Assets/Scripts/Decoration/DecorationSaveAdapter.cs`
- Create: `Assets/UI/Prefabs/DecorationHud.prefab`
- Create: `Assets/Tests/Helpers/DecorationTestFactory.cs`
- Create: `Assets/Tests/PlayMode/DecorationModeTests.cs`

**Interfaces:**
- Consumes: loaded `RoomDefinition`, `RoomBounds`, `RoomLayoutData`, `IItemCatalog3D`, and `ISaveService`.
- Produces: `DecorationModeController.Enter(RoomLayoutData layout)`, `Exit()`, `Place(string itemId)`, `RotateSelected(int direction)`, `ScaleSelected(float delta)`, `RemoveSelected()`, and `LayoutChanged(RoomLayoutData layout)`.

- [ ] **Step 1: Write failing placement and restore tests**

```csharp
[Test]
public void PlacementClampsToRoomAndSnapsRotationToFortyFiveDegrees()
{
    var rules = DecorationTestFactory.FloorRules(0.8f, 1.2f, 45f);
    var room = DecorationTestFactory.RoomBounds(min: Vector3.zero, max: new Vector3(5f, 3f, 5f));
    var result = PlacementValidator.Validate(
        desiredPosition: new Vector3(9f, 0f, -2f),
        desiredYaw: 67f,
        desiredScale: 1.5f,
        rules,
        room);

    Assert.That(result.Position, Is.EqualTo(new Vector3(5f, 0f, 0f)).Using(Vector3ComparerWithEqualsOperator.Instance));
    Assert.That(result.Yaw, Is.EqualTo(45f));
    Assert.That(result.Scale, Is.EqualTo(1.2f));
}

[UnityTest]
public IEnumerator SavedLayoutRestoresStableInstanceIds()
{
    var mode = DecorationTestFactory.CreateMode();
    mode.Enter(DecorationTestFactory.LayoutWithBed("bed-1"));
    yield return null;
    Assert.That(mode.FindPlaced("bed-1"), Is.Not.Null);
    Assert.That(mode.CurrentLayout.placements.Single().instanceId, Is.EqualTo("bed-1"));
}
```

- [ ] **Step 2: Run Play Mode tests and verify the red state**

Expected: compilation fails because the decoration controllers and validator do not exist.

- [ ] **Step 3: Implement pure placement validation**

`PlacementValidator.Validate` must clamp floor position to `RoomBounds`, snap yaw to the nearest rule step, clamp uniform scale, preserve wall height for wall items, and reject a surface placement whose `supportPointId` is missing. Return a `PlacementResult` containing `IsValid`, corrected position/yaw/scale, support IDs, and a localized error key.

Define `PlacementResult` as an immutable readonly struct with properties `bool IsValid`, `Vector3 Position`, `float Yaw`, `float Scale`, `string SupportInstanceId`, `string SupportPointId`, and `string ErrorKey`.

Create `DecorationTestFactory` with `FloorRules`, `RoomBounds`, `LayoutWithBed`, and `CreateMode` helpers backed by an in-memory catalog and save service. The fake save service records save count and the last deep-copied layout.

- [ ] **Step 4: Implement selection and camera controls**

`DecorationSelection` raycasts only placeable layers, outlines the selected item with a URP-compatible renderer feature or duplicated silhouette material, and clears selection when tapping empty floor. `DecorationCamera` uses a room-defined top/angled pose, pans inside limits, zooms between 4 m and 9 m, and never modifies the exploration camera transform.

- [ ] **Step 5: Implement layout restoration and autosave**

`DecorationSaveAdapter.Restore` instantiates each valid placement by stable `instanceId`, parents supported items to logical support anchors without altering world scale, ignores unknown IDs, and returns warnings instead of throwing. On every completed drag, rotation, scale, add, or remove action, serialize the complete active room layout and call `ISaveService.Save` once. Coalesce continuous drag frames into one save at pointer release.

- [ ] **Step 6: Implement safe mode transitions**

On `Enter`, disable `ThirdPersonController`, `ThirdPersonCamera`, pet following, and interaction input; enable the decoration camera and HUD. On `Exit`, validate the current layout, save it, restore exploration camera, place player and pet on safe points if blocked by moved furniture, and re-enable controls.

- [ ] **Step 7: Run decoration tests and manual aspect checks**

Run Play Mode tests, then test dragging, snapping, surface placement, removal, app restart, and both rooms at 16:9 and 20:9. Expected: no item leaves room bounds, every edit restores after restart, and no main function requires network.

- [ ] **Step 8: Commit decoration mode**

```powershell
git add Assets/Scripts/Decoration Assets/UI/Prefabs/DecorationHud.prefab Assets/Tests/PlayMode
git commit -m "feat: add persistent 3D decoration mode"
```

### Task 11: Replace the Monolithic 2D App with 3D Game Flow and Photo Mode

**Files:**
- Create: `Assets/Scripts/Flow/GameMode.cs`
- Create: `Assets/Scripts/Flow/GameFlowController.cs`
- Create: `Assets/Scripts/Flow/AlbaWorldCompositionRoot.cs`
- Create: `Assets/Scripts/Flow/CustomizationController.cs`
- Create: `Assets/Scripts/Photo/PhotoModeController.cs`
- Create: `Assets/Scripts/Photo/PhotoPoseLibrary.cs`
- Create: `Assets/Scripts/Photo/PhotoBrandingSettings.cs`
- Create: `Assets/Resources/Data/PhotoBrandingSettings.asset`
- Create: `Assets/UI/Prefabs/MainHud.prefab`
- Create: `Assets/UI/Prefabs/CustomizationPanel.prefab`
- Create: `Assets/UI/Prefabs/PhotoHud.prefab`
- Modify: `Assets/Scripts/Runtime/AlbaWorldBootstrap.cs`
- Modify: `Assets/Scripts/Runtime/PhotoExporter.cs`
- Modify: `Assets/Scripts/Runtime/LanguageService.cs`
- Retire: `Assets/Scripts/AlbaWorldApp.cs`
- Retire: `Assets/Scripts/UI/DraggableSceneElement.cs`
- Create: `Assets/Tests/Helpers/FlowTestFactory.cs`
- Create: `Assets/Tests/PlayMode/GameFlowTests.cs`
- Create: `Assets/Tests/PlayMode/PhotoModeTests.cs`

**Interfaces:**
- Consumes: character, player, pet, room, decoration, language, save, rewarded-ad, and photo-export services.
- Produces: `GameFlowController.ChangeMode(GameMode mode)`, `CustomizationController.Equip(string itemId)`, and `PhotoModeController.Capture()`.

- [ ] **Step 1: Write failing mode-transition tests**

```csharp
[UnityTest]
public IEnumerator DecorationAndPhotoModesOwnExactlyOneCameraAndInputContext()
{
    var flow = FlowTestFactory.Create();
    flow.ChangeMode(GameMode.Decoration);
    yield return null;
    Assert.That(flow.DecorationCamera.enabled, Is.True);
    Assert.That(flow.ExplorationCamera.enabled, Is.False);
    Assert.That(flow.PlayerController.enabled, Is.False);

    flow.ChangeMode(GameMode.Photo);
    yield return null;
    Assert.That(flow.PhotoCamera.enabled, Is.True);
    Assert.That(flow.DecorationCamera.enabled, Is.False);
    Assert.That(flow.ActiveUiRoot.name, Is.EqualTo("PhotoHud"));
}
```

- [ ] **Step 2: Run Play Mode tests and verify the red state**

Expected: compilation fails because `GameFlowController` and `GameMode` do not exist.

- [ ] **Step 3: Add explicit game modes and one composition root**

Define `GameMode { Exploration, Customization, Decoration, Photo }`. `AlbaWorldCompositionRoot` constructs or references every service exactly once, loads the save, loads the active room, assembles character/pet, restores layouts, and then enters Exploration. `GameFlowController` owns transitions and rejects re-entrant transitions until the previous transition is complete.

Add a `WorldStateCoordinator` inside the composition root that copies player position/yaw and active room ID into `GameSaveData` after room changes, on application pause, and on application quit. Invalid restored positions use the room's safe player spawn instead of blocking startup.

- [ ] **Step 4: Rebuild the customization UI around the 3D catalog**

`CustomizationController.Equip` checks unlock state and body compatibility, applies the item through the correct assembler, updates schema-3 loadout, and saves immediately. Build tabs for body/skin/face/hair/outfit/shoes/accessories/pet/pet accessories. Locked items show one optional-video button; refusal or failure leaves the item locked and every free category usable.

- [ ] **Step 5: Implement photo camera and pose library**

`PhotoModeController` disables all non-photo UI, freezes player and pet navigation without changing animation pose, clones the gameplay framing into a dedicated camera, allows orbit/pan/zoom within room bounds, and applies one of `Idle`, `Wave`, or two approved photo poses. `Capture()` waits until end of frame, temporarily hides `PhotoHud`, calls `IPhotoExportService.CaptureAndSave`, restores the HUD, and shows a localized success/error message.

`PhotoBrandingSettings` stores the Alba World logo sprite, configurable Play Store URL, and configurable QR sprite. Compose the logo and QR into a dedicated photo overlay that remains visible during capture while all controls stay hidden. Use a clearly marked non-production QR until the Play Store page exists; changing the final QR must require only replacing the asset, not code.

Create `FlowTestFactory.Create()` with fake catalog, save, rewarded-ad, photo-export, room, character, and pet services plus three disabled test cameras. The helper returns the flow controller and exposed test components used by mode assertions.

- [ ] **Step 6: Retire 2D runtime-only code without deleting migration data**

Remove `AlbaWorldApp` and `DraggableSceneElement` from runtime bootstrap and scene references. Keep `ColorSpriteFactory` only if still used by test/editor utilities; otherwise delete it in this commit. Do not remove legacy save fields until a later released schema proves migration in production.

- [ ] **Step 7: Complete localization keys**

Add `pt-BR` and `en` keys for movement hints, interaction, decoration controls, incompatible item, invalid placement, photo poses, photo success/error, ad declined/failure, and room names. Add an Edit Mode test that enumerates every runtime key requested by the new controllers and asserts both language dictionaries contain it.

- [ ] **Step 8: Run flow, photo, localization, and restore tests**

Expected: all Edit Mode and Play Mode tests pass; the game can start offline, enter every mode, save a photo on PC, close, and restore the same character, pet, room, and furniture layout.

- [ ] **Step 9: Commit the 3D game flow**

```powershell
git add Assets/Scripts/Flow Assets/Scripts/Photo Assets/Scripts/Runtime Assets/Scripts/UI Assets/UI Assets/Tests Assets/Scenes/Main.unity
git commit -m "feat: complete Alba World 3D game flow"
```

### Task 12: Integrate Child-Safe Rewarded Ads and Android Photo Export

**Files:**
- Modify: `Packages/manifest.json`
- Modify: `ProjectSettings/PackageManagerSettings.asset`
- Modify: `Assets/Scripts/Core/GameContracts.cs`
- Create: `Assets/Scripts/Monetization/ChildSafeAdsConfiguration.cs`
- Create: `Assets/Scripts/Monetization/IAdsSdkCalls.cs`
- Create: `Assets/Scripts/Monetization/GoogleRewardedAdsService.cs`
- Create: `Assets/Plugins/Android/AndroidManifest.xml`
- Modify: `Assets/Scripts/Runtime/RewardedAdsService.cs`
- Modify: `Assets/Plugins/Android/MediaStoreExporter.java`
- Create: `Assets/Tests/Editor/ChildSafeAdsTests.cs`
- Create: `Assets/Tests/PlayMode/RewardGrantTests.cs`
- Create: `Assets/Tests/Helpers/RewardTestFactory.cs`
- Create: `docs/compliance/ad-sdk-verification.md`

**Interfaces:**
- Consumes: `IRewardedAdsService`, `RewardLimiter`, `GameSaveData.unlockedItemIds`, Google Mobile Ads Unity Plugin 11.1.0, and Android MediaStore bridge.
- Produces: `RewardedAdResult`, one production `GoogleRewardedAdsService` with child-safe initialization and exactly-once reward callbacks; editor builds keep a deterministic fake.

- [ ] **Step 1: Record the policy and SDK verification**

In `docs/compliance/ad-sdk-verification.md`, record the verification date, Google Mobile Ads Unity Plugin 11.1.0, resolved Android artifact `com.google.android.gms:play-services-ads`, and the current minimum self-certified version from the official Google Play Families list. Include links to the official list and plugin release. Repeat this verification before every Play submission.

- [ ] **Step 2: Install only the Google Mobile Ads package without mediation**

Add the OpenUPM scoped registry (`https://package.openupm.com`, scope `com.google`) and pin `com.google.ads.mobile` to `11.1.0`. Do not install mediation adapters, analytics, Firebase, or banner/interstitial packages.

- [ ] **Step 3: Write failing configuration and idempotency tests**

```csharp
[Test]
public void ChildConfigurationIsAppliedBeforeInitialization()
{
    var calls = new FakeAdsSdkCalls();
    ChildSafeAdsConfiguration.Initialize(calls);
    Assert.That(calls.Sequence, Is.EqualTo(new[] { "child=true", "underAge=true", "rating=G", "npa=1", "initialize" }));
}

[UnityTest]
public IEnumerator DuplicateRewardCallbacksUnlockOnlyOnce()
{
    var fixture = RewardTestFactory.Create("hair.rainbow");
    fixture.Sdk.CompleteRewardTwice();
    yield return null;
    Assert.That(fixture.Save.unlockedItemIds.Count(id => id == "hair.rainbow"), Is.EqualTo(1));
    Assert.That(fixture.SaveService.SaveCount, Is.EqualTo(1));
}
```

Replace the boolean rewarded callback contract with:

```csharp
public enum RewardedAdResult { Rewarded, Declined, Failed, LimitReached, Unavailable }

public interface IRewardedAdsService
{
    bool IsAvailable { get; }
    void ShowForItem(string itemId, Action<RewardedAdResult> completed);
}
```

Update the editor fake and customization controller in the same task so every result maps to a distinct localized UI message.

- [ ] **Step 4: Implement child-safe initialization order**

Before calling `MobileAds.Initialize`, set child-directed treatment true, under-age-of-consent true, maximum ad content rating `G`, and non-personalized ads (`npa=1`). Do not preload or request any ad before this sequence completes. Use the official Google test rewarded unit ID in development builds and an inspector-configured production ID only in non-development builds.

Define `IAdsSdkCalls` with `SetChildDirected(bool)`, `SetUnderAgeOfConsent(bool)`, `SetMaximumRating(string)`, `SetNonPersonalized(bool)`, and `Initialize(Action<bool>)`. `ChildSafeAdsConfiguration.Initialize` calls those methods in the exact order asserted by the test; the production adapter maps them to Google Mobile Ads APIs.

- [ ] **Step 5: Implement exactly-once rewards**

`GoogleRewardedAdsService.ShowForItem` rejects unavailable ads, user cancellation, daily-limit exhaustion, and concurrent shows with distinct localized results. Grant only from the confirmed reward callback. Use a per-show GUID and a `HashSet<string>` of completed show IDs to prevent duplicate callbacks, add the item ID with ordinal distinct semantics, and save immediately once.

Create `RewardTestFactory.Create(string itemId)` with a fake ads SDK that starts one pending show, can emit duplicate reward callbacks, and a fake save service exposing `SaveCount`. Create `FakeAdsSdkCalls` in the same helper with an ordered `Sequence` list used by `ChildSafeAdsTests`.

- [ ] **Step 6: Remove advertising ID and preserve photo permissions**

Create an Android manifest override that removes `com.google.android.gms.permission.AD_ID` with `tools:node="remove"`. Keep `WRITE_EXTERNAL_STORAGE` only with `android:maxSdkVersion="28"`; never add gallery read permissions. Verify Android 10+ photo export uses MediaStore and Android 6–9 asks for write permission only after the save button.

- [ ] **Step 7: Run tests and inspect the merged Android manifest**

Build a development APK, use `aapt2 dump permissions`, and assert `AD_ID` and gallery-read permissions are absent while `WRITE_EXTERNAL_STORAGE` has max SDK 28. Run rewarded tests offline and with simulated load failure.

- [ ] **Step 8: Commit monetization hardening**

```powershell
git add Packages ProjectSettings Assets/Scripts/Monetization Assets/Scripts/Runtime/RewardedAdsService.cs Assets/Plugins/Android Assets/Tests docs/compliance
git commit -m "feat: add child-safe optional rewarded ads"
```

### Task 13: Optimize, Validate, and Produce Android Test Builds

**Files:**
- Create: `Assets/Editor/AlbaBuildValidation.cs`
- Modify: `Assets/Editor/BuildTools.cs`
- Create: `Assets/Tests/Editor/BuildValidationTests.cs`
- Create: `docs/testing/3d-device-matrix.md`
- Create: `docs/legal/privacy-policy-pt-BR.md`
- Create: `docs/legal/privacy-policy-en.md`
- Create: `Assets/Scripts/Flow/LegalSettings.cs`
- Create: `Assets/Resources/Data/LegalSettings.asset`
- Modify: `README.md`
- Produce: `outputs/AlbaWorld-3D-MVP-debug.apk`
- Produce: `outputs/AlbaWorld-3D-MVP-debug.aab`
- Produce: `outputs/AlbaWorld-3D-MVP-source.zip`

**Interfaces:**
- Consumes: the complete strict 3D catalog, URP scene, runtime tests, Android toolchain, and all approved assets.
- Produces: `AlbaBuildValidation.ValidateOrThrow()` and reproducible APK/AAB outputs.

- [ ] **Step 1: Write failing build-validation tests**

```csharp
[Test]
public void ProductionValidationRejectsMissingPrefabsAndOversizedTextures()
{
    var report = AlbaBuildValidation.Collect();
    Assert.That(report.MissingPrefabIds, Is.Empty);
    Assert.That(report.TexturePathsOverLimit, Is.Empty);
    Assert.That(report.DuplicateItemIds, Is.Empty);
    Assert.That(report.EnabledScenePaths, Is.EqualTo(new[] { "Assets/Scenes/Main.unity" }));
}
```

- [ ] **Step 2: Implement pre-build validation**

`AlbaBuildValidation` implements `IPreprocessBuildWithReport` and rejects duplicate/empty IDs, missing runtime prefabs, missing translations, texture sizes above the declared atlas limits, character/pet meshes above triangle budgets, development AdMob IDs in release builds, absent privacy-policy URL, enabled portrait orientations, and any Android permission for gallery reading or advertising ID.

Create Portuguese and English privacy policies covering local saves, optional rewarded ads, photo export, children/families treatment, no accounts, no analytics, and contact details for Alba World Games. `LegalSettings` contains both policy URLs and a support email; the asset must use the final hosted URLs before a release-signed build, while development builds may use the committed local policy filenames.

- [ ] **Step 3: Apply measured mobile optimizations**

Profile one character, one pet, and the busiest furnished room. Enable SRP Batcher, static batching for room shells, GPU instancing for repeated compatible materials, baked indirect light, 2× MSAA, 20 m shadow distance, and ASTC texture compression. Remove unused textures/materials and disable post-processing effects that cost more than 1 ms on the target emulator/device.

- [ ] **Step 4: Run the complete automated test suite**

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test Tools\CoreTests\AlbaWorld.CoreTests.csproj --no-restore
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -runTests -testPlatform editmode -testResults "$PWD\work\3d-editmode-final.xml" -logFile "$PWD\work\3d-editmode-final.log"
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -runTests -testPlatform playmode -testResults "$PWD\work\3d-playmode-final.xml" -logFile "$PWD\work\3d-playmode-final.log"
```

Expected: every result file reports `failed="0"`; logs contain no compiler error, unhandled exception, missing translation, or Android API warning.

- [ ] **Step 5: Build APK and AAB**

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -nographics -projectPath $PWD -executeMethod AlbaWorld.Editor.BuildTools.BuildAndroidApk -logFile "$PWD\work\3d-apk.log"
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -nographics -projectPath $PWD -executeMethod AlbaWorld.Editor.BuildTools.BuildAndroidAab -logFile "$PWD\work\3d-aab.log"
```

Expected: both logs contain `Build Finished, Result: Success`; each artifact is below 150 MB.

- [ ] **Step 6: Verify Android package metadata**

Use Unity SDK `aapt2` and `apksigner` to verify package `com.albaworldgames.albaworld`, min SDK 25, target SDK 35, ARM64 native library, valid debug signature, no `AD_ID`, no gallery-read permission, and write permission capped at API 28.

- [ ] **Step 7: Test on a PC Android emulator and one physical low-memory device**

Create an API 35 x86_64 AVD named `AlbaWorld_API35` with 2 GB RAM and 20:9 resolution, storing Android Studio/AVD files on drive D. Install the APK with `adb install -r`. Complete the matrix in `docs/testing/3d-device-matrix.md`: first launch offline, movement, both cameras, every customization category, cat/dog, both rooms, decoration restore, ad load failure, photo saving, app restart, and update install. Repeat performance and photo-permission checks on one physical Android device before any store submission.

- [ ] **Step 8: Refresh user-facing outputs and README**

Copy the successful APK/AAB into the named `outputs` files, refresh the source ZIP with `Assets`, `Art`, `Packages`, `ProjectSettings`, `Tools`, `docs`, `README.md`, and `.gitignore`, and document PC controls, emulator installation, APK installation, and the debug-signing limitation.

- [ ] **Step 9: Commit final validation material**

```powershell
git add Assets/Editor Assets/Tests/Editor docs/testing README.md
git commit -m "test: validate Alba World 3D Android MVP"
```

## Final Acceptance Gate

Do not claim the 3D MVP complete until all of the following evidence exists:

- Five concept sheets and three 3D approval checkpoints explicitly accepted by the user.
- Blender validation exits 0 for characters, pets, and furniture.
- Strict catalog validation reports no missing or duplicate IDs.
- .NET, Unity Edit Mode, and Unity Play Mode suites all pass from fresh commands.
- The game starts with network disabled and every main function remains usable.
- APK and AAB build successfully, stay below 150 MB, and expose the expected Android metadata.
- The API 35 emulator matrix is complete and one physical low-memory Android device has been tested.
- Current Google Play Families self-certified SDK status is rechecked immediately before submission.
