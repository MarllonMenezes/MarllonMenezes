# Alba World — continuação futura

Este documento registra o handoff auditado em 15 de julho de 2026, após a integração offline dos pets Kenney. O worktree ainda contém alterações pendentes de personagens da antiga Tarefa 6; elas foram preservadas e não fazem parte do commit de auditoria dos pets.

## Identificação e ambiente

- Jogo: **Alba World**
- Estúdio: **Alba World Games**
- Pacote Android: `com.albaworldgames.albaworld`
- Projeto ativo: `C:\Users\Alba\Documents\Codex\2026-07-12\new-chat\.worktrees\alba-world-3d`
- Branch: `feature/alba-world-3d`
- Unity 6.3: `D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe`
- Blender 4.5: `D:\Blender\4.5\blender.exe`

Não abrir o mesmo projeto simultaneamente em duas instâncias do Unity.

## Subsistema Kenney concluído

O pacote local Kenney Cube Pets 2.0 foi copiado sem criação de arte nova. Os 24 IDs estáveis (`pet.beaver` até `pet.tiger`) estão definidos uma vez em `KenneyPetIds.All`; `pet.cat` e `pet.dog` permanecem preservados. Cada entrada tem definição, visual, prefab local, regra de escala/pivô, categoria `Pet`, slot `Pet` e chaves `item.pet.*` em `pt-BR` e `en`.

Os prefabs usam a textura compartilhada e o material URP Simple Lit em `Assets/Art3D/Pets`. O assembly runtime seleciona e substitui pets com fallback seguro para `pet.cat`; o follow controller, save/schema 3, casa e modo foto reutilizam a instância selecionada. Nenhum desses fluxos depende de rede, pacote runtime ou importador em execução.

O painel de configurações exibe o crédito localizado `Kenney — www.kenney.nl` em `pt-BR` e `en`. `PetLoadoutData.colorId` é consumido no visual 3D por `MaterialPropertyBlock`, sem duplicar materiais (`petcolor.sunny` identidade; `petcolor.cocoa` marrom quente). `accessoryIds` permanecem persistidos e enviados aos hooks, mas a renderização 3D está explicitamente adiada até a aprovação de assets compatíveis. Se um ID de pet desconhecido for restaurado, o fallback `pet.cat` é salvo uma vez; a recarga não repete o ID inválido.

Origem e licença:

- Fonte e manifesto: `Assets/Art3D/Pets/Source/KenneyCubePets`
- Registro legal: `docs/legal/assets/kenney-cube-pets-2.0/License.txt` e `manifest.json`
- Licença: Creative Commons Zero (CC0)
- Crédito conservado: **Kenney — www.kenney.nl**

## Auditoria e evidências

### Historical Task 6 baseline

The original Task 6 evidence below is retained for traceability only. Its Play Mode result (21/21 in `work/task6-kenney-playmode.xml`) predates the final review corrections and is not the current acceptance result.

O relatório completo está em [`docs/testing/kenney-pets-test-report.md`](testing/kenney-pets-test-report.md). Os comandos novos produziram:

- manifesto Edit Mode: 2/2 aprovados (`work/task6-kenney-source.xml`);
- prefabs Edit Mode: 2/2 aprovados (`work/task6-kenney-prefab.xml`);
- catálogo Edit Mode: 2/2 aprovados (`work/task6-kenney-catalog.xml`);
- Play Mode: 21/21 aprovados (`work/task6-kenney-playmode.xml`);
- testes .NET: 14/14 aprovados (`dotnet test Tools\\CoreTests\\AlbaWorld.CoreTests.csproj --no-restore`);
- auditoria temporária de triângulos: 1/1 aprovada (`work/task6-kenney-triangles.xml`, helper removido após a medição).

Os 24 prefabs medem entre 422 e 951 triângulos. Isso atende ao teto atual de 7.000 triângulos validado pelo teste; fica abaixo do alvo de design de 4.000–7.000 para um pet equipado e deve ser considerado no balanceamento visual/performance futuro, sem adicionar geometria nesta etapa.

### Final review evidence (current)

- Catalog Edit Mode: 3/3 passed (`work/task7-green-credit-final.xml`), including the localized in-game credit.
- Play Mode: 24/24 passed (`work/task7-green-playmode-final.xml`), covering the complete final pet set plus color, deferred-accessory, and fallback-save behavior.
- .NET tests: 14/14 passed (`dotnet test Tools\\CoreTests\\AlbaWorld.CoreTests.csproj --no-restore`).
- Android release gate: **blocked**, not approved. The development APK pipeline repeatedly returned Burst `bcl.exe`/`AndroidPlayerBuildProgram` `ExitCode: 4`, produced no APK, and `adb devices` listed no devices or emulators. See `work/task7-android-apk.log`.

## Revisão visual e limitações conhecidas

Foi gerada uma grade GPU-backed exclusivamente com os 24 prefabs Kenney (6×4, rótulos de espécie) por um helper editor-only temporário. O helper foi removido após a captura e não alterou modelos, materiais, prefabs ou personagens. Os PNGs arquivados são:

- `Art/Reviews/Pets/kenney-pets-grid-16x9.png` — 1600×900 (16:9)
- `Art/Reviews/Pets/kenney-pets-grid-20x9.png` — 2000×900 (20:9)

Comando executado sem `-nographics` (Unity usou o backend gráfico e saiu com código 0):

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod AlbaWorld.Editor.KenneyPetGridCaptureTemp.Capture -logFile work/task6-kenney-grid-gpu.log
```

A inspeção confirmou as 24 espécies visíveis, centralizadas e legendadas nos dois formatos. As duas falhas de render `-nographics` já conhecidas dos personagens não foram corrigidas, repetidas nem incluídas neste escopo. A revisão futura de rooms/furniture deve preservar esse limite e não alterar os assets de personagens.

As alterações não consolidadas da antiga Tarefa 6 de personagens continuam no worktree (modelos, materiais, prefabs, scripts, testes e imagens). Não fazer limpeza, restauração ou commit amplo delas ao integrar este handoff.

## Próxima etapa: polish and Android release

O subsistema rooms/furniture foi implementado no fluxo 3D ativo. A próxima etapa é polir personagens/roupas e concluir a preparação Android, mantendo o projeto offline, bilíngue e sem multiplayer.

## Rooms/furniture concluído

The rooms/furniture slice is now implemented on the active 3D flow. Kenney Furniture Kit 1.0 (CC0) is archived at \`D:\AlbaWorldAssets\KenneyFurnitureKit-1.0\`, with project copies under \`Assets/Art3D/Furniture/Source/KenneyFurnitureKit\` and legal records under \`docs/legal/assets/kenney-furniture-kit-1.0\`. Nine real models are generated as prefabs and linked by the 3D catalog.

The runtime controller keeps separate \`room.sunny\` and \`room.cozy\` layouts in \`GameSaveData.rooms3D\`. Furniture can be added, selected, dragged within bounds, resized, mirrored, reordered, removed, and restored offline. The character, selected Kenney pet, room furniture, and photo capture remain in the same scene.

Focused evidence: \`work/furniture-prefab-green.xml\` (1/1), \`work/furniture-catalog-green.xml\` (1/1), and \`work/rooms-playmode-green.xml\` (2/2).

## Assets externos

## Atualização: fluxo de primeira execução do MVP

Em 15/07/2026 foi implementado o marco de primeira execução aprovado pelo usuário:

- `GameSaveData.onboardingCompleted` e migração idempotente para schema 5;
- Welcome com título, instruções de seleção/arraste/modos, botão Jogar e troca de idioma;
- Casa só habilita movimento depois de sair da Welcome;
- cômodo atual visível na barra superior, com nomes localizados para `room.sunny` e `room.cozy`;
- troca de idioma reconstrói Welcome, Casa ou Vestir sem perder o estado;
- build Windows local regenerado em `Builds/AlbaWorldWindows/AlbaWorld.exe`.

Evidência reproduzível: [`docs/testing/first-run-mvp-test-report.md`](testing/first-run-mvp-test-report.md).

## Atualização: Casa, Vestir e sala jogável

Esta atualização substitui a antiga HUD sobreposta por dois modos claros no mesmo cenário:

- **Casa**: personagem anda por clique/toque ou WASD/setas; o centro da sala fica reservado para caminhar.
- **Vestir**: a movimentação e os controles de móveis ficam ocultos; categorias e itens aparecem em cartões com texto localizado.
- **Móveis**: os modelos Kenney são centralizados pelo pivô visual, entram em slots periféricos, não podem ocupar o centro nem sobrepor outro móvel, e possuem `Excluir` e `Desfazer`.
- **Persistência**: posição do personagem, layout por cômodo e loadout de roupas continuam no JSON local existente, sem novo servidor ou conta.
- **Layout**: safe area de 2%–98%, barra superior e dock inferior separados do conteúdo, sem `PetCard` antigo e sem palco cilíndrico central.

O relatório reproduzível desta etapa está em [`docs/testing/house-dress-layout-test-report.md`](testing/house-dress-layout-test-report.md). Para testar no PC, abra `Assets/Scenes/Main.unity`, escolha `Game > 16:9` ou `20:9` e pressione Play; o mouse simula toque e o teclado usa WASD/setas.

Para qualquer asset futuro, registrar licença comercial, redistribuição, atribuição, URL, autor, restrições editoriais, compatibilidade de rig, polígonos, texturas e impacto Android antes da importação. O pacote Kenney acima já possui esses registros legais; não importar outros pacotes Kenney sem decisão explícita.
