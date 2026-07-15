# Alba World

MVP 2D infantil offline da Alba World Games.

## Abrir

1. Instale Unity 6 LTS pelo Unity Hub com Android Build Support, Android SDK & NDK Tools e OpenJDK.
2. Abra esta pasta no Unity Hub.
3. Aguarde o Package Manager baixar os pacotes.
4. Se a cena não for criada automaticamente, use `Alba World > Generate Demo Scene`.
5. Pressione Play.

## Simular celular no PC

No Unity 6.3, use a janela **Game**: selecione `16:9` ou `20:9`, ajuste a escala para caber na tela e pressione Play. O mouse simula toque e arraste. O pacote Device Simulator oficial disponível para versões anteriores não é compatível com este editor e não faz parte do projeto.

## Controles do protótipo

- Menu: personagem, pet, casa, modo foto e idioma.
- Casa: toque em móveis para colocar; arraste os móveis dentro do cômodo.
- Modo foto: salva uma imagem localmente. No editor ela fica em `Application.persistentDataPath/AlbaWorldExports`; no Android usa `Pictures/Alba World`.

## Anúncios antes da publicação

O projeto mantém o serviço de recompensa isolado em `RewardedAdsService`. Antes de publicar:

- adicione a versão do Google Mobile Ads Unity Plugin que estiver na lista atual de SDKs certificados do Google Play Families;
- substitua o adaptador de simulação pelo callback real de anúncio recompensado;
- configure tratamento infantil antes de inicializar o SDK, anúncios não personalizados, classificação máxima G e remova `AD_ID`;
- use IDs de teste durante todo o desenvolvimento;
- mantenha o limite de dois itens permanentes por dia.

## Testes

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' test Tools\CoreTests\AlbaWorld.CoreTests.csproj
```

Os testes Unity ficam em `Assets/Tests/Editor`. O teste automatizado usado nesta entrega foi:

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath . -runTests -testPlatform editmode -testResults work\unity-tests.xml
```

### Pets Kenney (offline)

The in-game settings panel also shows the localized `Kenney — www.kenney.nl` credit in `pt-BR` and `en`. `PetLoadoutData.colorId` is consumed in the 3D visual through a `MaterialPropertyBlock` (`petcolor.sunny` identity and `petcolor.cocoa` warm brown), preserving shared materials without duplication. `accessoryIds` remain persisted and broadcast to hooks, but 3D rendering is explicitly deferred until compatible assets are approved; no placeholder or new art is created. When restoring an unknown pet ID, the flow saves the `pet.cat` fallback once; a subsequent reload does not repeat the invalid ID or save again.

O subsistema de pets inclui os 24 prefabs locais do Kenney Cube Pets 2.0. `pet.cat` e `pet.dog` continuam sendo os IDs legados; todos os animais usam o catálogo 3D, localização bilíngue, persistência local e os fluxos de casa/foto sem exigir internet. A fonte e a licença Creative Commons Zero ficam arquivadas em `Assets/Art3D/Pets/Source/KenneyCubePets` e `docs/legal/assets/kenney-cube-pets-2.0` (crédito recomendado: **Kenney — www.kenney.nl**).

Para repetir a auditoria focada, execute no diretório do projeto:

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter AlbaWorld.Tests.KenneySourceManifestTests -testResults work/kenney-source.xml
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter AlbaWorld.Tests.KenneyPetPrefabTests -testResults work/kenney-prefab.xml
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter AlbaWorld.Tests.KenneyPetCatalogTests -testResults work/kenney-catalog.xml
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform playmode -testResults work/kenney-playmode.xml
& 'C:\Program Files\dotnet\dotnet.exe' test Tools\CoreTests\AlbaWorld.CoreTests.csproj --no-restore
```

Os resultados, a tabela de triângulos e as limitações da captura visual estão em [`docs/testing/kenney-pets-test-report.md`](docs/testing/kenney-pets-test-report.md). A próxima etapa do projeto é rooms/furniture, limitada a conectar o pet existente ao posicionamento e ao modo foto.

## AAB

No Unity, use `Alba World > Build Android AAB`. O arquivo é criado em `Builds/AlbaWorld.aab`. A validação local gera um AAB assinado com o certificado Android Debug; configure sua própria keystore/Play App Signing antes do envio à Play Store.

## APK para teste local

No Unity, use `Alba World > Build Android APK (local test)`. O arquivo instalável será criado em `Builds/AlbaWorld.apk` com assinatura de desenvolvimento.

Com um aparelho conectado e a depuração USB autorizada:

```powershell
& 'D:\Unity\Hub\Editor\6000.3.19f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe' install -r Builds\AlbaWorld.apk
```

Android acceptance run on 2026-07-15: the development APK build command started, but the Android pipeline repeatedly ran `AndroidPlayerBuildProgram` with `ExitCode: 4` in `com.unity.burst/.Runtime\\bcl.exe`; no `Builds/AlbaWorld.apk` was produced. `adb devices` started the daemon and returned an empty list, so no device/emulator was available for installation or offline smoke testing. Full log: `work/task7-android-apk.log`.

## Instalação usada nesta máquina

- Unity Hub: `D:\UnityHub\Unity Hub.exe`
- Unity 6.3 LTS + Android Build Support: `D:\Unity\Hub\Editor\6000.3.19f1`
- SDK, NDK, OpenJDK e Gradle ficam dentro do módulo Android desse editor.
