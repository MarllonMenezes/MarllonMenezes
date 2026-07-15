# Alba World — migração para personagens Cartoon City

Status: proposta para revisão do usuário  
Data: 15/07/2026  
Projeto: Alba World (`com.albaworldgames.albaworld`)

## Decisão aprovada

O personagem artesanal atual será substituído por personagens prontos do pacote **Cartoon City Characters**, de RG Poly. A versão gratuita oferece 17 personagens em FBX e glTF, com esqueleto e animações compartilhados, e é publicada sob CC0 para uso comercial. A integração usará **FBX direto no Unity** para não adicionar loader runtime nem dependência de rede.

Fontes de referência:

- Página do pacote: https://rg-poly.itch.io/cartoon-city-massive-pack-characters
- Registro de licença: Creative Commons Zero 1.0 Universal, confirmado na página do pacote.

O arquivo original será baixado primeiro para `D:\AlbaWorldAssets\RGPolyCartoonCityCharacters\` e somente a versão gratuita selecionada será copiada para `Assets/Art3D/Characters/Source/RGPolyCartoonCity`. O manifesto, a licença, a URL, a data de download e o hash do arquivo serão registrados em `docs/legal/assets/rg-poly-cartoon-city-characters/`.

## Objetivo do MVP

- Trocar os dois corpos artesanais por presets Cartoon City sem alterar a casa, pets, móveis, foto, idioma ou funcionamento offline.
- Manter um avatar por cena, com escolha de personagem completo, cor/material e acessórios que passarem na validação de encaixe.
- Manter o salvamento local e migrar saves existentes sem reutilizar IDs antigos.
- Tornar seleção e manipulação previsíveis: personagem, móvel e pet só podem ser controlados depois de um clique/toque explícito que os deixe selecionados.

## Fora deste escopo

- Não copiar personagens, UI ou assets de PK XD.
- Não adicionar multiplayer, conta, chat, compras ou dependência online.
- Não prometer cabelo/roupa independentes quando o modelo escolhido só fornece um personagem completo.
- Não importar a versão paga ou os arquivos-fonte do pacote sem nova autorização.

## Experiência e seleção

Será criado um controlador de seleção único para toda a cena. Cada personagem, pet ativo e móvel terá um componente de entidade selecionável, um collider de clique e um marcador visual de seleção.

Regra de entrada:

1. Toque/clique em uma entidade não selecionada apenas seleciona e consome o evento. Não move, arrasta, redimensiona, espelha, exclui nem muda a ordem visual nessa primeira interação.
2. O marcador fica visível e os controles da HUD são habilitados apenas para a entidade compatível.
3. Uma nova interação, já com a entidade selecionada, permite a operação: arrastar móvel/pet, usar controles de móvel ou tocar no chão para mover o personagem selecionado.
4. Toque/clique no chão sem personagem selecionado não move ninguém e mostra a instrução localizada para selecionar o personagem primeiro.
5. Toque no vazio limpa a seleção. Trocar para Vestir, Foto ou outro cômodo também limpa a seleção.
6. A ordem de raycast é HUD/UI, entidade selecionável e, por último, chão. Assim, tocar no personagem não dispara movimento para o ponto sob ele.

O pet continuará seguindo o personagem por padrão. Ao selecionar e arrastar o pet pela primeira vez, o modo passa para posicionamento manual e o seguir é pausado; uma ação localizada `Seguir personagem` restaura o comportamento. O estado manual/seguir será salvo junto com o cômodo.

## Presets e personalização

Será adicionado um catálogo separado de presets, sem alterar os IDs históricos de roupa:

```text
CartoonCityCharacterPreset
  id                 // imutável, por exemplo cartooncity.char.01
  displayKey         // chave pt-BR/en
  prefab             // FBX convertido em prefab Unity
  skinColorIds       // cores/materials que o renderer suporta
  accessorySlots     // slots aprovados após teste de encaixe
  triangleBudget     // limite para Android
  free               // true para os presets do arquivo gratuito
```

O modo Vestir passará a exibir:

- **Personagem:** presets completos Cartoon City;
- **Cor:** variações de material suportadas pelo preset;
- **Acessórios:** somente itens com collider, escala e ponto de encaixe validados;
- **Cabelo/roupa/sapato antigos:** permanecem reconhecidos pela migração, mas não serão aplicados sobre um preset incompatível.

O botão `Trocar personagem` deixará de alternar apenas `body.girl`/`body.boy` e abrirá a seleção de presets. O preset escolhido será aplicado atomicamente: se o prefab, rig ou material falhar, o personagem atual permanece intacto.

## Integração Unity

1. Importar somente FBX gratuito para `Assets/Art3D/Characters/Source/RGPolyCartoonCity`.
2. Configurar cada FBX como Humanoid, validar o avatar e reaproveitar o esqueleto/animações compartilhados.
3. Criar prefabs determinísticos em `Assets/Art3D/Characters/Prefabs/CartoonCity` com colliders de seleção, `Animator`, materiais URP e LOD quando necessário.
4. Substituir a resolução de `BodyGirl`/`BodyBoy` em `AlbaWorld3DApp` pelo catálogo de presets.
5. Adaptar `CharacterWardrobeController` para aplicar preset, cor e acessório aprovado, preservando seus eventos e o salvamento local.
6. Centralizar input de personagem, pet e móveis em um contrato de seleção, sem permitir que `CharacterMovementController` e `RoomFurnitureController` processem o mesmo toque duas vezes.

## Persistência e migração

O `schemaVersion` será incrementado. A migração manterá os IDs antigos e fará somente o mapeamento:

```text
body.girl -> cartooncity.char.01
body.boy  -> cartooncity.char.02
```

Se um preset não existir, será usado o primeiro preset gratuito válido e um aviso localizado será mostrado. A posição do personagem, layouts dos cômodos, pet, idioma, configurações e itens desbloqueados não serão apagados. A migração será idempotente e salvará imediatamente após uma conversão válida.

## Testes de aceitação

### Edit Mode

- manifesto/licença presentes e URL registrada;
- IDs de presets únicos e todos os prefabs carregáveis;
- todos os FBX aprovados como Humanoid e com skeleton compartilhado;
- triângulos, materiais e texturas dentro do orçamento Android;
- traduções de presets e ações de seleção presentes em `pt-BR` e `en`;
- migração `body.girl`/`body.boy` idempotente e sem perda de campos.

### Play Mode

- primeiro clique em personagem, pet ou móvel seleciona sem mover;
- segundo gesto move somente a entidade selecionada;
- entidade não selecionada não responde a excluir, escala, espelho ou ordem;
- clique no chão move apenas o personagem selecionado;
- pet alterna corretamente entre seguir e posicionamento manual;
- troca de preset, cor e acessório salva e restaura após reiniciar;
- layouts e seleção não vazam de um cômodo para outro;
- 16:9, 18:9 e 20:9 não cortam o modo Vestir.

### Dispositivo/PC

- primeira execução offline;
- perda de foco, pausa e fechamento durante seleção não corrompem o JSON;
- aparelho Android com 2 GB de RAM mantém a cena estável;
- pacote final permanece abaixo de 150 MB após importar somente a versão gratuita selecionada.

## Riscos e decisões de contenção

- **Variedade modular menor:** presets completos são assumidos como a personalização principal; peças independentes só entram após teste real.
- **Tamanho do pacote:** baixar e importar somente a versão gratuita e remover demos não utilizadas.
- **Rig/material incompatível:** validar um personagem piloto antes de converter os 17; em caso de falha, manter o fallback atual até a correção.
- **Seleção ambígua:** o controlador único consome o primeiro clique e bloqueia os sistemas antigos de movimento/drag nesse frame.

## Critério de pronto para implementação

A implementação começa somente depois da revisão deste documento. O primeiro marco será um único preset Cartoon City jogável, selecionável e salvo; depois os demais presets gratuitos serão adicionados em lotes pequenos com testes verdes.
