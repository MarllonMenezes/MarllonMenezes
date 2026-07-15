# Alba World — relatório do fluxo de primeira execução

Data: 15/07/2026  
Branch: `feature/alba-world-mvp`  
Unity: `6.3.19f1`

## Entrega validada

- Welcome/tutorial na primeira execução;
- `Jogar` grava `onboardingCompleted` e entra em Casa;
- Casa, Vestir e Welcome são modos independentes;
- Casa exibe o cômodo atual como botão localizado;
- `room.sunny` e `room.cozy` continuam com layouts separados;
- idioma pode ser alternado sem apagar o estado;
- Windows player recompilado em `Builds/AlbaWorldWindows/AlbaWorld.exe`.

## Comandos e resultados

### Testes .NET

```powershell
dotnet test Tools/CoreTests/AlbaWorld.CoreTests.csproj --no-restore
```

Resultado: **16/16 aprovados, 0 falhas**.

Os dois testes novos cobrem save novo com Welcome pendente e migração de schema 4 para 5 preservando progresso e sendo idempotente.

### Testes Unity Edit Mode

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -projectPath . -runTests -testPlatform editmode -testResults work\first-run-editmode-gpu.xml -logFile work\first-run-editmode-gpu.log
```

Resultado: **86/86 aprovados, 0 falhas**. O modo com GPU foi usado porque um teste histórico de revisão de personagens precisa de uma render texture real.

Contrato focado:

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter 'FirstRunUiTests|HouseDressLocalizationTests' -testResults work\first-run-contracts-final.xml -logFile work\first-run-contracts-final.log
```

Resultado: **3/3 aprovados**.

### Testes Unity Play Mode

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform playmode -testResults work\first-run-playmode-final.xml -logFile work\first-run-playmode-final.log
```

Resultado: **28/28 aprovados, 0 falhas**.

### Player Windows

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -executeMethod AlbaWorld.Editor.BuildTools.BuildWindowsPlayer -logFile work\first-run-windows-build.log
```

Resultado: build concluído; o executável foi atualizado em `Builds/AlbaWorldWindows/AlbaWorld.exe`.

## Limitação conhecida

O teste histórico `CharacterImportTests.RenderReviewRestoresBatchingAndWritesTheApprovedCapture` falha quando a suíte Edit Mode é executada com `-nographics`, pois a captura fica 100% na cor de limpeza. Com backend gráfico a suíte completa passa (86/86). Isso é uma limitação do teste de renderização, não do fluxo Welcome/Casa.

## Próximos blocos do MVP

Ainda faltam exportação Android via `MediaStore` com logo/QR, AdMob recompensado configurado para famílias, animações de personagens, roupas modulares e AAB assinado para a Play Store.
