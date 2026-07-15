# Alba World Rooms & Furniture Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Add two offline rooms with real Kenney furniture prefabs, bounded editing, and per-room persistence to the current 3D Alba World flow.

**Architecture:** Stage the licensed Kenney Furniture Kit source on D: and copy only the selected FBX models into the project. An editor setup command generates deterministic prefabs and assigns them to the existing `ItemVisual3D` assets. A focused `RoomFurnitureController` owns runtime instances and layout JSON, while `AlbaWorld3DApp` supplies the UI and photo context.

**Tech Stack:** Unity 6.3.19f1, C#, URP, Unity Test Framework, Kenney Furniture Kit 1.0 (CC0).

## Global Constraints

- Keep `com.albaworldgames.albaworld`, Alba World Games, offline operation, and bilingual UI.
- Do not restore `AlbaWorldApp`, `UiFactory`, `ColorSpriteFactory`, or `DraggableSceneElement`.
- Do not create new art; use only imported Kenney furniture models and existing character/pet assets.
- Keep layout state in `GameSaveData.rooms3D` with stable item IDs and instance IDs.

---

### Task 1: Stage furniture sources and import prefabs

**Files:**
- Create: `Assets/Art3D/Furniture/Source/KenneyFurnitureKit/` (nine FBX files, `License.txt`, `manifest.json`)
- Create: `Assets/Art3D/Furniture/Prefabs/<item>.prefab`
- Create: `Assets/Editor/KenneyFurnitureAssetSetup.cs`
- Modify: `Assets/Editor/AlbaCatalogBuilder.cs`
- Modify: `Assets/Resources/Data/Visuals/furniture.*.asset`
- Create: `Assets/Tests/Editor/KenneyFurniturePrefabTests.cs`

**Interfaces:** `KenneyFurnitureAssetSetup.PrefabPathFor(string itemId)` returns the generated prefab path; the catalog visual's `prefab` points at that asset.

- [ ] Copy `bedSingle.fbx`, `loungeSofa.fbx`, `table.fbx`, `chairCushion.fbx`, `bookcaseOpen.fbx`, `lampRoundFloor.fbx`, `pottedPlant.fbx`, `rugRound.fbx`, and `books.fbx` from `D:\AlbaWorldAssets\KenneyFurnitureKit-1.0\package\Models\FBX format`.
- [ ] Implement idempotent setup that creates prefabs, removes cameras/lights from imported hierarchies, assigns a shared URP material, and writes the CC0 manifest.
- [ ] Add a failing-then-passing Edit Mode test for nine prefab paths with renderers and nonzero mesh triangles.
- [ ] Update catalog building to assign furniture prefab references and fail clearly when any selected source is missing.

### Task 2: Implement bounded room furniture runtime

**Files:**
- Create: `Assets/Scripts/Runtime/RoomFurnitureController.cs`
- Create: `Assets/Tests/PlayMode/RoomFurnitureTests.cs`
- Modify: `Assets/Scripts/Core/SaveModels.cs` only if normalization coverage requires it.

**Interfaces:**

```csharp
public void Initialize(ItemCatalog3D catalog, Transform roomRoot, GameSaveData save, ISaveService saveService);
public bool TryAdd(string itemId, Vector3 worldPosition);
public bool TryMove(string instanceId, Vector3 worldPosition);
public bool TryScale(string instanceId, float delta);
public bool TryMirror(string instanceId);
public bool TryRemove(string instanceId);
public void SetRoom(string roomId);
public IReadOnlyList<FurniturePlacementData> ActivePlacements { get; }
```

- [ ] Write Play Mode tests for add/move/scale/mirror/remove, floor bounds, room isolation, and save/restore.
- [ ] Resolve catalog prefabs, instantiate only valid furniture, clamp X/Z to the room rectangle, and fix Y to the floor.
- [ ] Serialize each active room immediately after a successful mutation; ignore invalid IDs when restoring.

### Task 3: Connect 3D app UI and photo flow

**Files:**
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Runtime/LanguageService.cs`
- Modify: `Assets/Tests/Editor/ThreeDFlowReplacementTests.cs`

- [ ] Replace the generic stage-only presentation with the room root and imported furniture instances.
- [ ] Add localized furniture buttons, room toggle, remove, scale, mirror, front/back controls, and a clear selection state.
- [ ] Keep character and selected pet inside the room and ensure `PhotoExporter` captures the active room layout.
- [ ] Add Play Mode coverage for switching rooms and restoring the selected pet plus furniture.

### Task 4: Verify and document

**Files:**
- Create: `docs/legal/assets/kenney-furniture-kit-1.0/License.txt`
- Create: `docs/legal/assets/kenney-furniture-kit-1.0/manifest.json`
- Create: `docs/testing/rooms-furniture-test-report.md`
- Modify: `README.md`
- Modify: `docs/ALBA-WORLD-CONTINUATION.md`

- [ ] Run focused Unity Edit Mode and Play Mode tests and a fresh compile.
- [ ] Verify no old procedural runtime files or scene components return.
- [ ] Record source URL, CC0 license, imported models, test result files, and the known Windows `level0` build issue if it persists.
