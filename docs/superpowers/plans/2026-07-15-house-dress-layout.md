# Alba World — Casa e Vestir Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (recommended for this session) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** reorganize the Alba World MVP into a readable Casa mode and a separate Vestir mode with a clear walkable room, safe furniture placement, visible deletion, and functional local appearance controls.

**Architecture:** Keep one Unity scene and add explicit runtime modes. `AlbaWorld3DApp` owns lifecycle and world objects; focused controllers own UI, movement, wardrobe, and furniture validation. The UI talks to controllers through small methods/events and never edits save dictionaries directly.

**Tech Stack:** Unity 6.3 LTS, C#, Unity UI/TextMeshPro, Unity Test Framework, existing Kenney CC0 pet/furniture assets, JSON local save service.

## Global Constraints

- Use one offline Unity scene; do not add networking, accounts, chat, analytics, or purchases.
- Keep existing item IDs and save fields; increment `SaveMigration.CurrentSchemaVersion` only when a new serialized field is required.
- Do not download or create new external art assets for this change; reuse current character materials, character prefabs, Kenney pets, and Kenney furniture.
- Support `pt-BR` and `en`; Portuguese source files must be UTF-8 and must not contain mojibake.
- Keep the game horizontal and validate 16:9, 18:9, and 20:9 safe areas.
- Keep API minimum 23/Android configuration unchanged unless a test exposes a regression.
- Run Edit Mode, Play Mode, and .NET tests before claiming completion.

---

### Task 1: Add failing tests for localization and layout contracts

**Files:**
- Create: `Assets/Tests/Editor/HouseDressLocalizationTests.cs`
- Create: `Assets/Tests/Editor/HouseDressLayoutContractTests.cs`
- Test existing: `Assets/Tests/Editor/ThreeDFlowReplacementTests.cs`

**Interfaces:**
- Consumes: `LanguageService`, `AlbaWorldUiMode`, `RoomFurnitureController.WalkableBounds` (introduced in later tasks).
- Produces: executable contracts for UTF-8 copy, mode names, and safe-room dimensions.

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Linq;
using AlbaWorld.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class HouseDressLocalizationTests
{
    [Test]
    public void PortugueseHouseDressCopyIsReadableAndComplete()
    {
        var service = new LanguageService("pt-BR");
        var keys = new[]
        {
            "hud.house", "hud.dress", "hud.furniture", "hud.actions", "hud.delete",
            "hud.undo", "hud.moveHint", "hud.noFreeSlot", "wardrobe.skin", "wardrobe.hair",
            "wardrobe.outfit", "wardrobe.shoes", "wardrobe.accessories"
        };

        foreach (var key in keys)
        {
            var value = service.Get(key);
            Assert.That(value, Does.Not.EqualTo(key), key);
            Assert.That(value, Does.Not.Contain("Ã"), key);
            Assert.That(value, Does.Not.Contain("Â"), key);
            Assert.That(value, Does.Not.Contain("�"), key);
        }
    }
}
```

```csharp
using AlbaWorld.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class HouseDressLayoutContractTests
{
    [Test]
    public void UiModesExposeCasaAndVestir()
    {
        Assert.That(System.Enum.IsDefined(typeof(AlbaWorldUiMode), "Casa"), Is.True);
        Assert.That(System.Enum.IsDefined(typeof(AlbaWorldUiMode), "Vestir"), Is.True);
    }

    [Test]
    public void WalkableZoneHasRoomForCharacter()
    {
        var bounds = RoomFurnitureController.DefaultWalkableBounds;
        Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(3.2f));
        Assert.That(bounds.size.z, Is.GreaterThanOrEqualTo(2.6f));
    }
}
```

- [ ] **Step 2: Run the focused tests to verify they fail**

Run Unity Edit Mode with `-runTests -testPlatform editmode -testFilter "HouseDressLocalizationTests|HouseDressLayoutContractTests" -testResults "work/house-dress-contracts-red.xml"`.

Expected: compile/test failure because the new mode enum, localization keys, and walkable bounds do not yet exist.

- [ ] **Step 3: Commit the failing tests**

```powershell
git add Assets/Tests/Editor/HouseDressLocalizationTests.cs Assets/Tests/Editor/HouseDressLayoutContractTests.cs
git commit -m "test: define house and dress layout contracts"
```

---

### Task 2: Introduce runtime mode and focused UI controller

**Files:**
- Create: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Runtime/LanguageService.cs`
- Modify: `Packages/manifest.json` only if TextMeshPro is missing (it is currently present, so no change expected).

**Interfaces:**
- Consumes: `GameSaveData`, `LanguageService`, `RoomFurnitureController`, `CharacterWardrobeController`, `Action` callbacks from the app.
- Produces: `AlbaWorldUiMode Mode`, `EnterDressMode()`, `EnterHouseMode()`, `SetFurnitureSelection(bool)`, and localized callbacks for buttons.

- [ ] **Step 1: Add mode and localization keys while keeping tests red for behavior**

Add:

```csharp
public enum AlbaWorldUiMode
{
    Casa,
    Vestir
}
```

Add these keys to both language dictionaries: `hud.house`, `hud.dress`, `hud.furniture`, `hud.actions`, `hud.delete`, `hud.undo`, `hud.moveHint`, `hud.noFreeSlot`, `hud.selectFurniture`, `hud.back`, `hud.save`, `wardrobe.skin`, `wardrobe.hair`, `wardrobe.outfit`, `wardrobe.shoes`, and `wardrobe.accessories`.

Rewrite the Portuguese dictionary file as UTF-8 and verify the literal values are `Móveis`, `Cômodo`, `Atrás`, `Acessórios`, and `Não foi possível salvar`.

- [ ] **Step 2: Build the UI controller with safe-area anchors**

`AlbaWorldUiController` must:

```csharp
public sealed class AlbaWorldUiController : MonoBehaviour
{
    public AlbaWorldUiMode Mode { get; private set; }
    public void Initialize(LanguageService language, RoomFurnitureController furniture,
        CharacterWardrobeController wardrobe, Action onPhoto, Action onRoom, Action onCharacter,
        Action onPet, Action onLanguage);
    public void EnterHouseMode();
    public void EnterDressMode();
    public void SetFurnitureSelection(bool selected);
    public void ShowNotice(string message, bool success);
}
```

Create a safe-area root, a compact top bar, a viewport gap, a bottom dock, and a single furniture action row. Use `TextMeshProUGUI` with `enableWordWrapping`, `overflowMode = TextOverflowModes.Ellipsis`, minimum button heights, and `LayoutElement` preferred sizes. Keep all button listeners on the controller and route mutations through callbacks.

- [ ] **Step 3: Connect the app lifecycle**

Replace `AlbaWorld3DApp.CreateHud` with controller creation and pass callbacks. `Start` must enter Casa mode after the world, furniture, movement, and wardrobe controllers are initialized. `OnApplicationPause` and `OnApplicationQuit` continue to call the existing `Persist` method.

- [ ] **Step 4: Run focused Edit Mode tests**

Run the focused contract tests. Expected: localization/mode/zone contracts pass; behavior tests remain pending for later tasks.

- [ ] **Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Scripts/Runtime/LanguageService.cs Assets/Tests/Editor/HouseDressLocalizationTests.cs Assets/Tests/Editor/HouseDressLayoutContractTests.cs
git commit -m "feat: add readable house and dress UI modes"
```

---

### Task 3: Add character movement with persistence

**Files:**
- Create: `Assets/Scripts/Runtime/CharacterMovementController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Core/World3DModels.cs` only if a helper for the existing `PlayerWorldStateData` is needed.
- Create: `Assets/Tests/PlayMode/CharacterMovementTests.cs`

**Interfaces:**
- Consumes: `Transform` character, `GameSaveData.playerWorld`, `ISaveService`, `RoomFurnitureController.WalkableBounds`, current UI mode.
- Produces: `SetInputEnabled(bool)`, `SetDestination(Vector3)`, `RestorePosition()`, `SavePosition()`, and `IsMoving`.

- [ ] **Step 1: Write the failing Play Mode tests**

Test that a character destination is clamped inside `WalkableBounds`, that movement is disabled in Vestir mode, and that `playerWorld.position` is updated after arrival.

```csharp
[UnityTest]
public IEnumerator DestinationNeverLeavesWalkableBounds()
{
    var fixture = CreateMovementFixture();
    var controller = fixture.Controller;
    controller.SetDestination(new Vector3(99f, 0f, 99f));
    yield return null;
    Assert.That(RoomFurnitureController.DefaultWalkableBounds.Contains(controller.transform.localPosition), Is.True);
}
```

The Play Mode fixture `CreateMovementFixture()` returns a temporary GameObject and a `CharacterMovementController` initialized with a `GameSaveData` plus in-memory save service; teardown destroys the GameObject and clears the fixture.

- [ ] **Step 2: Implement movement without physics**

Use `Camera.main.ScreenPointToRay` plus a horizontal `Plane` for touch/click destinations. Use `Input.GetAxisRaw("Horizontal")` and `Input.GetAxisRaw("Vertical")` for WASD/arrow fallback. Move with `Vector3.MoveTowards`, rotate toward direction, and clamp to the walkable bounds. Ignore input when `EventSystem.current.IsPointerOverGameObject()` is true or when `SetInputEnabled(false)` is active.

- [ ] **Step 3: Restore and save position**

On initialization, read `save.playerWorld.position`, clamp it to the zone, and place the character there. On arrival, pause, and quit, write the serializable position and call `ISaveService.Save`.

- [ ] **Step 4: Attach pet follow to the moving character**

Keep `PetFollowController.FollowTarget = _character.transform`; set its floor height and offset after every character replacement.

- [ ] **Step 5: Run Play Mode movement tests**

Expected: movement tests pass and no furniture UI receives the movement pointer.

- [ ] **Step 6: Commit**

```powershell
git add Assets/Scripts/Runtime/CharacterMovementController.cs Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Tests/PlayMode/CharacterMovementTests.cs
git commit -m "feat: add bounded character movement"
```

---

### Task 4: Make furniture placement safe and deletion reversible

**Files:**
- Modify: `Assets/Scripts/Runtime/RoomFurnitureController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Create: `Assets/Tests/PlayMode/RoomFurnitureSafetyTests.cs`

**Interfaces:**
- Consumes: `ItemCatalog3D`, existing placement rules, walkable bounds, current room save.
- Produces: `public static Bounds DefaultWalkableBounds`, `TryAddToFirstFreeSlot(string itemId)`, `TryMove(...)` with validation, `TryUndoRemove()`, `HasSelection`, and `SelectionChanged`.

- [ ] **Step 1: Write failing safety tests**

Cover these cases:

```csharp
using System.Linq;
using UnityEngine;

[Test]
public void FurnitureCannotBeAddedInsideWalkableZone()
{
    var controller = CreateControllerWithEmptyRoom();
    var added = controller.TryAdd("furniture.bed", new Vector3(0f, 0.22f, 0f));
    Assert.That(added, Is.False);
    Assert.That(controller.ActivePlacements, Is.Empty);
}

[Test]
public void FurnitureCannotOverlapExistingFurniture()
{
    var controller = CreateControllerWithEmptyRoom();
    Assert.That(controller.TryAdd("furniture.bed", new Vector3(-3.2f, 0.22f, 2.3f)), Is.True);
    var first = controller.SelectedInstanceId;
    var firstPlacement = controller.ActivePlacements.Single(item => item.instanceId == first);
    Assert.That(controller.TryAdd("furniture.sofa", new Vector3(2.6f, 0.22f, 2.3f)), Is.True);
    var second = controller.SelectedInstanceId;
    var beforeMove = controller.ActivePlacements.Single(item => item.instanceId == second);
    var overlap = new Vector3(firstPlacement.position.x, 0.22f, firstPlacement.position.z);
    Assert.That(controller.TryMove(second, overlap), Is.False);
    var afterMove = controller.ActivePlacements.Single(item => item.instanceId == second);
    Assert.That(afterMove.position.x, Is.EqualTo(beforeMove.position.x).Within(0.001f));
    Assert.That(afterMove.position.z, Is.EqualTo(beforeMove.position.z).Within(0.001f));
}

[Test]
public void RemoveCanBeUndoneBeforeTimeout()
{
    var controller = CreateControllerWithEmptyRoom();
    Assert.That(controller.TryAdd("furniture.chair", new Vector3(3.2f, 0.22f, -1.6f)), Is.True);
    var instanceId = controller.SelectedInstanceId;
    Assert.That(controller.TryRemove(instanceId), Is.True);
    Assert.That(controller.TryUndoRemove(), Is.True);
    Assert.That(controller.ActivePlacements.Any(item => item.instanceId == instanceId), Is.True);
}
```

The test fixture helper `CreateControllerWithEmptyRoom()` creates a temporary room root, loads `Resources/Data/AlbaItemCatalog3D`, initializes `RoomFurnitureController` with an in-memory `ISaveService`, and destroys the temporary root in teardown; it is test-only and is not a production API.

- [ ] **Step 2: Add explicit room geometry**

Define `DefaultWalkableBounds` as a central rectangle at floor height. Keep room limits separate from walkable limits. Add an item predicate that permits `furniture.rug` in the walk zone but blocks beds, sofas, tables, shelves, chairs, lamps, books, and plants.

- [ ] **Step 3: Add occupancy validation**

Before adding or moving, calculate the candidate collider bounds at the candidate position and reject intersections with other instances or the blocked walk zone. On drag rejection, restore `_lastValidPosition` and persist only the valid position.

- [ ] **Step 4: Add deterministic free slots**

Use a fixed perimeter slot list ordered clockwise around the room. `TryAddToFirstFreeSlot` scans the list, tests each candidate with the same occupancy validator, and returns false without creating an object when every slot is occupied.

- [ ] **Step 5: Add selection feedback and undo**

Expose `HasSelection` and `SelectionChanged`. Add a lightweight selection ring/outline child to the selected instance. On remove, retain the removed placement and prefab for a short undo window; `TryUndoRemove` restores it once and saves. Clear the undo state after the timeout or after another remove.

- [ ] **Step 6: Sanitize old saves**

During `SetRoom`/`LoadRoom`, load placements in order, discard duplicate IDs, clamp bounds, and relocate invalid/overlapping placements to free slots. Do not delete valid item IDs from the save.

- [ ] **Step 7: Run the safety tests**

Expected: no object is spawned on top of another, the central area remains available, and remove/undo persists correctly.

- [ ] **Step 8: Commit**

```powershell
git add Assets/Scripts/Runtime/RoomFurnitureController.cs Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Tests/PlayMode/RoomFurnitureSafetyTests.cs
git commit -m "fix: reserve walkable room space and add furniture undo"
```

---

### Task 5: Add wardrobe controller and Vestir mode behavior

**Files:**
- Create: `Assets/Scripts/Runtime/CharacterWardrobeController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Create: `Assets/Tests/PlayMode/CharacterWardrobeTests.cs`

**Interfaces:**
- Consumes: `ItemCatalog3D`, `GameSaveData.character`, current body ID, existing character prefab renderer names/materials.
- Produces: `SelectCategory(ItemCategory category)`, `TryApply(string itemId)`, `SelectedCategory`, `SelectionChanged`, and `SaveLoadout()`.

- [ ] **Step 1: Write failing wardrobe tests**

Verify that applying a catalog ID updates the matching `CharacterLoadoutData` field, rejects an unknown/incompatible ID, and saves immediately.

```csharp
[Test]
public void ApplyingHairUpdatesCharacterLoadout()
{
    var save = new GameSaveData();
    var controller = CreateWardrobe(save);
    Assert.That(controller.TryApply("hair.cloud"), Is.True);
    Assert.That(save.character.hairId, Is.EqualTo("hair.cloud"));
}
```

The wardrobe fixture `CreateWardrobe(GameSaveData save)` creates the current body prefab, loads `Resources/Data/AlbaItemCatalog3D`, and initializes `CharacterWardrobeController` with an in-memory save service. It must be disposed in teardown.

- [ ] **Step 2: Implement slot mapping**

Map `Skin`, `Hair`, `Outfit`, `Shoes`, and `HumanAccessory` to the existing save fields. Enforce `BodyCompatibility` and `free/unlockedItemIds` using the same rules as the catalog.

- [ ] **Step 3: Apply existing visuals without new assets**

Use renderer child names (`GirlHair`/`BoyHair`, `GirlBodySurface`/`BoyBodySurface`, suit/foot renderers, and face/accessory children) and `MaterialPropertyBlock` color changes derived from `ItemDefinition.tint`. Keep shared materials untouched. If a slot has no matching renderer, retain the save ID and show a localized “visual not available” notice rather than silently failing.

- [ ] **Step 4: Build the Vestir panel**

The UI controller must hide furniture and movement controls, show the character preview and category tabs, create two-column item cards, and wire `Voltar`/`Salvar`. Entering Vestir calls `SetInputEnabled(false)`; returning to Casa re-enables it.

- [ ] **Step 5: Run wardrobe Play Mode tests**

Expected: mode switches do not destroy the character, loadout changes survive a save/reload, and unsupported visuals are communicated without breaking the flow.

- [ ] **Step 6: Commit**

```powershell
git add Assets/Scripts/Runtime/CharacterWardrobeController.cs Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Tests/PlayMode/CharacterWardrobeTests.cs
git commit -m "feat: add local wardrobe mode"
```

---

### Task 6: Rebuild the room composition and responsive UI validation

**Files:**
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Modify: `Assets/Scripts/Runtime/RoomFurnitureController.cs`
- Modify: `Assets/Editor/ProjectSetup.cs` only if camera/scene defaults need regeneration.
- Create: `Assets/Tests/Editor/ResponsiveLayoutContractTests.cs`

**Interfaces:**
- Consumes: movement, furniture, wardrobe, and UI contracts from Tasks 2–5.
- Produces: a first-launch room with a clear center and a consistent UI at supported aspect ratios.

- [ ] **Step 1: Write layout tests**

Assert that the Canvas has one top bar, one bottom dock, no old `PetCard` overlay, and that each generated button has a nonzero rect, a `LayoutElement`, and a TextMeshPro child with wrapping enabled.

- [ ] **Step 2: Recompose the room**

Remove the central cylinder stage from the playable path. Keep floor and walls as shell geometry, place the bed/sofa/table/shelf near walls, place plant/lamp in corners, and keep only a floor rug in the walk zone. Spawn the character at `(-1.0f, floorY, -0.8f)` and pet near its follow offset.

- [ ] **Step 3: Recompose the camera**

Use a slightly higher, wider camera target centered on the walkable area so the character and room edges remain visible above the dock. Avoid hard-coded screen pixels; all UI dimensions remain in reference-resolution anchors and safe-area offsets.

- [ ] **Step 4: Validate aspect ratios**

Run Play Mode captures or layout assertions at 1920×1080, 2160×1080, and 2400×1080. Verify no button or label is outside the safe area and no label uses horizontal overflow.

- [ ] **Step 5: Run all Unity tests**

Use the GPU-capable Unity test runner:

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -projectPath (Get-Location).Path -runTests -testPlatform editmode -testResults work\house-dress-editmode.xml -quit
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -projectPath (Get-Location).Path -runTests -testPlatform playmode -testResults work\house-dress-playmode.xml -quit
```

Expected: all existing tests plus the new layout, movement, safety, and wardrobe tests pass.

- [ ] **Step 6: Commit**

```powershell
git add Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Scripts/Runtime/RoomFurnitureController.cs Assets/Editor/ProjectSetup.cs Assets/Tests/Editor/ResponsiveLayoutContractTests.cs
git commit -m "feat: recompose playable room and responsive layout"
```

---

### Task 7: Run non-Unity tests, manual Editor test, and update handoff docs

**Files:**
- Modify: `docs/ALBA-WORLD-CONTINUATION.md`
- Modify: `docs/testing/rooms-furniture-test-report.md`
- Create: `docs/testing/house-dress-layout-test-report.md`

**Interfaces:**
- Consumes: all runtime/test results from Tasks 1–6.
- Produces: reproducible local test evidence and next-step instructions.

- [ ] **Step 1: Run .NET tests**

```powershell
dotnet test Tools\CoreTests\AlbaWorld.CoreTests.csproj --no-restore
```

Expected: all existing .NET tests pass.

- [ ] **Step 2: Open the scene in Unity and test manually**

Open `Assets/Scenes/Main.unity`, press Play, and verify:

1. Casa mode shows a readable room with a clear center.
2. Clicking/tapping the floor moves the character; WASD/arrows move it on PC.
3. Móveis adds to free slots only; dragging onto another item snaps back.
4. Selecting an item reveals `Excluir`; exclusion removes it and `Desfazer` restores it once.
5. Vestir opens the wardrobe, hides furniture controls, changes a slot, saves, and returns to Casa.
6. Changing language keeps all labels readable in Portuguese and English.
7. Closing/reopening restores character position, room placements, and wardrobe selection.

- [ ] **Step 3: Record evidence**

Write the exact Unity versions, test result counts, aspect ratios, and any known limitations to `docs/testing/house-dress-layout-test-report.md`. Do not claim Android or Windows standalone success unless a fresh build passes.

- [ ] **Step 4: Update continuation notes**

Add the new modes, safe placement rules, and manual test procedure to `docs/ALBA-WORLD-CONTINUATION.md`.

- [ ] **Step 5: Commit the test report**

```powershell
git add docs/testing/house-dress-layout-test-report.md docs/ALBA-WORLD-CONTINUATION.md docs/testing/rooms-furniture-test-report.md
git commit -m "docs: record house and dress layout validation"
```

- [ ] **Step 6: Verify the final worktree and diff**

```powershell
git status --short
git diff --check
git log --oneline -8
```

Expected: only intentional changes are present, no untracked runtime files remain, and the final commit includes the test report.

---

## Self-review checklist

- The plan covers localization, UI modes, movement, furniture safety, wardrobe, responsive layout, tests, and documentation from the approved specification.
- Existing save IDs and fields remain authoritative; the plan does not remove or reuse IDs.
- Every new controller has a narrow responsibility and an explicit interface.
- The no-new-assets constraint is repeated in the global constraints and wardrobe task.
- Every task includes a test or verification command before its commit.
- The known standalone build limitation is not misrepresented as passing.
