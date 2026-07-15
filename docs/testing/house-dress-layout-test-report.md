# Alba World — validação Casa e Vestir

Data: 15/07/2026  
Unity: 6.3.19f1 (`D:\Unity\Hub\Editor\6000.3.19f1`)  
Projeto: `C:\Users\Alba\Documents\Codex\2026-07-12\new-chat`  
Branch: `feature/alba-world-mvp`

## Resultado automatizado

| Suíte | Resultado | Evidência |
| --- | ---: | --- |
| Unity Edit Mode completo | 72/72 aprovados | `work/house-dress-editmode.xml` |
| Unity Play Mode completo | 28/28 aprovados | `work/house-dress-playmode.xml` |
| .NET CoreTests | 14/14 aprovados | `dotnet test Tools\CoreTests\AlbaWorld.CoreTests.csproj --no-restore` |
| Contrato responsivo Casa/Vestir | 1/1 aprovado | `work/responsive-layout-green.xml` |
| Movimento limitado | 3/3 aprovados | `work/character-movement-green.xml` |
| Segurança de móveis + desfazer | 3/3 aprovados | `work/furniture-safety-green.xml` |
| Guarda-roupa local | 3/3 aprovados | `work/wardrobe-green2.xml` |

## O que foi verificado

- A HUD tem safe area, barra superior, dock inferior e conteúdo separado; os botões têm `LayoutElement`, texto TextMeshPro e âncoras normalizadas.
- Casa e Vestir são modos explícitos. Vestir desativa movimento e não destrói o personagem.
- O personagem é limitado ao retângulo central e salva `playerWorld.position` quando chega ao destino.
- Móveis Kenney são reposicionados pelo centro visual, entram nos slots do perímetro e são rejeitados no centro ou quando colidem com outro móvel.
- Arrastar um móvel inválido restaura a última posição válida; `Excluir` remove e `Desfazer` restaura uma vez dentro da janela local.
- Cabelo, pele, roupa, calçado e acessório validam catálogo, gratuidade/desbloqueio e compatibilidade corporal; as cores usam `MaterialPropertyBlock`.
- Os dicionários `pt-BR` e `en` contêm as novas chaves da HUD e do guarda-roupa.

## Teste manual no PC

1. Abra `Assets/Scenes/Main.unity` no Unity e pressione Play.
2. Em `Game`, alterne entre `16:9`, `18:9` e `20:9`; confirme que a barra superior e o dock não cortam textos.
3. Em Casa, clique no chão ou use WASD/setas para mover o personagem.
4. Abra Móveis, adicione itens e arraste-os para o perímetro; tente colocar um item sobre outro para confirmar que ele volta.
5. Selecione um móvel e use `Excluir`; em seguida use `Desfazer`.
6. Abra Vestir, selecione uma categoria e aplique um item gratuito; volte para Casa e troque o idioma.
7. Feche e reabra o Play Mode para confirmar posição, móveis e roupa salvos.

O Device Simulator oficial não é compatível com este editor; a simulação recomendada é a janela Game com presets de proporção. Nenhuma função principal desse roteiro depende de rede.

## Limitações ainda abertas

- Não foi declarado um build Android aprovado nesta etapa. A falha anterior do pipeline Android/Burst e a ausência de aparelho ou emulador continuam registradas em `work/task7-android-apk.log`.
- A captura visual manual em aparelhos Android, MediaStore e o fluxo de anúncios permanecem como gates de publicação e devem ser testados antes do envio à Play Store.
