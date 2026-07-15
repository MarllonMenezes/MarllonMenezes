# Alba World — fluxo de primeira execução do MVP

Status: aprovado pelo usuário em 15/07/2026  
Projeto: Alba World (`com.albaworldgames.albaworld`)

## Objetivo

Transformar a fatia 3D existente em uma primeira execução compreensível para uma criança: o jogo abre em uma tela de boas-vindas, explica a regra de seleção, entra na Casa com dois cômodos identificáveis e mantém Vestir, Foto, idioma e salvamento em superfícies separadas.

Esta etapa não importa arte nova, não adiciona rede, anúncios reais ou contas. Ela usa os personagens Cartoon City, pets Kenney e móveis Kenney já registrados no projeto.

## Fluxo aprovado

1. `AlbaWorld3DApp` carrega o save local, cria a cena e monta a interface.
2. Se `onboardingCompleted` for falso, a interface inicia em `Welcome`; caso contrário, inicia em `Casa`.
3. A tela Welcome exibe nome do jogo, estúdio, idioma atual, três instruções curtas (selecionar, arrastar, trocar de modo) e o botão `Jogar`.
4. Ao tocar `Jogar`, o save marca `onboardingCompleted = true`, grava imediatamente e a interface entra em Casa.
5. Casa mantém o centro livre para andar. A barra superior mostra o cômodo atual como um botão de seleção; tocar nele alterna explicitamente entre `room.sunny` e `room.cozy`, atualiza o texto localizado e restaura o layout próprio do cômodo.
6. Vestir continua em uma tela dedicada, com botão Voltar e botão Salvar. Foto continua acessível pela barra superior e não altera o modo atual.
7. Troca de idioma reconstrói a tela ativa sem perder o save, a seleção ou o cômodo.

## Arquitetura

- `GameSaveData.onboardingCompleted` é o único novo estado persistido. `SaveMigration.CurrentSchemaVersion` sobe para 5; saves antigos continuam válidos e começam com a Welcome uma vez.
- `AlbaWorldUiMode` ganha `Welcome`. `AlbaWorldUiController` constrói e destrói a raiz da Welcome da mesma forma que Casa/Vestir, mantendo a responsabilidade de layout em um único componente.
- `AlbaWorldUiController.Initialize` recebe um callback `StartGame`. O app decide apenas a transição e a persistência; a UI não edita o JSON diretamente.
- O nome do cômodo é mantido em um campo de UI atualizado por `SetRoomName`, evitando texto antigo após a troca.
- `AlbaWorld3DApp.OnUiModeChanged` só habilita movimento em Casa; Welcome e Vestir bloqueiam movimento. O mundo continua renderizado atrás da Welcome, mas todas as ações ficam bloqueadas pela tela de overlay.

## Layout e acessibilidade

- Canvas em `1920x1080`, safe area de 2% a 98%, com cartões grandes e mínimo de 44 px para toque.
- Welcome usa uma coluna central: título, subtítulo, três cartões de instrução e dois botões inferiores (`Jogar`, idioma). Nenhum controle ocupa o centro da sala antes de iniciar.
- A barra de Casa reserva a faixa superior ao título, cômodo, idioma, Vestir e Foto; o dock inferior reserva ações de móveis e pets.
- Todos os textos novos têm chaves `pt-BR` e `en`; caso uma chave não exista, a UI usa um rótulo curto de fallback em vez de ficar vazia.

## Erros e comportamento offline

- Falha de leitura do save mantém o comportamento existente de iniciar com defaults e Welcome.
- Falha de escrita não impede entrar em Casa; uma mensagem localizada informa que o progresso será tentado novamente no próximo ciclo.
- Nenhuma função principal depende da internet ou de um asset baixado durante a execução.

## Testes de aceitação

### Edit Mode / .NET

- `onboardingCompleted` é falso por padrão, preservado quando verdadeiro e migrado de forma idempotente.
- schema 4 migra para schema 5 sem apagar personagem, pet, cômodos ou itens desbloqueados.

### Play Mode

- save novo inicia `Welcome`; tocar Jogar grava e muda para Casa.
- save com onboarding concluído inicia Casa diretamente.
- Welcome bloqueia movimento e Casa o habilita.
- nome do cômodo muda para os dois idiomas após alternância.
- trocar idioma na Welcome e na Casa reconstrói a tela sem perder a etapa.

## Fora deste escopo

- exportação PNG final com `MediaStore` e QR configurável;
- integração de AdMob recompensado;
- animações de caminhada/idle dos FBX Cartoon City;
- vestir modular com peças independentes;
- publicação ou assinatura Android.
