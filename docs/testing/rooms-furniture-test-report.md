# Rooms & Furniture Verification

Date: 2026-07-15  
Unity: 6.3.19f1 (\`D:\Unity\Hub\Editor\6000.3.19f1\`)

## Source audit

- Package: Kenney Furniture Kit 1.0
- Official source: https://kenney.nl/assets/furniture-kit
- License: Creative Commons Zero (CC0)
- Download archive: \`D:\AlbaWorldAssets\KenneyFurnitureKit-1.0\`
- Imported models: \`bedSingle\`, \`loungeSofa\`, \`table\`, \`chairCushion\`, \`bookcaseOpen\`, \`lampRoundFloor\`, \`pottedPlant\`, \`rugRound\`, \`books\`
- Project license record: \`docs/legal/assets/kenney-furniture-kit-1.0\`

## Automated evidence

| Check | Result |
| --- | --- |
| \`KenneyFurniturePrefabTests\` | 1/1 passed — \`work/furniture-prefab-green.xml\` |
| \`KenneyFurnitureCatalogTests\` | 1/1 passed — \`work/furniture-catalog-green.xml\` |
| \`RoomFurnitureTests\` | 2/2 passed — \`work/rooms-playmode-green.xml\` |
| Full Play Mode suite | 19/19 passed — \`work/rooms-playmode-final.xml\` |
| Full Edit Mode suite (GPU) | 68/68 passed — \`work/rooms-editmode-gpu.xml\` |
| Fresh Unity compile | passed — \`work/rooms-compile-final.log\` |
| Main scene root | one \`AlbaWorld3DApp\`, zero old \`Alba World App\` |

## Covered behavior

- Nine furniture prefabs contain renderable meshes.
- Catalog references point to the generated prefabs.
- Add, bounded move, scale, mirror, remove, and save operations work offline.
- Sunny and cozy rooms maintain independent layouts.
- Mirrored furniture survives a room switch and restore.
- No prototype procedural runtime files were restored.

## Known limitation

The Windows standalone player has a pre-existing Unity \`level0 corrupted\` failure before managed scene startup. Unity Editor compilation and focused Edit/Play Mode tests pass; Android packaging remains a separate gate.

The same Edit Mode suite reports one pre-existing GPU render-review limitation only when forced with \`-nographics\`; the normal GPU-backed run passes all 68 tests.
