# Alba World — Layout Casa e Vestir

**Data:** 2026-07-15  
**Status:** aprovado para especificação; implementação aguardando revisão deste documento  
**Objetivo:** tornar o MVP legível e jogável em telas móveis horizontais, separando casa, movimento, móveis e vestir em modos claros sem criar novos assets externos.

## Problemas observados

- O HUD atual coloca catálogo de pets, móveis, comandos e ações no mesmo espaço do viewport.
- Os móveis iniciais e os móveis adicionados podem bloquear a região central onde o personagem deveria ficar.
- O personagem permanece como apresentação estática, sem espaço ou controle de movimento.
- Não existe um fluxo visual separado para vestir o personagem.
- O botão de excluir não é percebido quando a seleção está distante do catálogo.
- Textos em português aparecem com codificação quebrada, por exemplo `MÃ³veis`.
- Áreas ancoradas por percentuais não garantem legibilidade nas proporções 16:9 a 20:9.

## Decisão de produto

O MVP usará uma única cena 3D com dois modos de interface:

1. **Casa:** explorar a sala, mover o personagem, escolher pet e decorar.
2. **Vestir:** visualizar o personagem em destaque e alterar os slots de aparência.

A troca de modo não carrega uma nova cena, não exige internet e preserva o estado local imediatamente.

## Alternativas avaliadas

### Uma cena com modos Casa e Vestir — escolhida

Reaproveita a câmera, o estado salvo e os prefabs existentes. A troca é instantânea e o código pode bloquear a interação de móveis enquanto o usuário veste o personagem.

### Cenas separadas

Isola a interface, mas exige serialização e restauração de estado em cada troca, além de aumentar o tempo de transição e a quantidade de cenas a manter.

### Painéis laterais recolhíveis

Reduz a troca de modo, mas deixa o viewport apertado em telas móveis e mantém a sobreposição que gerou o problema original.

## Layout visual

### Modo Casa

- Canvas em `Screen Space Overlay`, usando área segura e `CanvasScaler` com referência 1920×1080.
- Barra superior compacta com nome do cômodo, estado offline e ações `Vestir`, `Pet` e `Foto`.
- Viewport 3D central ocupando a maior área disponível.
- Dock inferior com aproximadamente 25% da altura, separado do viewport por uma margem fixa.
- O dock terá duas abas:
  - `Móveis`: grade de itens com cartões de tamanho uniforme e rótulos curtos.
  - `Ações`: troca de personagem, troca de cômodo, idioma e foto.
- Quando um móvel estiver selecionado, uma faixa de ações fixa no topo do dock exibirá `Menor`, `Maior`, `Espelhar`, `Frente`, `Atrás` e `Excluir`.
- `Excluir` ficará visualmente destacado, ficará desativado sem seleção e exibirá `Desfazer` por alguns segundos após a exclusão.
- Nenhum painel será criado sobre o personagem ou sobre o centro do viewport.

### Modo Vestir

- O dock de móveis e os controles de decoração ficam ocultos.
- O lado esquerdo mostra o personagem em escala maior, com pedestal e espaço vazio ao redor.
- O lado direito mostra categorias de aparência: `Pele`, `Cabelo`, `Roupa`, `Calçados` e `Acessórios`.
- Cada categoria usa cartões em duas colunas, com texto centralizado e quebra de linha controlada.
- `Voltar` restaura o modo Casa sem perder alterações; `Salvar` grava o loadout e retorna ao modo Casa.
- Os materiais/paletas e prefabs já presentes no repositório serão reutilizados. Nenhum novo asset externo será baixado ou criado nesta etapa.

## Espaço de jogo e movimento

- O piso da sala terá uma área lógica de caminhada central, separada das bordas de decoração.
- O personagem inicia no lado esquerdo dessa área; o pet começa próximo e segue o personagem.
- Toque/clique em um ponto livre do piso define o destino do personagem.
- No PC, WASD e setas são entradas equivalentes para validar o MVP sem dispositivo Android.
- O movimento será sem física, sem navegação online e com limite de sala.
- A entrada de movimento ficará desativada no modo Vestir e enquanto o ponteiro estiver sobre o dock.
- A posição do personagem será salva em `GameSaveData.playerWorld.position` e restaurada na próxima execução.

## Regras de móveis

- O layout inicial será reorganizado para as paredes e cantos; a área central ficará livre.
- Cada móvel terá uma caixa de ocupação derivada dos renderers/collider.
- Móveis que não são decoração de chão não poderão intersectar a zona de caminhada.
- Uma nova adição será procurada em slots livres pré-definidos; se não houver slot, a ação falhará com mensagem localizada e não criará uma instância sobre outra.
- Ao arrastar, a posição será validada continuamente. Se o destino intersectar outro objeto ou a zona proibida, o item volta à última posição válida.
- A regra de limite continuará sendo aplicada por cômodo e será persistida em `rooms3D`.
- A seleção terá uma indicação visual simples no objeto e manterá `SelectedInstanceId` como fonte única para os controles.

## Legibilidade e localização

- `LanguageService.cs` será salvo explicitamente em UTF-8 e todas as strings quebradas serão corrigidas em `pt-BR` e `en`.
- Os textos de runtime usarão TextMeshPro, com fallback apenas se o asset padrão da biblioteca não estiver disponível.
- Botões terão altura mínima, padding interno e quebra de linha; nenhum texto dependerá de overflow horizontal.
- O layout será validado em 16:9, 18:9 e 20:9.
- As mensagens de erro, sucesso, exclusão, ausência de slot e troca de modo serão localizadas.

## Componentes e limites

- `AlbaWorld3DApp`: ciclo de vida, câmera, personagem, pet, cômodo e persistência.
- `AlbaWorldUiController` (novo): construção do Canvas, modos Casa/Vestir, dock, seleção de categoria e mensagens.
- `CharacterMovementController` (novo): clique/toque, WASD/setas, limites e persistência de posição.
- `CharacterWardrobeController` (novo): leitura do catálogo, aplicação dos slots suportados e atualização do `CharacterLoadoutData`.
- `RoomFurnitureController`: seleção, arraste, slots livres, colisão lógica, zona de caminhada, exclusão e persistência.
- `LanguageService`: textos UTF-8, pt-BR/en e chaves novas de interface.

Os componentes deverão comunicar-se por métodos e eventos pequenos; a UI não manipulará diretamente os dicionários internos de persistência.

## Fluxo de dados

1. `AlbaWorld3DApp.Start` carrega e migra `GameSaveData`.
2. O controlador de movimento restaura `playerWorld.position`.
3. O controlador de móveis restaura o cômodo, sanitiza placements antigos e seleciona nenhum item.
4. A UI inicia no modo Casa e cria somente o dock correspondente.
5. `Vestir` troca o modo, bloqueia movimento/decoração e abre o controlador de aparência.
6. Uma escolha de roupa atualiza o loadout, aplica o material/prefab disponível e salva imediatamente.
7. `Excluir` remove o placement selecionado, salva imediatamente e oferece `Desfazer` local.
8. Fechamento, pausa e troca de modo chamam a persistência idempotente.

## Compatibilidade com saves existentes

- Nenhum ID existente será removido ou reutilizado.
- Layouts antigos serão normalizados na carga; placements inválidos ou fora da área serão reposicionados para o primeiro slot livre compatível.
- A posição padrão do personagem será usada quando `playerWorld` estiver ausente ou fora dos limites.
- A versão do schema só será incrementada se a migração realmente adicionar campos novos; o plano deve incluir teste de migração correspondente.

## Testes de aceitação

### Edit Mode

- Strings pt-BR não contêm sequências de mojibake (`Ã`, `Â`, `�`).
- Todas as chaves de Casa, Vestir, movimento, exclusão e desfazer existem em pt-BR e en.
- Slots de móveis não se sobrepõem e a zona de caminhada é respeitada.
- Catálogo de roupas mantém IDs existentes e não cria assets externos.
- Serialização preserva `playerWorld.position`, cômodo e placements.

### Play Mode

- A cena inicia no modo Casa sem painéis sobrepostos.
- O personagem se move por toque/clique e WASD/setas dentro dos limites.
- O pet acompanha o personagem.
- Adicionar ou arrastar um móvel sobre outro ou sobre a zona proibida é recusado/revertido.
- Selecionar um móvel habilita `Excluir`; excluir remove, salva e permite desfazer.
- Entrar e sair do modo Vestir oculta/restaura corretamente o dock.
- Trocar categoria e item preserva o loadout ao fechar e reabrir a cena.
- A interface permanece legível em 16:9, 18:9 e 20:9.

## Critérios de conclusão

- Nenhuma tela apresenta texto sobreposto ou cortado.
- Há uma área central clara para o personagem andar.
- O fluxo Casa → Vestir → Casa é utilizável sem internet.
- O botão de excluir é encontrado sem procurar pelo layout.
- O projeto usa somente assets já aprovados no repositório nesta etapa.
- Os testes Edit Mode, Play Mode e .NET passam antes do commit da implementação.
