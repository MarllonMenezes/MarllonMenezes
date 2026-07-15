# Alba World — Rooms & Furniture Design

## Objetivo

Substituir o palco genérico por dois cômodos decoráveis offline, usando modelos 3D do Kenney Furniture Kit 1.0 (CC0) e preservando personagem, pet e foto no mesmo contexto.

## Escopo aprovado

- Dois ambientes alternáveis: `room.sunny` e `room.cozy`.
- Móveis importados do Furniture Kit: cama, sofá, mesa, cadeira, estante, luminária, planta, tapete e livros.
- Catálogo existente continua sendo a fonte de IDs, traduções e regras de posicionamento.
- Arrastar com mouse/toque, escala em passos, espelhar, trazer à frente, enviar para trás e remover.
- Limites de posicionamento no piso do cômodo; itens de parede e superfície permanecem fora desta primeira fatia visual.
- Layout independente por cômodo em `GameSaveData.rooms3D`.
- Nenhuma arte nova criada; fonte, licença e arquivos usados ficam registrados em `docs/legal/assets/kenney-furniture-kit-1.0`.

## Arquitetura

`FurnitureAssetSetup` importa os FBX aprovados e gera prefabs determinísticos. `RoomFurnitureController` instancia os prefabs do `ItemCatalog3D`, mantém IDs de instância e converte transformações para `FurniturePlacementData`. `AlbaWorld3DApp` apenas encaminha comandos da interface e mantém a captura de foto no mesmo cômodo.

## Persistência e segurança

Cada operação válida salva imediatamente o layout do cômodo ativo. IDs desconhecidos são ignorados durante restauração, sem destruir itens válidos. O controlador nunca acessa rede, física ou `Rigidbody`; o limite é aplicado em X/Z e o Y é fixado no piso.

## Critérios de aceite

1. Os nove prefabs importados têm `MeshRenderer` e material válido.
2. O catálogo resolve todos os nove IDs de móveis para prefabs reais.
3. Arrastar não ultrapassa os limites do cômodo e restaura após recarregar.
4. Alternar cômodo preserva layouts independentes.
5. Edit Mode, Play Mode e compilação Unity passam sem retorno do protótipo procedural antigo.
