# Cartoon City Character Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the handcrafted girl/boy models with the free CC0 Cartoon City character presets while keeping offline wardrobe selection, save migration, and explicit click-to-select control for characters, pets, and furniture.

**Architecture:** Import only the free Cartoon City FBX files and expose them through a new `CharacterPresetCatalog` with immutable preset IDs. `AlbaWorld3DApp` resolves one preset at a time, `CharacterWardrobeController` applies preset/material/accessory state atomically, and `WorldSelectionController` owns all world pointer arbitration so a first click selects and a later gesture manipulates. Existing furniture and pet systems consume the selection contract instead of reading the same pointer independently.

**Tech Stack:** Unity 6.3.19f1, C# nullable runtime assemblies, Unity Test Framework Edit/Play Mode, URP, FBX Humanoid import, local JSON save migration, Unity Localization-compatible `LanguageService`, PowerShell tooling on Windows.

## Global Constraints

- Project remains offline-first: no runtime loader, account, multiplayer, chat, analytics, or network dependency.
- Use only the free Cartoon City Characters download; record URL, date, SHA-256, manifest, and CC0 license under `docs/legal/assets/rg-poly-cartoon-city-characters/`.
- Store the downloaded archive under `D:\AlbaWorldAssets\RGPolyCartoonCityCharacters\` before copying selected FBX files into the project.
- Target landscape Android, API floor 23, package size below 150 MB, and stable operation on a 2 GB RAM device.
- Keep `pt-BR` and `en`; Portuguese Brazilian is selected from a Portuguese-Brazil device and English remains the fallback.
- First pointer interaction on an unselected character, pet, or furniture selects only; no movement, drag, scale, mirror, delete, or ordering is allowed until a later interaction.
- Do not reuse or delete legacy IDs; migrate `body.girl` and `body.boy` to new preset IDs through a versioned schema migration.
- Do not copy PK XD characters, UI, names, or proprietary assets.

---

### Task 1: Acquire and register the free Cartoon City source

**Files:**
- Create: `D:\AlbaWorldAssets\RGPolyCartoonCityCharacters\CharactersFree.zip`
- Create: `docs/legal/assets/rg-poly-cartoon-city-characters/manifest.json`
- Create: `docs/legal/assets/rg-poly-cartoon-city-characters/License.txt`
- Create: `docs/legal/assets/rg-poly-cartoon-city-characters/README.md`
- Test: `Assets/Tests/Editor/CartoonCitySourceTests.cs`

**Interfaces:**
- Produces `CartoonCitySourceManifest` JSON fields `sourceUrl`, `downloadedUtc`, `sha256`, `license`, `freeArchive`, and `selectedFiles`.
- Produces the source directory consumed by Task 2; no runtime code depends on the archive path.

The manifest test uses this serializable data shape (the implementation may keep it in the editor test assembly or move it to a small shared legal-record type):

```csharp
[Serializable]
public sealed class CartoonCitySourceManifest
{
    public string sourceUrl = string.Empty;
    public string downloadedUtc = string.Empty;
    public string sha256 = string.Empty;
    public string license = string.Empty;
    public string freeArchive = string.Empty;
    public string[] selectedFiles = Array.Empty<string>();
}
```

- [ ] **Step 1: Download the named free archive and record its checksum**

Use the official package page and save only the free download to D:, then calculate SHA-256:

```powershell
$source = 'https://rg-poly.itch.io/cartoon-city-massive-pack-characters'
$download = 'D:\AlbaWorldAssets\RGPolyCartoonCityCharacters\CharactersFree.zip'
New-Item -ItemType Directory -Force (Split-Path $download) | Out-Null
# Download through the page's free download control, then verify locally:
Get-FileHash $download -Algorithm SHA256
```

Record the exact archive name, timestamp, hash, source URL, CC0 text, and the list of FBX files selected for the MVP. Do not copy the full paid bundle.

- [ ] **Step 2: Write the source-manifest test first**

Create an Edit Mode test that loads `manifest.json` and asserts non-empty URL, SHA-256, `CC0`, an archive path containing `RGPolyCartoonCityCharacters`, and at least one selected FBX file.

```csharp
[Test]
public void CartoonCityManifestRecordsCommercialLicenseAndSelectedFbx()
{
    var manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(
        "Assets/Art3D/Characters/Source/RGPolyCartoonCity/manifest.json");
    Assert.That(manifest, Is.Not.Null);
    var data = JsonUtility.FromJson<CartoonCitySourceManifest>(manifest!.text);
    Assert.That(data.sourceUrl, Does.Contain("rg-poly.itch.io"));
    Assert.That(data.license, Does.Contain("CC0"));
    Assert.That(data.sha256, Has.Length.EqualTo(64));
    Assert.That(data.selectedFiles, Is.Not.Empty);
}
```

- [ ] **Step 3: Run the focused test to verify RED**

Run:

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -projectPath . -runTests -testPlatform editmode -testFilter CartoonCitySourceTests -testResults work/cartoon-city-source-red.xml -logFile work/cartoon-city-source-red.log
```

Expected: FAIL because the manifest and imported source directory do not exist yet.

- [ ] **Step 4: Add the manifest and legal records**

Copy only the selected FBX files and their textures into `Assets/Art3D/Characters/Source/RGPolyCartoonCity`. Add `manifest.json`, `License.txt`, and `README.md` with the source URL, date, hash, CC0 notice, and a statement that unmodified asset files are not redistributed outside the game build.

- [ ] **Step 5: Run the focused test to verify GREEN and commit**

Run the same command and expect `passed=1 failed=0`. Commit:

```powershell
git add Assets/Art3D/Characters/Source/RGPolyCartoonCity docs/legal/assets/rg-poly-cartoon-city-characters
git commit -m "chore: register Cartoon City free character source"
```

### Task 2: Add preset definitions and schema migration

**Files:**
- Create: `Assets/Scripts/Catalog/CharacterPresetDefinition.cs`
- Create: `Assets/Scripts/Catalog/CharacterPresetCatalog.cs`
- Create: `Assets/Resources/Data/CharacterPresets/CartoonCityPresetCatalog.asset`
- Modify: `Assets/Scripts/Core/World3DModels.cs`
- Modify: `Assets/Scripts/Core/SaveModels.cs`
- Modify: `Assets/Scripts/Core/SaveMigration.cs`
- Test: `Assets/Tests/Editor/CharacterPresetCatalogTests.cs`
- Test: `Assets/Tests/Editor/CharacterPresetMigrationTests.cs`

**Interfaces:**
- `CharacterPresetDefinition` exposes immutable `id`, `displayKey`, `prefab`, `skinColorIds`, `accessorySlots`, `free`, and `triangleBudget`.
- `CharacterPresetCatalog.Get(string id)`, `All`, `Validate()`, and `FirstFree()` are the only runtime lookup APIs.
- `CharacterLoadoutData.characterPresetId` is persisted without removing `bodyId`.
- `SaveMigration.CurrentSchemaVersion` increments by one; `body.girl` maps to `cartooncity.char.01`, and `body.boy` maps to `cartooncity.char.02`.

- [ ] **Step 1: Write failing catalog and migration tests**

Cover unique IDs, missing prefab detection, first-free fallback, idempotent conversion, and preservation of rooms, pet, language, and unlocked IDs. Example migration assertion:

```csharp
[Test]
public void LegacyBodyIdsMigrateOnceWithoutDroppingOtherState()
{
    var save = new GameSaveData { schemaVersion = 3, languageCode = "en", selectedPetId = "pet.fox" };
    save.character.bodyId = "body.girl";
    save.rooms3D = new[] { new RoomLayoutData { roomId = "room.sunny" } };
    var migrated = SaveMigration.Upgrade(save);
    var again = SaveMigration.Upgrade(migrated);
    Assert.That(migrated.character.characterPresetId, Is.EqualTo("cartooncity.char.01"));
    Assert.That(again.character.characterPresetId, Is.EqualTo("cartooncity.char.01"));
    Assert.That(again.selectedPetId, Is.EqualTo("pet.fox"));
    Assert.That(again.rooms3D, Has.Length.EqualTo(1));
}
```

- [ ] **Step 2: Run tests to verify RED**

Run the two focused filters and expect failures for the missing catalog type/field and migration mapping.

- [ ] **Step 3: Implement the catalog and migration**

Use `ScriptableObject` definitions with validation that reports duplicate IDs, null prefabs, empty display keys, and non-positive triangle budgets. Keep old `bodyId`, `hairId`, `outfitId`, and `shoesId` values for backward-compatible JSON; the new preset ID becomes the active source of truth after migration.

- [ ] **Step 4: Run tests to verify GREEN and commit**

Run the focused Edit Mode filters and expect all assertions to pass. Commit:

```powershell
git add Assets/Scripts/Catalog/CharacterPresetDefinition.cs Assets/Scripts/Catalog/CharacterPresetCatalog.cs Assets/Resources/Data/CharacterPresets Assets/Scripts/Core/World3DModels.cs Assets/Scripts/Core/SaveModels.cs Assets/Scripts/Core/SaveMigration.cs Assets/Tests/Editor/CharacterPresetCatalogTests.cs Assets/Tests/Editor/CharacterPresetMigrationTests.cs
git commit -m "feat: add Cartoon City preset catalog and save migration"
```

### Task 3: Import one Cartoon City pilot and generate deterministic prefabs

**Files:**
- Create: `Assets/Scripts/Editor/CartoonCityCharacterAssetSetup.cs`
- Create: `Assets/Art3D/Characters/Prefabs/CartoonCity/CharacterPilot.prefab`
- Create: `Assets/Art3D/Characters/Materials/CartoonCity/`
- Modify: `Assets/Resources/Data/CharacterPresets/CartoonCityPresetCatalog.asset`
- Test: `Assets/Tests/Editor/CartoonCityCharacterImportTests.cs`

**Interfaces:**
- Editor menu `Alba World/Setup Cartoon City Pilot` is deterministic and safe to run repeatedly.
- The pilot prefab has a root `CartoonCityCharacter`, `Animator`, `CharacterSelectable`, colliders, URP materials, and one `CharacterMovementController` consumer added at runtime.

- [ ] **Step 1: Write failing import tests**

Assert that the pilot FBX exists, is imported as `ModelImporterAnimationType.Human`, has a valid avatar, has an `Animator`, has at least one renderer and collider, and stays below the configured triangle budget.

- [ ] **Step 2: Run the import filter to verify RED**

Run `CartoonCityCharacterImportTests`; expect failure because the source has not been copied/imported and no prefab exists.

- [ ] **Step 3: Implement the editor setup menu**

Configure the FBX importer for Humanoid/CreateFromThisModel, assign URP materials without mutating source textures, generate the pilot prefab, add a capsule/box collider sized from renderer bounds, and populate `CartoonCityPresetCatalog.asset` with `cartooncity.char.01`.

- [ ] **Step 4: Run the import filter to verify GREEN and commit**

Run `CartoonCityCharacterImportTests`; expect all import assertions to pass. Commit the pilot and deterministic setup script:

```powershell
git add Assets/Scripts/Editor/CartoonCityCharacterAssetSetup.cs Assets/Art3D/Characters/Prefabs/CartoonCity Assets/Art3D/Characters/Materials/CartoonCity Assets/Resources/Data/CharacterPresets/CartoonCityPresetCatalog.asset Assets/Tests/Editor/CartoonCityCharacterImportTests.cs
git commit -m "feat: import Cartoon City pilot character"
```

### Task 4: Replace runtime body resolution with character presets

**Files:**
- Create: `Assets/Scripts/Runtime/CharacterPresetController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Runtime/CharacterWardrobeController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Modify: `Assets/Scripts/Runtime/LanguageService.cs`
- Test: `Assets/Tests/PlayMode/CharacterPresetControllerTests.cs`
- Test: `Assets/Tests/PlayMode/CharacterWardrobeTests.cs`

**Interfaces:**
- `CharacterPresetController.TryApply(string presetId, string skinColorId, string accessoryId)` returns `bool` and commits only after a successful instantiate/material/accessory pass.
- `CharacterPresetController.ActivePresetId` and `ActiveInstance` are read-only runtime state.
- `CharacterWardrobeController.ItemsForCategory` exposes preset/color/accessory categories while preserving existing save events.

- [ ] **Step 1: Write failing Play Mode tests**

Test that applying a valid preset replaces the active instance atomically, invalid IDs preserve the current instance, skin color uses `MaterialPropertyBlock`, and the migrated save is written immediately. Test the UI category keys in both locales.

- [ ] **Step 2: Run the filters to verify RED**

Run `CharacterPresetControllerTests` and the updated wardrobe filters; expect failures for the missing controller and preset catalog wiring.

- [ ] **Step 3: Implement the pilot runtime path**

Load `CharacterPresetCatalog` from `Resources`, apply the migrated preset in `AlbaWorld3DApp.CreateCharacter`, normalize height to the existing walkable-room scale, keep the current movement controller, and replace the old body toggle with a preset picker. Hide incompatible hair/outfit/shoes options instead of applying geometry that clips.

- [ ] **Step 4: Update localized UI copy**

Add `wardrobe.character`, `wardrobe.color`, `wardrobe.accessories`, `hud.selectCharacter`, `hud.selectPet`, and `hud.followPet` in `pt-BR` and `en`. Keep the existing `body.girl`/`body.boy` display keys only for migration diagnostics.

- [ ] **Step 5: Run Play Mode and commit**

Run the focused Play Mode tests and expect all to pass. Commit:

```powershell
git add Assets/Scripts/Runtime/CharacterPresetController.cs Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Scripts/Runtime/CharacterWardrobeController.cs Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Scripts/Runtime/LanguageService.cs Assets/Tests/PlayMode/CharacterPresetControllerTests.cs Assets/Tests/PlayMode/CharacterWardrobeTests.cs
git commit -m "feat: use Cartoon City character presets at runtime"
```

### Task 5: Implement unified click-to-select arbitration

**Files:**
- Create: `Assets/Scripts/Runtime/WorldSelectionController.cs`
- Create: `Assets/Scripts/Runtime/SelectableWorldEntity.cs`
- Create: `Assets/Scripts/Runtime/SelectionMarker.cs`
- Modify: `Assets/Scripts/Runtime/RoomFurnitureController.cs`
- Modify: `Assets/Scripts/Runtime/CharacterMovementController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Test: `Assets/Tests/PlayMode/WorldSelectionControllerTests.cs`
- Test: `Assets/Tests/PlayMode/RoomFurnitureSafetyTests.cs`
- Test: `Assets/Tests/PlayMode/CharacterMovementTests.cs`

**Interfaces:**
- `SelectableWorldEntity` exposes `EntityKind`, `EntityId`, `CanManipulate`, and `SetSelected(bool)`.
- `WorldSelectionController.SelectedEntity`, `Select(GameObject)`, `Clear()`, `TryBeginManipulation(Vector2)`, and `ConsumePointerDown(Vector2)` are the only world-pointer entry points.
- `WorldSelectionController.SelectionChanged` publishes the selected entity ID and kind to the HUD.

- [ ] **Step 1: Write failing selection tests**

Cover these exact contracts:

```csharp
[UnityTest]
public IEnumerator FirstClickSelectsFurnitureWithoutMovingIt()
{
    var before = fixture.Controller.PositionOf("furniture.1");
    fixture.Selection.ConsumePointerDown(fixture.FurnitureScreenPoint);
    yield return null;
    Assert.That(fixture.Selection.SelectedEntity.EntityId, Is.EqualTo("furniture.1"));
    Assert.That(fixture.Controller.PositionOf("furniture.1"), Is.EqualTo(before));
}

[UnityTest]
public IEnumerator UnselectedFloorTapCannotMoveCharacter()
{
    fixture.Selection.Clear();
    fixture.Selection.ConsumePointerDown(fixture.FloorScreenPoint);
    yield return null;
    Assert.That(fixture.Movement.IsMoving, Is.False);
}
```

Add equivalent pet selection and second-gesture manipulation tests, plus assertions that the old furniture controller does not process the same pointer event after the selection controller consumes it.

- [ ] **Step 2: Run selection filters to verify RED**

Run `WorldSelectionControllerTests`; expect failures because both furniture and movement currently receive the first pointer event directly.

- [ ] **Step 3: Implement the selection controller and marker**

Raycast in priority order UI → selectable entity → floor, select on first pointer down, and publish one event. Use a pink ring/outline with renderer bounds and destroy it when selection clears. Do not use physics simulation; colliders are query-only.

- [ ] **Step 4: Gate furniture drag and controls**

Change `RoomFurnitureController.BeginDrag` so the first hit calls `SetSelected` and exits. A later pointer down on the same selected instance arms `_draggingId`; only then can `DragToScreen` call `Move`. Ensure scale, mirror, front/back, delete, and undo check `HasSelection` and do nothing for another or empty selection.

- [ ] **Step 5: Gate character movement**

Change `CharacterMovementController.ReadPointerDestination` to ignore a raycast that hits a selectable entity and to require the character entity to be selected before accepting a floor destination. Keyboard movement remains available only while the character is selected and Casa mode is active.

- [ ] **Step 6: Run selection, furniture, and movement tests to verify GREEN and commit**

Run `WorldSelectionControllerTests`, `RoomFurnitureSafetyTests`, and `CharacterMovementTests`; expect all to pass. Commit:

```powershell
git add Assets/Scripts/Runtime/WorldSelectionController.cs Assets/Scripts/Runtime/SelectableWorldEntity.cs Assets/Scripts/Runtime/SelectionMarker.cs Assets/Scripts/Runtime/RoomFurnitureController.cs Assets/Scripts/Runtime/CharacterMovementController.cs Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Tests/PlayMode/WorldSelectionControllerTests.cs Assets/Tests/PlayMode/RoomFurnitureSafetyTests.cs Assets/Tests/PlayMode/CharacterMovementTests.cs
git commit -m "feat: require explicit world selection before manipulation"
```

### Task 6: Add selected-pet manual placement and follow restore

**Files:**
- Modify: `Assets/Scripts/Pets/PetFollowController.cs`
- Modify: `Assets/Scripts/Pets/PetAssemblyController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Core/World3DModels.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Modify: `Assets/Scripts/Runtime/LanguageService.cs`
- Test: `Assets/Tests/PlayMode/PetFollowTests.cs`
- Test: `Assets/Tests/PlayMode/PetSelectionTests.cs`

**Interfaces:**
- `PetFollowController.IsFollowing`, `SetFollowing(bool)`, and `SetManualPosition(Vector3)` are deterministic and save through the app coordinator.
- `PetSelectionTests` verifies first click selection, second drag movement within room bounds, follow pause, follow restore, and persistence after reload.

- [ ] **Step 1: Write failing pet selection/follow tests**

Assert that a first click on the pet does not move it, the next drag moves only the selected pet, and `hud.followPet` restores the character follow target.

- [ ] **Step 2: Run the focused filters to verify RED**

Run `PetFollowTests` and `PetSelectionTests`; expect failures for missing manual mode state and selection input.

- [ ] **Step 3: Add manual mode and persistence**

Add `pet.manualPlacement` and `pet.position` fields to the local save model with migration defaults. When a selected pet is dragged, disable `FollowTarget`, clamp to the walkable room bounds, and save on pointer release. `SetFollowing(true)` restores the offset behind the character and clears manual placement.

- [ ] **Step 4: Add the localized restore action and run GREEN tests**

Expose `Seguir personagem`/`Follow character` only when the pet is selected and manual. Run the focused filters and commit:

```powershell
git add Assets/Scripts/Pets/PetFollowController.cs Assets/Scripts/Pets/PetAssemblyController.cs Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Scripts/Core/World3DModels.cs Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Scripts/Runtime/LanguageService.cs Assets/Tests/PlayMode/PetFollowTests.cs Assets/Tests/PlayMode/PetSelectionTests.cs
git commit -m "feat: allow selected pets to be placed and restored to follow"
```

### Task 7: Import the remaining free presets and complete the catalog

**Files:**
- Modify: `Assets/Scripts/Editor/CartoonCityCharacterAssetSetup.cs`
- Modify: `Assets/Art3D/Characters/Prefabs/CartoonCity/`
- Modify: `Assets/Resources/Data/CharacterPresets/CartoonCityPresetCatalog.asset`
- Test: `Assets/Tests/Editor/CartoonCityCharacterImportTests.cs`

**Interfaces:**
- The setup menu accepts a deterministic list of selected free FBX names and produces stable preset IDs `cartooncity.char.01` through the approved free set.

- [ ] **Step 1: Extend the import test matrix**

Add one test case per selected free FBX asserting Humanoid avatar, common skeleton signature, renderer/collider presence, triangle budget, and no missing material references.

- [ ] **Step 2: Run the matrix to verify RED for unimported presets**

Run `CartoonCityCharacterImportTests`; expect only the pilot to pass and the remaining selected files to fail.

- [ ] **Step 3: Generate the remaining prefabs and catalog entries**

Run `Alba World/Setup Cartoon City Pilot` after extending it to all selected free FBX names. Keep only the approved free set, remove unused demo textures, and update `manifest.json` selected files and hashes.

- [ ] **Step 4: Run the full import matrix to verify GREEN and commit**

Expect every selected free preset to pass, then commit:

```powershell
git add Assets/Scripts/Editor/CartoonCityCharacterAssetSetup.cs Assets/Art3D/Characters/Prefabs/CartoonCity Assets/Resources/Data/CharacterPresets/CartoonCityPresetCatalog.asset Assets/Art3D/Characters/Source/RGPolyCartoonCity/manifest.json Assets/Tests/Editor/CartoonCityCharacterImportTests.cs
git commit -m "feat: add remaining free Cartoon City presets"
```

### Task 8: Localization, responsive UI, and full regression verification

**Files:**
- Modify: `Assets/Scripts/Runtime/LanguageService.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Create: `docs/testing/cartoon-city-character-test-report.md`
- Test: `Assets/Tests/Editor/HouseDressLocalizationTests.cs`
- Test: `Assets/Tests/Editor/ResponsiveLayoutContractTests.cs`
- Test: `Tools/CoreTests/` existing suite

- [ ] **Step 1: Write failing localization and responsive assertions**

Assert every new character/pet-selection/follow key exists in both locales, selected controls are readable, and selection state clears when switching Casa/Vestir, room, language, or photo mode.

- [ ] **Step 2: Run focused tests to verify RED**

Run `HouseDressLocalizationTests` and `ResponsiveLayoutContractTests`; expect missing keys/state assertions until the UI is updated.

- [ ] **Step 3: Implement UI copy and state presentation**

Add preset cards, selected markers, an explicit selection hint, disabled manipulation controls when nothing is selected, and the pet follow restore action. Keep the existing built-in runtime font fallback so no button renders as a textless rectangle.

- [ ] **Step 4: Run the complete verification matrix**

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -projectPath . -runTests -testPlatform editmode -testResults work/cartoon-city-editmode.xml -logFile work/cartoon-city-editmode.log
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -projectPath . -runTests -testPlatform playmode -testResults work/cartoon-city-playmode.xml -logFile work/cartoon-city-playmode.log
dotnet test Tools/CoreTests/AlbaWorld.CoreTests.csproj --nologo --verbosity minimal
```

Expected: zero failed tests, no missing-font errors, no missing localization keys, and no importer errors.

- [ ] **Step 5: Run manual PC acceptance**

Open `Assets/Scenes/Main.unity`, press Play, and test 16:9, 18:9, and 20:9. Confirm first-click selection for character, pet, and furniture; second-gesture manipulation; wardrobe presets; manual pet placement/follow restore; save/reload; language toggle; photo mode; and offline launch.

- [ ] **Step 6: Write the test report and commit**

Record exact test totals, selected preset IDs, source hash, package-size estimate, and any remaining limitations in `docs/testing/cartoon-city-character-test-report.md`. Commit:

```powershell
git add Assets/Scripts/Runtime/LanguageService.cs Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Tests/Editor/HouseDressLocalizationTests.cs Assets/Tests/Editor/ResponsiveLayoutContractTests.cs docs/testing/cartoon-city-character-test-report.md
git commit -m "test: verify Cartoon City character MVP and selection UX"
```

## Plan self-review

- **Spec coverage:** source/license is Task 1; preset catalog and migration are Task 2; FBX/Humanoid pilot and remaining imports are Tasks 3 and 7; runtime preset wardrobe is Task 4; explicit selection for all three entity types is Task 5; pet manual mode is Task 6; localization, responsive UI, offline PC acceptance, and regression evidence are Task 8.
- **Selection consistency:** the first-click rule is defined once in Task 5 and consumed by character, furniture, and pet tasks; no subsystem is allowed to process the same pointer event independently.
- **Migration consistency:** `characterPresetId` is introduced in Task 2, consumed by Tasks 4 and 7, and never replaces legacy IDs in serialized data.
- **No placeholders:** every task names exact files, test filters, expected RED/GREEN outcomes, and commit commands.
- **Scope:** only one free asset family, one pilot before the full set, and no online/monetization changes are included.
