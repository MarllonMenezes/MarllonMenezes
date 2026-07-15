# Alba World — Integração dos pets Kenney

**Data:** 15/07/2026  
**Status:** Aprovado pelo usuário para implementação  
**Projeto:** Alba World / `com.albaworldgames.albaworld`

## Objetivo

Adicionar ao MVP os 24 animais do pacote local `outputs/kenney_cube-pets_1.0`, usando os modelos gratuitos com licença Creative Commons Zero (CC0). Cada espécie será selecionável offline, persistida no save local, exibida no editor de pet, disponível na casa e incluída no modo foto.

Nenhuma arte nova será criada nesta etapa. O pacote Kenney permanecerá arquivado com sua licença como registro de origem. O crédito “Kenney — www.kenney.nl” será incluído nos créditos do jogo, embora a licença CC0 não o exija.

## Abordagem escolhida

Será usada importação direta dos arquivos FBX. A fonte será copiada para o projeto em `Assets/Art3D/Pets/Source/KenneyCubePets`, com a textura compartilhada em `Assets/Art3D/Pets/Textures`. Cada FBX será convertido pelo Unity em um prefab local e receberá um material URP Simple Lit compatível com Android.

GLB em runtime foi rejeitado porque adicionaria um importador/dependência e aumentaria o risco de build. Recriar ou rigar os modelos no Blender foi rejeitado porque não é necessário para o MVP e criaria trabalho de arte adicional.

## Catálogo e IDs

Os IDs são permanentes e não serão reutilizados:

| ID | Arquivo fonte | Nome de exibição |
| --- | --- | --- |
| `pet.beaver` | `animal-beaver.fbx` | Beaver / Castor |
| `pet.bee` | `animal-bee.fbx` | Bee / Abelha |
| `pet.bunny` | `animal-bunny.fbx` | Bunny / Coelho |
| `pet.cat` | `animal-cat.fbx` | Cat / Gato |
| `pet.caterpillar` | `animal-caterpillar.fbx` | Caterpillar / Lagarta |
| `pet.chick` | `animal-chick.fbx` | Chick / Pintinho |
| `pet.cow` | `animal-cow.fbx` | Cow / Vaca |
| `pet.crab` | `animal-crab.fbx` | Crab / Caranguejo |
| `pet.deer` | `animal-deer.fbx` | Deer / Cervo |
| `pet.dog` | `animal-dog.fbx` | Dog / Cachorro |
| `pet.elephant` | `animal-elephant.fbx` | Elephant / Elefante |
| `pet.fish` | `animal-fish.fbx` | Fish / Peixe |
| `pet.fox` | `animal-fox.fbx` | Fox / Raposa |
| `pet.giraffe` | `animal-giraffe.fbx` | Giraffe / Girafa |
| `pet.hog` | `animal-hog.fbx` | Hog / Javali |
| `pet.koala` | `animal-koala.fbx` | Koala / Coala |
| `pet.lion` | `animal-lion.fbx` | Lion / Leão |
| `pet.monkey` | `animal-monkey.fbx` | Monkey / Macaco |
| `pet.panda` | `animal-panda.fbx` | Panda / Panda |
| `pet.parrot` | `animal-parrot.fbx` | Parrot / Papagaio |
| `pet.penguin` | `animal-penguin.fbx` | Penguin / Pinguim |
| `pet.pig` | `animal-pig.fbx` | Pig / Porco |
| `pet.polar` | `animal-polar.fbx` | Polar Bear / Urso-polar |
| `pet.tiger` | `animal-tiger.fbx` | Tiger / Tigre |

`pet.cat` e `pet.dog` já existem no modelo de save e continuarão sendo os padrões iniciais. Os outros 22 IDs serão adicionados sem alterar ou remover IDs antigos.

Cada entrada terá categoria `Pet`, slot `Pet`, prefab obrigatório, regra de escala por espécie e chave de localização `item.pet.<animal>`. As opções de cor existentes (`petcolor.sunny`, `petcolor.cocoa`) continuarão como estado separado; quando o material compartilhado permitir, a cor será aplicada como multiplicador, sem duplicar texturas.

## Arquitetura de runtime

Será criado um assembly visual de pet que recebe `PetLoadoutData` e resolve o prefab pelo `IItemCatalog3D`. O componente deve:

1. remover o visual anterior;
2. instanciar o prefab do ID salvo;
3. aplicar escala e offset da entrada;
4. aplicar cor e acessórios compatíveis;
5. registrar o estado imediatamente no `ISaveService`.

O comportamento de acompanhamento será deliberadamente simples e determinístico: o pet interpola posição e rotação para um ponto atrás do personagem, sem `Rigidbody`, sem navegação e sem depender da rede. Em casa e no modo foto, o mesmo objeto poderá ser posicionado pelo editor e preservará o layout local.

## Importação e desempenho

- A textura Kenney compartilhada será importada uma vez, com compressão adequada ao Android.
- Materiais usarão URP Simple Lit, sem shader customizado.
- O importador aceitará o material/mesh do FBX, mas removerá câmeras, luzes e objetos auxiliares não necessários.
- A validação registrará triângulos por espécie; o orçamento alvo para pets continua entre 4.000 e 7.000 triângulos quando medido no prefab equipado.
- Prefabs terão escala e pivô revisados para que animais grandes (elefante/girafa) não sejam inutilizáveis no quarto e animais pequenos (abelha/peixe) permaneçam legíveis.

## Localização e licença

Serão adicionadas chaves em português brasileiro e inglês para os 24 nomes. O texto de créditos conterá a atribuição recomendada pela licença. O arquivo original `License.txt`, a URL do pacote e uma cópia do manifesto de integração ficarão em `docs/legal/assets/kenney-cube-pets-2.0/`.

## Testes e aceitação

### Edit Mode

- os 24 IDs existem exatamente uma vez;
- cada entrada pertence à categoria `Pet` e tem prefab;
- cada chave de localização existe em `pt-BR` e `en`;
- cada prefab tem mesh, material e bounds válidos;
- o pacote não adiciona permissões Android ou dependências de rede.

### Play Mode

- selecionar cada espécie atualiza o visual;
- fechar e reabrir restaura cada seleção;
- o pet segue o personagem dentro do limite definido;
- trocar de cômodo não perde o pet;
- modo foto captura o pet selecionado;
- falha de carregamento de asset deixa o último pet válido e não corrompe o save.

### Aceitação manual

Testar a primeira execução offline, proporções 16:9 a 20:9, aparelho/emulador com 2 GB de RAM e instalação Android sem dependência de internet. Nenhuma espécie deve exigir download após a instalação.

## Fora do escopo desta etapa

- animações complexas específicas por espécie;
- criação de modelos, texturas ou acessórios novos;
- multiplayer, conta, chat ou compartilhamento direto;
- compras ou desbloqueios pagos;
- importação dos demais pacotes Kenney ainda não escolhidos.
