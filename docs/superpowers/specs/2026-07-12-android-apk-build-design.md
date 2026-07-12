# Build Android APK para teste local

## Objetivo

Permitir instalar o MVP diretamente em um aparelho Android sem alterar o fluxo de publicação do AAB.

## Decisão

Adicionar o menu `Alba World > Build Android APK (local test)`. O comando reutiliza a cena principal, desativa `buildAppBundle`, gera `Builds/AlbaWorld.apk` e usa `BuildOptions.Development`, mantendo a assinatura de desenvolvimento para testes locais.

## Verificação

O comando será executado em batchmode pelo Unity 6.3 LTS. A aceitação é a existência de um APK não vazio; o AAB continua sendo gerado pelo comando separado `Build Android AAB`.
