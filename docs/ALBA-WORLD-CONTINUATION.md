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

Origem e licença:

- Fonte e manifesto: `Assets/Art3D/Pets/Source/KenneyCubePets`
- Registro legal: `docs/legal/assets/kenney-cube-pets-2.0/License.txt` e `manifest.json`
- Licença: Creative Commons Zero (CC0)
- Crédito conservado: **Kenney — www.kenney.nl**

## Auditoria e evidências

O relatório completo está em [`docs/testing/kenney-pets-test-report.md`](testing/kenney-pets-test-report.md). Os comandos novos produziram:

- manifesto Edit Mode: 2/2 aprovados (`work/task6-kenney-source.xml`);
- prefabs Edit Mode: 2/2 aprovados (`work/task6-kenney-prefab.xml`);
- catálogo Edit Mode: 2/2 aprovados (`work/task6-kenney-catalog.xml`);
- Play Mode: 21/21 aprovados (`work/task6-kenney-playmode.xml`);
- testes .NET: 14/14 aprovados (`dotnet test Tools\\CoreTests\\AlbaWorld.CoreTests.csproj --no-restore`);
- auditoria temporária de triângulos: 1/1 aprovada (`work/task6-kenney-triangles.xml`, helper removido após a medição).

Os 24 prefabs medem entre 422 e 951 triângulos. Isso atende ao teto atual de 7.000 triângulos validado pelo teste; fica abaixo do alvo de design de 4.000–7.000 para um pet equipado e deve ser considerado no balanceamento visual/performance futuro, sem adicionar geometria nesta etapa.

## Limitações conhecidas

Não foi gerada uma grade Game view dos 24 pets em 16:9 e 20:9. O projeto não possui um capturador de pets, e o ambiente headless `-nographics` não oferece uma renderização Game view confiável. As duas falhas de render nographics já conhecidas dos personagens não foram corrigidas, repetidas nem incluídas neste escopo. A inspeção manual deve ocorrer em um editor com GPU antes do trabalho de rooms/furniture, sem alterar os assets de personagens.

As alterações não consolidadas da antiga Tarefa 6 de personagens continuam no worktree (modelos, materiais, prefabs, scripts, testes e imagens). Não fazer limpeza, restauração ou commit amplo delas ao integrar este handoff.

## Próxima etapa: rooms/furniture

O próximo subsistema é **rooms/furniture**. Ele pode criar shells de cômodos, móveis e regras de posicionamento, conectar o pet já selecionado ao spawn seguro da casa e preservar a mesma instância no modo foto. Deve continuar offline e não expandir silenciosamente para novos pets, acessórios, animações específicas, multiplayer ou serviços de rede. Antes de qualquer commit dessa etapa, repetir a auditoria de catálogo/persistência e revisar visualmente em 16:9 e 20:9.

## Assets externos

Para qualquer asset futuro, registrar licença comercial, redistribuição, atribuição, URL, autor, restrições editoriais, compatibilidade de rig, polígonos, texturas e impacto Android antes da importação. O pacote Kenney acima já possui esses registros legais; não importar outros pacotes Kenney sem decisão explícita.
