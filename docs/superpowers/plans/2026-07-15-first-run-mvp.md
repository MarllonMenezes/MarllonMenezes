# First-run MVP Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give Alba World a clear first-run flow with a Welcome/tutorial screen, explicit room selection, and persistence without changing the approved offline assets.

**Architecture:** Persist one `onboardingCompleted` flag through the existing versioned `GameSaveData`. Extend `AlbaWorldUiController` with a `Welcome` mode and a `StartGame` callback; the app owns persistence and mode transitions. Keep the existing Casa/Vestir construction and add a localized room label/button to the Casa top bar.

**Tech Stack:** Unity 6.3.19f1, C# nullable runtime assembly, Unity UI (`Canvas`, `Text`, `Button`), Unity Test Framework Edit/Play Mode, .NET 8 xUnit core tests.

## Global Constraints

- Project remains offline-first: no runtime loader, account, multiplayer, chat, analytics, or network dependency.
- Use only the already-approved Cartoon City, Kenney pets, and Kenney furniture assets.
- Keep `pt-BR` and `en`; Portuguese Brazilian is selected from a Portuguese-Brazil device and English remains the fallback.
- Landscape canvas uses `1920x1080`, safe area 2%–98%, and touch targets of at least 44 px.
- Existing save fields and immutable IDs must be preserved; schema migration must be idempotent.
- Welcome blocks movement; only Casa enables character movement.

---

### Task 1: Persist first-run completion

**Files:**
- Modify: `Assets/Scripts/Core/SaveModels.cs`
- Modify: `Tools/CoreTests/AlbaWorld.CoreTests.csproj`
- Create: `Tools/CoreTests/FirstRunSaveTests.cs`
- Test: `Tools/CoreTests/FirstRunSaveTests.cs`

**Interfaces:**
- Produces `GameSaveData.onboardingCompleted` (bool, default false).
- Produces `SaveMigration.CurrentSchemaVersion == 5`.

- [ ] **Step 1: Write the failing tests**

```csharp
using AlbaWorld.Core;
using Xunit;

public sealed class FirstRunSaveTests
{
    [Fact]
    public void NewSaveStartsWithWelcomePending()
    {
        var save = SaveMigration.Upgrade(null);
        Assert.False(save.onboardingCompleted);
        Assert.Equal(5, save.schemaVersion);
    }

    [Fact]
    public void SchemaFourMigrationPreservesProgressAndIsIdempotent()
    {
        var old = new GameSaveData
        {
            schemaVersion = 4,
            onboardingCompleted = true,
            activeRoomId = "room.cozy",
            unlockedItemIds = new[] { "hair.sunny" }
        };
        var upgraded = SaveMigration.Upgrade(old);
        var again = SaveMigration.Upgrade(upgraded);
        Assert.True(upgraded.onboardingCompleted);
        Assert.Equal("room.cozy", upgraded.activeRoomId);
        Assert.Equal(new[] { "hair.sunny" }, upgraded.unlockedItemIds);
        Assert.Equal(5, again.schemaVersion);
        Assert.True(again.onboardingCompleted);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Tools/CoreTests/AlbaWorld.CoreTests.csproj --no-restore --filter FirstRunSaveTests`

Expected: FAIL because `onboardingCompleted` and schema 5 do not exist yet.

- [ ] **Step 3: Add the field and bump migration**

Add `public bool onboardingCompleted;` beside `languageCode`, and change `CurrentSchemaVersion` from 4 to 5. Do not change the existing normalization or legacy ID mappings.

- [ ] **Step 4: Run the focused and full .NET tests**

Run: `dotnet test Tools/CoreTests/AlbaWorld.CoreTests.csproj --no-restore --filter FirstRunSaveTests`

Expected: 2 passed.

Run: `dotnet test Tools/CoreTests/AlbaWorld.CoreTests.csproj --no-restore`

Expected: all existing and new tests pass with 0 failures.

- [ ] **Step 5: Commit**

```powershell
git add Assets/Scripts/Core/SaveModels.cs Tools/CoreTests/AlbaWorld.CoreTests.csproj Tools/CoreTests/FirstRunSaveTests.cs
git commit -m "feat: persist first-run onboarding state"
```

### Task 2: Add Welcome/tutorial UI mode

**Files:**
- Modify: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Scripts/Runtime/LanguageService.cs`
- Create: `Assets/Tests/PlayMode/FirstRunUiTests.cs`

**Interfaces:**
- `AlbaWorldUiMode.Welcome` is a public mode.
- `AlbaWorldUiController.Initialize(..., Action startGame, ...)` receives a start callback.
- `AlbaWorldUiController.EnterWelcomeMode()` rebuilds the overlay.
- `AlbaWorldUiController.SetRoomName(string)` updates the top-bar label when Casa is active.

- [ ] **Step 1: Write the failing Play Mode tests**

```csharp
using NUnit.Framework;
using UnityEngine;

public sealed class FirstRunUiTests
{
    [Test]
    public void WelcomeModeIsPublicAndBlocksWorldInput()
    {
        var app = new GameObject("first-run-app");
        var ui = app.AddComponent<AlbaWorld.Runtime.AlbaWorldUiController>();
        Assert.That(System.Enum.IsDefined(typeof(AlbaWorld.Runtime.AlbaWorldUiMode), "Welcome"), Is.True);
        Object.DestroyImmediate(app);
    }
}
```

- [ ] **Step 2: Run the focused Unity test to verify it fails**

Run Unity Edit/Play Mode test filtering `FirstRunUiTests` through the existing project test command. Expected: compilation/test failure because `Welcome` is missing.

- [ ] **Step 3: Implement the minimal UI mode**

Add `Welcome` to the enum, a `StartGame` callback, and a `BuildWelcomeMode()` root under `_safeRoot`. The root must contain localized title, subtitle, three instruction cards, a `Jogar` button, and the language button. Use existing `Panel`, `Label`, `AddButton`, and `Anchor` helpers with 2% safe area anchors and 44 px minimum height. `EnterWelcomeMode()` destroys Casa/Vestir roots, sets `Mode`, builds Welcome, and invokes `ModeChanged`.

In `Initialize`, build Welcome only when the app requests it; add a public `bool Initialized` if needed for the app to safely call `EnterWelcomeMode` after wiring callbacks.

In `AlbaWorld3DApp`, pass `CompleteOnboarding` as the `startGame` callback. `CompleteOnboarding` sets `onboardingCompleted = true`, persists immediately, and calls `_ui.EnterHouseMode()`. `OnUiModeChanged` must enable movement only for `Casa`.

- [ ] **Step 4: Add localized strings**

Add keys and values to the existing language tables: `welcome.title`, `welcome.subtitle`, `welcome.select`, `welcome.drag`, `welcome.modes`, `welcome.play`, `welcome.language`, `room.sunny`, and `room.cozy` in both `pt-BR` and `en`. Fallback values must be short and non-empty.

- [ ] **Step 5: Run focused and full Unity tests**

Expected: focused first-run tests pass; full Edit Mode and Play Mode suites remain green.

- [ ] **Step 6: Commit**

```powershell
git add Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Scripts/Runtime/LanguageService.cs Assets/Tests/PlayMode/FirstRunUiTests.cs
git commit -m "feat: add first-run welcome tutorial"
```

### Task 3: Make room selection visible and localized

**Files:**
- Modify: `Assets/Scripts/Runtime/AlbaWorldUiController.cs`
- Modify: `Assets/Scripts/Runtime/AlbaWorld3DApp.cs`
- Modify: `Assets/Tests/PlayMode/FirstRunUiTests.cs`

**Interfaces:**
- `AlbaWorldUiController.SetRoomName(string)` updates a cached `_roomName` text.
- `AlbaWorld3DApp.ChangeRoomStyle()` calls `SetRoomName` after applying the layout.

- [ ] **Step 1: Write the failing room-label test**

```csharp
[Test]
public void RoomNameCanBeUpdatedWithoutRebuildingWorld()
{
    var app = new GameObject("room-ui");
    var ui = app.AddComponent<AlbaWorld.Runtime.AlbaWorldUiController>();
    Assert.DoesNotThrow(() => ui.SetRoomName("Sala ensolarada"));
    Object.DestroyImmediate(app);
}
```

- [ ] **Step 2: Run it and confirm the missing API failure**

Expected: FAIL because `SetRoomName` is not defined.

- [ ] **Step 3: Add the visible room button**

Replace the static room label in `BuildHouseMode` with a button using the current localized room name. Keep the existing `Room` callback so clicking the button toggles the two room IDs. Store its `Text` in `_roomName`; `SetRoomName` updates it when the UI exists and caches the value otherwise. Use a compact high-contrast color distinct from the title.

Call `_ui.SetRoomName(_language.Get(_save.activeRoomId == "room.cozy" ? "room.cozy" : "room.sunny"));` after HUD creation and after `ChangeRoomStyle`.

- [ ] **Step 4: Run focused and full tests**

Expected: room-label test passes, language tests remain green, and both room layouts restore independently.

- [ ] **Step 5: Commit**

```powershell
git add Assets/Scripts/Runtime/AlbaWorldUiController.cs Assets/Scripts/Runtime/AlbaWorld3DApp.cs Assets/Tests/PlayMode/FirstRunUiTests.cs
git commit -m "feat: expose localized room selector"
```

### Task 4: Verification and handoff

**Files:**
- Modify: `docs/ALBA-WORLD-CONTINUATION.md`
- Create: `docs/testing/first-run-mvp-test-report.md`

- [ ] **Step 1: Run .NET tests**

Run: `dotnet test Tools/CoreTests/AlbaWorld.CoreTests.csproj --no-restore`

Expected: 0 failures.

- [ ] **Step 2: Run Unity Edit Mode and Play Mode suites**

Use the project’s existing batch test commands, writing fresh XML to `work/first-run-editmode.xml` and `work/first-run-playmode.xml`. Expected: no failed tests.

- [ ] **Step 3: Inspect the diff and compile logs**

Run `git diff --check`, inspect Unity `Editor.log` for compile errors, and confirm only the planned files changed.

- [ ] **Step 4: Write the report**

Record exact commands, test counts, schema version 5, the two room IDs, and the remaining release gaps (PNG/MediaStore, AdMob, signed AAB, modular clothing, animations).

- [ ] **Step 5: Commit the report**

```powershell
git add docs/ALBA-WORLD-CONTINUATION.md docs/testing/first-run-mvp-test-report.md
git commit -m "docs: record first-run mvp verification"
```
