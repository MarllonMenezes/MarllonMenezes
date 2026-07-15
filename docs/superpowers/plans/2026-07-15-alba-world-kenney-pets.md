# Alba World Kenney Pets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate all 24 Kenney Cube Pets assets into Alba World as selectable, saved, localizable, followable, photo-ready offline pets without creating new art.

**Architecture:** The existing catalog remains the single source of item IDs and visual prefabs. A Unity Editor setup tool will import the local Kenney FBX package, generate deterministic definitions/visuals/prefabs, and record the CC0 license. Runtime pet assembly resolves a selected `pet.*` ID from `IItemCatalog3D`, while `PetFollowController` moves a visual-only pet toward a target anchor without physics or networking.

**Tech Stack:** Unity 6.3.19f1, URP 17.3.0, C#, Unity Test Framework, Blender/FBX source assets, Android IL2CPP/ARM64.

## Global Constraints

- Package name stays `com.albaworldgames.albaworld` and publisher stays **Alba World Games**.
- The game remains offline and bilingual (`pt-BR` and `en`); no account, chat, multiplayer, analytics, purchases, or direct social sharing.
- The Kenney package is CC0. Keep the original `License.txt` and credit Kenney in the in-game credits.
- All 24 source animals are included: beaver, bee, bunny, cat, caterpillar, chick, cow, crab, deer, dog, elephant, fish, fox, giraffe, hog, koala, lion, monkey, panda, parrot, penguin, pig, polar bear, and tiger.
- Existing IDs `pet.cat` and `pet.dog` are preserved; no item ID is reused or removed.
- A selected pet must restore after a save/load cycle and must not require network access.
- Pets target 4,000–7,000 triangles when measured in the runtime prefab; materials use URP Simple Lit and Android-safe texture compression.
- Do not create or commission new art during this plan.

---

## File Map

- Create: `Assets/Art3D/Pets/Source/KenneyCubePets/` — copied FBX source files and source manifest.
- Create: `Assets/Art3D/Pets/Textures/colormap.png` — copied Kenney texture.
- Create: `Assets/Art3D/Pets/Materials/KenneyPets.mat` — shared URP material.
- Create: `Assets/Art3D/Pets/Prefabs/<animal>.prefab` — one prefab per species.
- Create: `Assets/Resources/Data/Definitions/pet.<animal>.asset` for the 22 new definitions; update existing cat/dog definitions only where required.
- Create: `Assets/Resources/Data/Visuals/pet.<animal>.asset` for all 24 visuals.
- Modify: `Assets/Editor/AlbaCatalogBuilder.cs` — deterministic 24-pet catalog specs and prefab references.
- Create: `Assets/Editor/KenneyPetAssetSetup.cs` — idempotent import, material, prefab, and license setup.
- Create: `Assets/Scripts/Pets/PetAssemblyController.cs` — loadout-to-prefab resolution and safe fallback.
- Create: `Assets/Scripts/Pets/PetFollowController.cs` — target-anchor following without physics.
- Create: `Assets/Scripts/Pets/KenneyPetIds.cs` — the single ordered list of 24 stable IDs.
- Create: `Assets/Editor/KenneySourceManifest.cs` — editor-only manifest loader used by source tests.
- Create: `Assets/Editor/MeshMetrics.cs` — editor-only triangle counting helper.
- Modify: `Assets/Scripts/Core/World3DModels.cs` only if pet runtime state needs an explicit target offset.
- Modify: `Assets/Scripts/Core/SaveModels.cs` only if migration must normalize the expanded pet IDs.
- Modify: `Assets/Scripts/Runtime/LanguageService.cs` for 24 `item.pet.*` keys and credits, because this project currently stores its bilingual dictionaries in code.
- Create: `Assets/Tests/Editor/KenneyPetCatalogTests.cs`.
- Create: `Assets/Tests/Helpers/LocalizationTestData.cs`.
- Create: `Assets/Tests/Helpers/PetTestFactory.cs`.
- Create: `Assets/Tests/PlayMode/PetAssemblyTests.cs`.
- Create: `docs/legal/assets/kenney-cube-pets-2.0/License.txt` and `manifest.json`.
- Modify: `README.md` with the import command, license note, and pet test commands.

## Interfaces

`PetAssemblyController` exposes:

```csharp
public bool TryApply(PetLoadoutData loadout);
public string ActivePetId { get; }
public GameObject ActiveInstance { get; }
public void Initialize(IItemCatalog3D catalog, Transform mount);
```

`PetFollowController` exposes:

```csharp
public Transform FollowTarget { get; set; }
public Vector3 FollowOffset { get; set; }
public float FollowSpeed { get; set; }
```

The controller never writes the save directly; the caller persists the selected `PetLoadoutData` through the existing `ISaveService` after `TryApply` succeeds.

## Task 1: Stage and validate the licensed Kenney source

**Files:**
- Create: `Assets/Art3D/Pets/Source/KenneyCubePets/` (24 FBXs and `License.txt` copy)
- Create: `Assets/Art3D/Pets/Source/KenneyCubePets/manifest.json`
- Create: `docs/legal/assets/kenney-cube-pets-2.0/License.txt`
- Create: `docs/legal/assets/kenney-cube-pets-2.0/manifest.json`
- Create: `Assets/Tests/Editor/KenneySourceManifestTests.cs`

**Interfaces:**
- Consumes: `outputs/kenney_cube-pets_1.0/Models/FBX format`, `Models/Textures/colormap.png`, and `License.txt`.
- Produces: a project-local, auditable source package with exactly 24 named entries.

- [ ] **Step 1: Write the failing manifest test**

```csharp
[Test]
public void KenneyManifestContainsAllTwentyFourAnimals()
{
    var manifest = KenneySourceManifest.LoadForTests();
    Assert.That(manifest.AnimalIds, Is.EquivalentTo(KenneyPetIds.All));
    Assert.That(manifest.License, Does.Contain("CC0"));
}
```

- [ ] **Step 2: Run the focused test and verify it fails**

Run the Unity Edit Mode test for `KenneySourceManifestTests`. Expected: FAIL because the manifest and copied source files do not exist yet.

- [ ] **Step 3: Copy only the approved source files**

Copy the 24 FBXs, the shared `colormap.png`, and `License.txt` with PowerShell:

```powershell
$src = 'C:\Users\Alba\Documents\Codex\2026-07-12\new-chat\outputs\kenney_cube-pets_1.0'
$dst = 'Assets\Art3D\Pets\Source\KenneyCubePets'
New-Item -ItemType Directory -Force $dst | Out-Null
Copy-Item "$src\Models\FBX format\animal-*.fbx" $dst -Force
Copy-Item "$src\Models\Textures\colormap.png" 'Assets\Art3D\Pets\Textures' -Force
Copy-Item "$src\License.txt" $dst -Force
Copy-Item "$src\License.txt" 'docs\legal\assets\kenney-cube-pets-2.0\License.txt' -Force
```

Create `manifest.json` with the 24 IDs, source filenames, package name, source path, license `CC0`, and optional credit URL `https://www.kenney.nl`.

Create `Assets/Scripts/Pets/KenneyPetIds.cs` with the authoritative list:

```csharp
namespace AlbaWorld.Pets;

public static class KenneyPetIds
{
    public static readonly string[] All =
    {
        "pet.beaver", "pet.bee", "pet.bunny", "pet.cat", "pet.caterpillar", "pet.chick",
        "pet.cow", "pet.crab", "pet.deer", "pet.dog", "pet.elephant", "pet.fish",
        "pet.fox", "pet.giraffe", "pet.hog", "pet.koala", "pet.lion", "pet.monkey",
        "pet.panda", "pet.parrot", "pet.penguin", "pet.pig", "pet.polar", "pet.tiger"
    };
}
```

Create `KenneySourceManifest.LoadForTests()` as an editor-only reader of the staged JSON and `KenneySourceManifest.AnimalIds`/`License` properties. The reader must fail with a clear exception when the JSON is missing or malformed.

- [ ] **Step 4: Run the focused test and verify it passes**

Expected: PASS with 24 unique source entries and the CC0 license text.

- [ ] **Step 5: Commit the source-only change**

```powershell
git add Assets/Art3D/Pets/Source Assets/Art3D/Pets/Textures/colormap.png docs/legal/assets/kenney-cube-pets-2.0 Assets/Tests/Editor/KenneySourceManifestTests.cs
git commit -m "art: stage CC0 Kenney pet sources"
```

## Task 2: Generate deterministic materials and prefabs

**Files:**
- Create: `Assets/Editor/KenneyPetAssetSetup.cs`
- Create: `Assets/Art3D/Pets/Materials/KenneyPets.mat`
- Create: `Assets/Art3D/Pets/Prefabs/<animal>.prefab` and `.meta` files
- Modify: `ProjectSettings/GraphicsSettings.asset` only if the importer requires no unrelated settings changes
- Create: `Assets/Tests/Editor/KenneyPetPrefabTests.cs`

**Interfaces:**
- Consumes: staged FBXs, `colormap.png`, and the 24-entry source manifest.
- Produces: 24 prefab paths returned by `KenneyPetAssetSetup.PrefabPathFor(string animalId)`.

- [ ] **Step 1: Write the failing prefab test**

```csharp
[Test]
public void EveryPetPrefabHasMeshRendererAndBoundedTriangles()
{
    foreach (var id in KenneyPetIds.All)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyPetAssetSetup.PrefabPathFor(id));
        Assert.That(prefab, Is.Not.Null, id);
        Assert.That(prefab.GetComponentInChildren<MeshRenderer>(), Is.Not.Null, id);
        Assert.That(MeshMetrics.TriangleCount(prefab), Is.LessThanOrEqualTo(7000), id);
    }
}
```

- [ ] **Step 2: Run the focused test and verify it fails**

Expected: FAIL with missing prefab paths.

- [ ] **Step 3: Implement idempotent setup**

`KenneyPetAssetSetup.Setup()` must:

1. load each `animal-*.fbx` from the staged source;
2. assign the shared URP Simple Lit material and `colormap.png`;
3. remove cameras/lights from the instantiated hierarchy;
4. normalize the root scale and pivot from a hard-coded `PetImportRule` table;
5. save `Assets/Art3D/Pets/Prefabs/<animal>.prefab`;
6. be safe to run repeatedly without duplicating assets.

The public path method must map `pet.cat` to `Assets/Art3D/Pets/Prefabs/cat.prefab`, etc. No runtime package or GLB importer is allowed.

Implement `MeshMetrics.TriangleCount(GameObject prefab)` by summing `MeshFilter.sharedMesh.triangles.Length / 3` for all child mesh filters and returning zero for a prefab without meshes. Keep this helper editor-only so runtime assemblies do not depend on `UnityEditor`.

- [ ] **Step 4: Run the focused test and verify it passes**

Expected: 24 prefabs pass mesh, renderer, material, bounds, and triangle-budget checks.

- [ ] **Step 5: Commit the asset-generation change**

```powershell
git add Assets/Editor/KenneyPetAssetSetup.cs Assets/Art3D/Pets/Materials Assets/Art3D/Pets/Prefabs Assets/Tests/Editor/KenneyPetPrefabTests.cs
git commit -m "art: generate Kenney pet prefabs"
```

## Task 3: Extend the immutable catalog and localization

**Files:**
- Modify: `Assets/Editor/AlbaCatalogBuilder.cs`
- Modify: `Assets/Scripts/Runtime/RuntimeCatalog.cs`
- Create/modify: `Assets/Resources/Data/Definitions/pet.*.asset`
- Create/modify: `Assets/Resources/Data/Visuals/pet.*.asset`
- Modify: localization tables/source files for `item.pet.*` keys
- Create: `Assets/Tests/Editor/KenneyPetCatalogTests.cs`

**Interfaces:**
- Consumes: `KenneyPetIds.All` and generated prefab paths.
- Produces: `IItemCatalog3D.GetVisual("pet.<animal>")` entries with category `Pet`, `EquipmentSlot.Pet`, and valid prefabs.

- [ ] **Step 1: Write the failing catalog test**

```csharp
[Test]
public void CatalogContainsEveryKenneyPetWithBothTranslations()
{
    var catalog = LoadCatalogForTests();
    foreach (var id in KenneyPetIds.All)
    {
        var visual = catalog.GetVisual(id);
        Assert.That(visual, Is.Not.Null, id);
        Assert.That(visual!.definition.category, Is.EqualTo(ItemCategory.Pet), id);
        Assert.That(visual.equipmentSlot, Is.EqualTo(EquipmentSlot.Pet), id);
        Assert.That(visual.prefab, Is.Not.Null, id);
        Assert.That(LocalizationTestData.Has("pt-BR", visual.definition.displayKey), Is.True, id);
        Assert.That(LocalizationTestData.Has("en", visual.definition.displayKey), Is.True, id);
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Expected: FAIL because only `pet.cat` and `pet.dog` are currently catalogued.

- [ ] **Step 3: Add deterministic 24-pet specs**

Add one `DefinitionSpec` per ID in `AlbaCatalogBuilder.BuildSpecs()` and one runtime `Add()` call per ID in `RuntimeCatalog`. Keep `pet.cat` and `pet.dog` free and use the existing reward policy for any future non-pet unlocks; all Kenney pets remain free for this MVP unless the owner later approves a different monetization decision.

`CreateOrUpdateVisual` must assign `visual.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyPetAssetSetup.PrefabPathFor(spec.Id));` for `ItemCategory.Pet` and throw a `BuildFailedException` if the asset is missing.

Add the 24 Portuguese and English display strings using keys `item.pet.<animal>`.

Implement `LocalizationTestData.Has(string locale, string key)` as a test helper that reads the committed locale dictionaries used by the project and returns `false` for missing or blank values; the test must not silently treat a missing table as present.

- [ ] **Step 4: Rebuild and run the focused catalog test**

Run `Alba World > Build 3D Item Catalog`, then the Edit Mode test. Expected: 24 unique pet IDs, no missing prefab, no missing translation.

- [ ] **Step 5: Commit catalog and localization**

```powershell
git add Assets/Editor/AlbaCatalogBuilder.cs Assets/Scripts/Runtime/RuntimeCatalog.cs Assets/Scripts/Runtime/LanguageService.cs Assets/Resources/Data/Definitions Assets/Resources/Data/Visuals Assets/Tests/Editor/KenneyPetCatalogTests.cs Assets/Tests/Helpers/LocalizationTestData.cs
git commit -m "feat: add all Kenney pets to catalog"
```

## Task 4: Implement pet assembly and follow behavior

**Files:**
- Create: `Assets/Scripts/Pets/PetAssemblyController.cs`
- Create: `Assets/Scripts/Pets/PetFollowController.cs`
- Create: `Assets/Tests/PlayMode/PetAssemblyTests.cs`
- Create: `Assets/Tests/PlayMode/PetFollowTests.cs`
- Create/modify: a test/vertical-slice scene or bootstrap object to add a pet anchor and controller; if no 3D scene is active yet, keep the controllers prefab-ready and cover integration with Play Mode fixtures.

**Interfaces:**
- Consumes: `PetLoadoutData`, `IItemCatalog3D`, and 24 catalog visuals.
- Produces: stable active instance, safe fallback to `pet.cat`, and bounded follow motion.

- [ ] **Step 1: Write failing Play Mode tests**

```csharp
[UnityTest]
public IEnumerator ApplyingEachPetCreatesTheRequestedPrefab()
{
    var fixture = PetTestFactory.Create();
    foreach (var id in KenneyPetIds.All)
    {
        Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = id }), Is.True, id);
        yield return null;
        Assert.That(fixture.Controller.ActivePetId, Is.EqualTo(id));
        Assert.That(fixture.Controller.ActiveInstance, Is.Not.Null);
    }
}

[UnityTest]
public IEnumerator FollowControllerMovesTowardAnchorWithoutPhysics()
{
    var fixture = PetTestFactory.Create();
    fixture.Controller.TryApply(new PetLoadoutData { petId = "pet.dog" });
    fixture.Follow.FollowTarget = fixture.Target.transform;
    fixture.Target.transform.position = new Vector3(3f, 0f, 2f);
    yield return new WaitForSeconds(0.5f);
    Assert.That(Vector3.Distance(fixture.Follow.transform.position, fixture.Target.transform.position), Is.LessThan(4f));
    Assert.That(fixture.Follow.GetComponent<Rigidbody>(), Is.Null);
}
```

- [ ] **Step 2: Run Play Mode tests and verify they fail**

Expected: FAIL because the pet scripts and test factory do not exist.

- [ ] **Step 3: Implement `PetAssemblyController`**

The controller resolves the requested visual, destroys only its own previous child instance, instantiates the prefab under a controlled root, applies the stored color/accessory hooks, and returns `false` without destroying the previous valid pet when an unknown ID or missing prefab is supplied. `ActivePetId` changes only after successful instantiation.

Add `PetTestFactory.Create()` under `Assets/Tests/Helpers` to create a temporary catalog containing the 24 generated visuals, a mount transform, a target transform, and the two controllers. The factory must expose `Controller`, `Follow`, and `Target` and destroy its temporary root in `IDisposable.Dispose()`.

- [ ] **Step 4: Implement `PetFollowController`**

Use `Vector3.SmoothDamp` or `Vector3.MoveTowards` in `LateUpdate`, clamp vertical motion to the pet’s configured floor height, and rotate only around Y toward the movement direction. Default values: offset `(0, 0, -1.25)`, speed `4.0`, turn speed `12.0`. Do not add `Rigidbody`, `NavMeshAgent`, network code, or random movement.

- [ ] **Step 5: Run Play Mode tests and verify they pass**

Expected: all 24 selections instantiate, the fallback is safe, and the pet follows within the bounded distance.

- [ ] **Step 6: Commit runtime pet behavior**

```powershell
git add Assets/Scripts/Pets Assets/Tests/PlayMode
git commit -m "feat: add selectable and following pets"
```

## Task 5: Connect selection, save, room, and photo flows

**Files:**
- Modify: `Assets/Scripts/AlbaWorldApp.cs` or the active 3D flow controller
- Modify: `Assets/Scripts/Core/SaveModels.cs` only for explicit normalization/migration coverage
- Create: `Assets/Tests/PlayMode/PetPersistenceTests.cs`
- Modify: photo capture setup so the active pet is included and UI remains hidden when the 3D photo flow is present; otherwise add the integration seam and test fixture without rewriting the existing 2D exporter.

**Interfaces:**
- Consumes: `PetAssemblyController`, `PetFollowController`, `GameSaveData.pet`, and existing `ISaveService`.
- Produces: offline selection/restoration in pet editor, house, and photo mode.

- [ ] **Step 1: Write failing persistence tests**

```csharp
[Test]
public void PetSelectionSurvivesSaveMigration()
{
    var input = new GameSaveData { schemaVersion = SaveMigration.CurrentSchemaVersion };
    input.pet.petId = "pet.panda";
    var output = SaveMigration.Upgrade(input);
    Assert.That(output.pet.petId, Is.EqualTo("pet.panda"));
}
```

- [ ] **Step 2: Run the test and verify it fails if normalization is incomplete**

Expected: the test fails only if `PetLoadoutData` is not preserved or the catalog fallback incorrectly replaces valid new IDs.

- [ ] **Step 3: Wire selection and persistence**

On pet selection, set `save.pet.petId`, call `PetAssemblyController.TryApply`, and invoke `ISaveService.Save` only after a successful apply. Restore the saved ID on boot; unknown IDs fall back to `pet.cat` while leaving the JSON valid.

- [ ] **Step 4: Include the active pet in house and photo contexts**

The room controller uses the same assembly prefab and keeps the pet inside the room bounds. Photo mode uses the active pet root and captures it with the character; no online operation or Android share intent is added.

- [ ] **Step 5: Run the focused Play Mode and .NET tests**

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test Tools\CoreTests\AlbaWorld.CoreTests.csproj --no-restore
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform playmode -testResults work\kenney-pets-playmode.xml
```

Expected: zero failed tests and no missing-pet warnings for valid IDs.

- [ ] **Step 6: Commit the flow integration**

```powershell
git add Assets/Scripts/AlbaWorldApp.cs Assets/Scripts/Core/SaveModels.cs Assets/Tests/PlayMode/PetPersistenceTests.cs README.md
git commit -m "feat: persist pets through house and photo flows"
```

## Task 6: Final asset audit and handoff

**Files:**
- Create: `docs/testing/kenney-pets-test-report.md`
- Modify: `README.md`
- Modify: `docs/ALBA-WORLD-CONTINUATION.md`

**Interfaces:**
- Consumes: all five previous task commits and generated test results.
- Produces: auditable evidence that all 24 pets work offline and the next project subsystem is ready.

- [ ] **Step 1: Run the complete focused verification**

Run source-manifest, prefab, catalog, Play Mode, and .NET tests from fresh commands. Confirm no compiler errors, no duplicate IDs, no missing translations, no missing prefabs, and no network dependency.

- [ ] **Step 2: Review visual outputs**

Capture a grid containing all 24 prefabs in Unity Game view at 16:9 and 20:9. Do not create a new model or texture. If a model needs a scale/offset adjustment, edit only its `PetImportRule` entry.

- [ ] **Step 3: Record the report and next step**

Write the test commands, result paths, triangle counts, source license, and known limitations to `docs/testing/kenney-pets-test-report.md`. Mark the next subsystem as rooms/furniture only after the pet report is complete.

- [ ] **Step 4: Commit the report**

```powershell
git add docs/testing/kenney-pets-test-report.md README.md docs/ALBA-WORLD-CONTINUATION.md
git commit -m "test: verify Kenney pets offline"
```

## Self-review checklist

- [ ] All 24 species are listed once with stable IDs.
- [ ] Existing `pet.cat` and `pet.dog` are preserved.
- [ ] The plan creates no new art and records the CC0 source.
- [ ] Every runtime interface used later is defined above.
- [ ] Every task has a focused test and an explicit commit.
- [ ] No task requires network access or a runtime asset importer.
- [ ] Room/decor/photo work is explicitly limited to connecting the pet, not silently expanding the plan into unrelated systems.
