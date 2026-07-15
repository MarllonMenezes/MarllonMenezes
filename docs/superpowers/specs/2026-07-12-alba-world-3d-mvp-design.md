# Alba World — Design do MVP 3D

## Visão geral

Alba World será convertido do protótipo 2D atual para um jogo 3D horizontal, infantil, offline e voltado a aparelhos Android. A experiência combinará exploração em terceira pessoa com um modo separado de decoração, mantendo personalização de personagem, pet, casa e modo foto.

A referência de mercado é a energia alegre de jogos sociais infantis como PK XD, mas a identidade visual, as proporções, os personagens, as interfaces, os ambientes e todos os assets serão originais da Alba World Games. Nenhuma malha, textura, personagem, interface ou elemento reconhecível de terceiros será copiado.

## Objetivos

- Substituir os placeholders geométricos atuais por personagens, pets, roupas, móveis e cômodos 3D reconhecíveis.
- Entregar uma direção visual própria chamada **Alba Chibi Pop**.
- Permitir caminhada em terceira pessoa e decoração em uma câmera própria.
- Preservar funcionamento offline, salvamento local, português do Brasil e inglês.
- Manter desempenho de pelo menos 30 FPS em um aparelho Android com 2 GB de RAM.
- Manter o pacote final abaixo de 150 MB.

## Fora do escopo

- Mundo aberto, multiplayer, chat ou contas online.
- Física livre para móveis ou personagens.
- Combate, minigames ou veículos.
- Animações faciais complexas por captura de movimento.
- Visual fotorealista ou materiais pesados de console.

## Identidade visual

### Alba Chibi Pop

Os personagens terão aproximadamente 2,7 cabeças de altura, cabeça grande, olhos expressivos, mãos e calçados maiores e formas suaves. O visual será alegre, colorido e imediatamente legível em telas pequenas.

O acabamento será de **realismo fofo e arredondado**. Low-poly descreve somente a otimização da topologia, não uma aparência quadrada. As malhas usarão curvas orgânicas, sombreamento suave e silhuetas reconhecíveis.

- Pets terão focinho, orelhas, patas, cauda, pelagem pintada e anatomia identificável.
- Roupas terão gola, mangas, costuras, bolsos, tecidos e dobras simplificadas.
- Móveis terão pés, puxadores, almofadas, estofamento, madeira e proporções funcionais.
- Texturas pintadas, mapas de normal leves e materiais simples fornecerão detalhes sem geometria excessiva.
- Não serão usados personagens cúbicos, pets em forma de bloco ou móveis compostos apenas por caixas genéricas.

## Personagens

Haverá dois corpos-base infantis, menina e menino, com a mesma altura e o mesmo esqueleto humanoide. A diferenciação visual virá de tronco, rosto, cabelo e roupa. O esqueleto compartilhado permitirá reutilizar animações.

### Personalização modular

- Seis tons de pele.
- Quatro rostos iniciais e cores de olhos configuráveis.
- Quatro cabelos.
- Quatro roupas entre conjuntos e peças completas.
- Dois calçados.
- Quatro acessórios distribuídos entre cabeça, rosto, costas e mãos.

Os encaixes modulares serão definidos por slots imutáveis. Roupas incompatíveis com um corpo não serão exibidas naquele corpo. Cabelos poderão ocultar partes específicas sob chapéus para evitar interseções.

### Orçamento técnico

- Personagem completo equipado: 8 mil a 12 mil triângulos.
- Um esqueleto humanoide compartilhado.
- Até três materiais ativos no personagem completo, priorizando atlas de textura.
- Texturas principais de 1024 × 1024, com compressão Android adequada.
- LOD simplificado será usado apenas quando produzir ganho mensurável em cena.

### Animações iniciais

- Parado com pequenas variações.
- Caminhar e correr.
- Virar e parar.
- Sentar em pontos compatíveis.
- Acenar e duas poses para o modo foto.

## Pets

O MVP terá gato e cachorro no estilo Super Chibi, com cabeça maior, patas curtas e anatomia reconhecível. Cada espécie terá esqueleto próprio e compartilhará animações apenas entre variações da mesma espécie.

- Cores de pelagem configuráveis.
- Boné, laço e bandana como acessórios modulares.
- Animações de parado, andar, correr, sentar e reação alegre.
- Comportamento de seguir o personagem mantendo distância segura.
- Reposicionamento automático quando ficar preso ou muito distante.
- Orçamento de 4 mil a 7 mil triângulos por pet equipado.

## Cômodos, móveis e decoração

O jogo terá dois cômodos completos:

1. **Quarto ensolarado:** rosa, lavanda, creme e madeira clara.
2. **Sala-jardim:** menta, azul-claro, plantas e madeira suave.

O catálogo inicial terá aproximadamente 16 móveis e objetos decorativos, incluindo cama, sofá, mesa, cadeira, estante, luminária, tapete, almofada, livros, quadro, relógio, planta e pequenos objetos de apoio.

Cada prefab de mobília definirá:

- Área ocupada no chão.
- Limites de escala permitidos.
- Rotação em passos de 45 graus.
- Pontos de apoio para objetos menores.
- Pontos opcionais para sentar ou interagir.
- Colisor simplificado separado da malha visual.

As formas serão arredondadas e reconhecíveis. Detalhes aparentes virão de boa silhueta, materiais, texturas e mapas de normal, não de uma quantidade excessiva de polígonos.

## Catálogo e desbloqueios

O catálogo do MVP oferecerá aproximadamente 45 opções entre tons, rostos, cabelos, roupas, acessórios, pets, acessórios de pets e objetos de casa.

- Pelo menos 32 opções serão gratuitas.
- Pelo menos oito itens serão desbloqueáveis permanentemente por vídeo opcional.
- O limite continuará em dois desbloqueios por dia.
- Nenhuma função principal dependerá de internet ou de anúncios.

Os IDs existentes serão preservados ou migrados explicitamente. IDs removidos nunca serão reutilizados para outro conteúdo.

## Jogabilidade

### Exploração em terceira pessoa

- Controle virtual no Android.
- Teclado WASD, setas e mouse no PC.
- Câmera orbitando o personagem com limites para impedir atravessar paredes.
- Interações simples com portas, assentos e pontos específicos de móveis.
- Um personagem e um pet ativos por vez.

### Modo decoração

Um botão alternará para uma câmera ampla do cômodo. Personagem e pet ficarão parados enquanto o usuário edita o ambiente.

- Arrastar móveis pelo piso.
- Girar em passos de 45 graus.
- Aumentar ou diminuir dentro dos limites do prefab.
- Remover e restaurar objetos.
- Encaixar itens pequenos nos pontos de apoio.
- Validar limites do cômodo e impedir posicionamento fora da área útil.
- Salvar automaticamente cada alteração.

### Modo foto

- Ocultar toda a interface.
- Ajustar a câmera dentro de limites seguros.
- Selecionar poses básicas.
- Incluir personagem, pet e cômodo atual.
- Salvar PNG localmente usando o fluxo Android já definido pelo projeto.

## Arquitetura Unity

O projeto será convertido para Unity 6 URP 3D. A implementação continuará modular e aproveitará o catálogo, os serviços offline e o JSON versionado existentes.

### Sistemas principais

- `CharacterCustomization`: instancia corpo, rosto, cabelo, roupas e acessórios por slot.
- `ThirdPersonController`: movimento, rotação e estados de animação.
- `ThirdPersonCamera`: órbita, limites, colisão e reenquadramento.
- `PetFollower`: acompanhamento, animação e recuperação de posição.
- `DecorationEditor`: seleção, arraste, rotação, escala e encaixes.
- `RoomLoader`: carrega apenas o cômodo atual e seu layout.
- `InteractionSystem`: portas, assentos e pontos interativos.
- `PhotoMode`: poses, câmera, ocultação de interface e captura.

### Dados

Os assets continuarão definidos por IDs imutáveis. Os itens 3D apontarão para prefabs e metadados de encaixe. O save JSON armazenará somente estado local:

- Corpo, pele, rosto, cabelo, roupas, calçados e acessórios equipados.
- Pet, cor e acessórios equipados.
- Cômodo atual.
- Posição e orientação do personagem.
- Para cada móvel: ID, posição, rotação, escala válida e ponto de apoio opcional.
- Configurações, idioma, itens desbloqueados e limite diário de anúncio.

Uma nova versão de schema migrará saves do protótipo 2D para seleções 3D equivalentes quando houver correspondência.

## Tratamento de falhas

- Um ID ausente será ignorado e registrado no console; o restante do save continuará carregando.
- Um prefab incompatível não aparecerá no catálogo até ser corrigido.
- Posições inválidas serão limitadas à área útil do cômodo.
- Um personagem preso será movido para o ponto seguro mais próximo.
- O pet será reposicionado perto do personagem quando ficar inacessível.
- Falhas de anúncio não afetarão caminhada, personalização, decoração ou modo foto.
- Uma falha ao salvar a imagem exibirá erro localizado e preservará o jogo em execução.

## Produção de assets

Cada asset terá:

- Folha de conceito aprovada em PNG, com frente, lado e costas quando necessário.
- Arquivo-fonte `.blend` organizado.
- Exportação `.fbx` compatível com Unity.
- Texturas PNG e materiais documentados.
- Prefab Unity com escala, pivô, colisor e metadados corretos.

A produção seguirá esta ordem:

1. Conceitos visuais dos personagens, pets, roupas, móveis e cômodos.
2. Corpos-base, rostos, rig compartilhado e animações básicas.
3. Cabelos, roupas, calçados e acessórios.
4. Gato, cachorro e acessórios.
5. Cômodos, móveis e decoração.
6. Sistemas 3D, controles, decoração, interação e modo foto.
7. Otimização, testes Android e APK de validação.

## Testes

### Assets

- Encaixe de cada roupa nos dois corpos permitidos.
- Ausência de interseções graves em parado, caminhada e poses.
- Pesos do esqueleto e deformação de ombros, quadril, joelhos e mãos.
- Pivôs, escala, colisores e pontos de apoio dos móveis.
- Materiais e texturas após compressão Android.

### Edit Mode

- IDs duplicados e referências de prefab ausentes.
- Compatibilidade de slots e corpos.
- Serialização e migração do save 2D para 3D.
- Validação de posições, escalas e rotações.
- Limite diário e concessão idempotente de recompensas.

### Play Mode

- Movimento, câmera, colisões e animações.
- Seguimento e recuperação do pet.
- Entrada e saída do modo decoração.
- Encaixe de objetos e restauração do layout.
- Troca de idioma e carregamento após reiniciar.
- Modo foto sem elementos de interface.

### Aparelhos

- Android com 2 GB de RAM.
- Proporções de 16:9 a 20:9.
- Primeira execução totalmente offline.
- Atualização sem perda de progresso.
- Meta de 30 FPS e pacote abaixo de 150 MB.

## Critérios de aceitação

- Personagens, pets, roupas e móveis possuem formas suaves, reconhecíveis e não quadradas.
- Os dois corpos usam o mesmo esqueleto e reutilizam as animações previstas.
- O usuário pode caminhar, trocar para decoração, editar o cômodo e voltar à exploração.
- O pet acompanha o personagem e pode ser equipado e posicionado.
- Cada cômodo restaura seu layout após fechar e reabrir o jogo.
- Nenhuma função principal exige internet.
- O modo foto salva PNG localmente.
- O jogo mantém estabilidade e legibilidade em Android de entrada.
