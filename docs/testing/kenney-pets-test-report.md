# Kenney pets — relatório de verificação da Tarefa 6

Data da auditoria: 15 de julho de 2026
Projeto: Alba World, Unity 6000.3.19f1, branch `feature/alba-world-3d`

## Resultado

Os 24 pets Kenney estão disponíveis offline como prefabs locais, com IDs estáveis, entradas de catálogo e traduções `pt-BR`/`en`. Os IDs legados `pet.cat` e `pet.dog` permanecem presentes. Não foi detectada dependência de rede, pacote runtime ou permissão Android nova.

| ID estável | Fonte FBX | Triângulos no prefab |
| --- | --- | ---: |
| `pet.beaver` | `animal-beaver.fbx` | 670 |
| `pet.bee` | `animal-bee.fbx` | 742 |
| `pet.bunny` | `animal-bunny.fbx` | 575 |
| `pet.cat` | `animal-cat.fbx` | 684 |
| `pet.caterpillar` | `animal-caterpillar.fbx` | 578 |
| `pet.chick` | `animal-chick.fbx` | 490 |
| `pet.cow` | `animal-cow.fbx` | 578 |
| `pet.crab` | `animal-crab.fbx` | 676 |
| `pet.deer` | `animal-deer.fbx` | 760 |
| `pet.dog` | `animal-dog.fbx` | 490 |
| `pet.elephant` | `animal-elephant.fbx` | 676 |
| `pet.fish` | `animal-fish.fbx` | 422 |
| `pet.fox` | `animal-fox.fbx` | 568 |
| `pet.giraffe` | `animal-giraffe.fbx` | 598 |
| `pet.hog` | `animal-hog.fbx` | 706 |
| `pet.koala` | `animal-koala.fbx` | 594 |
| `pet.lion` | `animal-lion.fbx` | 889 |
| `pet.monkey` | `animal-monkey.fbx` | 918 |
| `pet.panda` | `animal-panda.fbx` | 734 |
| `pet.parrot` | `animal-parrot.fbx` | 530 |
| `pet.penguin` | `animal-penguin.fbx` | 558 |
| `pet.pig` | `animal-pig.fbx` | 424 |
| `pet.polar` | `animal-polar.fbx` | 522 |
| `pet.tiger` | `animal-tiger.fbx` | 951 |

The current prefab test enforces the project ceiling of 7,000 triangles, and every species is below it. The original design target was 4,000–7,000 triangles per equipped pet; these intentionally low-poly Kenney meshes are below that lower target, which is recorded as a follow-up art/performance consideration rather than a reason to add geometry in this task.

## Historical Task 6 baseline (retained for traceability)

Commands were run from the project root. Unity commands intentionally omit `-quit`; the Test Framework exits after writing its result file.

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter AlbaWorld.Tests.KenneySourceManifestTests -testResults work/task6-kenney-source.xml -logFile work/task6-kenney-source.log
# result: Passed, 2 total, 2 passed, 0 failed

& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter AlbaWorld.Tests.KenneyPetPrefabTests -testResults work/task6-kenney-prefab.xml -logFile work/task6-kenney-prefab.log
# result: Passed, 2 total, 2 passed, 0 failed

& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter AlbaWorld.Tests.KenneyPetCatalogTests -testResults work/task6-kenney-catalog.xml -logFile work/task6-kenney-catalog.log
# result: Passed, 2 total, 2 passed, 0 failed

& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform playmode -testResults work/task6-kenney-playmode.xml -logFile work/task6-kenney-playmode.log
# result: Passed, 21 total, 21 passed, 0 failed

& 'C:\Program Files\dotnet\dotnet.exe' test Tools\CoreTests\AlbaWorld.CoreTests.csproj --no-restore
# result: Passed, 14 total, 14 passed, 0 failed
```

Triangle counts were emitted by a temporary editor-only audit test and saved at `work/task6-kenney-triangles.xml` (1/1 passed); the helper and its `.meta` file were removed after the audit. The focused test XML and logs above are ignored build evidence and are not product assets.

## Correção final do review (15/07/2026)

The final review follow-up was verified with fresh commands after the implementation:

Android is a release gate and remains **BLOCKED (not approved)**: the Burst `bcl.exe`/`AndroidPlayerBuildProgram` path repeatedly returned `ExitCode: 4`, produced no APK, and `adb devices` returned an empty device/emulator list.

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter AlbaWorld.Tests.KenneyPetCatalogTests -testResults work/task7-green-credit-final.xml -logFile work/task7-green-credit-final.log
# result: Passed, 3 total, 3 passed, 0 failed (localized in-game credit included)

& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform playmode -testResults work/task7-green-playmode-final.xml -logFile work/task7-green-playmode-final.log
# result: Passed, 24 total, 24 passed, 0 failed (color block, deferred accessories and fallback-save coverage)

& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -nographics -projectPath . -executeMethod AlbaWorld.Editor.BuildTools.BuildAndroidApk -logFile work/task7-android-apk.log
# result: blocked; AndroidPlayerBuildProgram repeatedly exited 4 while com.unity.burst/.Runtime\\bcl.exe ran; no APK was produced

& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe' devices
# result: daemon started, no devices/emulators listed; install/offline smoke test could not run
```

`PetLoadoutData.colorId` now applies a shared-material color multiplier through renderer property blocks (`petcolor.sunny` identity and `petcolor.cocoa` warm brown), without duplicating materials. `accessoryIds` remain persisted and broadcast to hooks, but 3D accessory rendering is explicitly deferred until approved assets exist; no placeholder geometry or new art is added. If restore encounters an unknown pet ID, it instantiates `pet.cat`, saves the repaired JSON once, and a save/reload test confirms the invalid ID is not repeated.

The focused Edit Mode tests cover:

- 24 manifest entries, unique IDs, safe local source paths and CC0 metadata;
- 24 prefab references, shared URP Simple Lit material/colormap, finite bounds, unit roots, no cameras/lights and the 7,000-triangle ceiling;
- one catalog entry per ID, `Pet` category, `Pet` slot, prefab path and both localization tables.

The final review set is 24 Play Mode tests, recorded in `work/task7-green-playmode-final.xml`. It covers selection of every species, follow motion, invalid-asset fallback, save migration, persistence, room placement and photo-context reuse, plus color property blocks, explicitly deferred accessories, and persisted unknown-pet repair. The .NET suite covers offline save/schema behavior.

## Source and license

The source is archived at `Assets/Art3D/Pets/Source/KenneyCubePets` and mirrored for legal review at `docs/legal/assets/kenney-cube-pets-2.0`. `License.txt` identifies **Kenney — www.kenney.nl** and Creative Commons Zero (CC0). The integration manifest records the 24 source filenames and `https://www.kenney.nl`; attribution is retained in project documentation even though CC0 does not require it.

## Visual review and limitations

No new model, texture or character asset was created. A temporary editor-only helper instantiated the 24 local Kenney prefabs in a GPU-backed Unity Game View-style scene, added review labels, rendered both requested ratios, and was removed after capture:

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod AlbaWorld.Editor.KenneyPetGridCaptureTemp.Capture -logFile work/task6-kenney-grid-gpu.log
```

The command intentionally omits `-nographics`; Unity exited with code 0 and logged both captures. The archived outputs are:

- [`Art/Reviews/Pets/kenney-pets-grid-16x9.png`](../../Art/Reviews/Pets/kenney-pets-grid-16x9.png) — 1600×900 (16:9).
- [`Art/Reviews/Pets/kenney-pets-grid-20x9.png`](../../Art/Reviews/Pets/kenney-pets-grid-20x9.png) — 2000×900 (20:9).

Manual inspection confirmed all 24 labeled pets are visible, centered in a 6×4 grid, with readable labels and no character assets in the capture scene. Existing character render failures were not changed or retried. The temporary helper and its `.meta` file were removed; only the two PNG review outputs are retained.

## Handoff

The Kenney pet subsystem is ready for the next subsystem, **rooms/furniture**. That work may connect the already-selected pet to room spawn/placement and photo framing, but must not silently expand into new pet art, accessories, animation sets or network services.
