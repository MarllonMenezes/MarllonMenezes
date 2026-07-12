# Alba World

MVP 2D infantil offline da Alba World Games.

## Abrir

1. Instale Unity 6 LTS pelo Unity Hub com Android Build Support, Android SDK & NDK Tools e OpenJDK.
2. Abra esta pasta no Unity Hub.
3. Aguarde o Package Manager baixar os pacotes.
4. Se a cena não for criada automaticamente, use `Alba World > Generate Demo Scene`.
5. Pressione Play.

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

## AAB

No Unity, use `Alba World > Build Android AAB`. O arquivo é criado em `Builds/AlbaWorld.aab`. A validação local gera um AAB assinado com o certificado Android Debug; configure sua própria keystore/Play App Signing antes do envio à Play Store.

## APK para teste local

No Unity, use `Alba World > Build Android APK (local test)`. O arquivo instalável será criado em `Builds/AlbaWorld.apk` com assinatura de desenvolvimento.

## Instalação usada nesta máquina

- Unity Hub: `D:\UnityHub\Unity Hub.exe`
- Unity 6.3 LTS + Android Build Support: `D:\Unity\Hub\Editor\6000.3.19f1`
- SDK, NDK, OpenJDK e Gradle ficam dentro do módulo Android desse editor.
