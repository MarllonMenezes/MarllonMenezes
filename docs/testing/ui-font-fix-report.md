# Alba World - correção de texto da HUD

## Causa

O fallback anterior criava um `TMP_FontAsset` a partir de `LegacyRuntime.ttf`. Essa fonte não possui dados incorporados para o TextMeshPro nesta instalação, então o Unity desenhava os paineis e botoes, mas nao desenhava as letras.

## Correcao

`AlbaWorldUiController` agora usa `UnityEngine.UI.Text` e carrega `LegacyRuntime.ttf`, com fallback para `Arial.ttf`, por `Resources.GetBuiltinResource`. A HUD nao depende dos recursos opcionais do TextMeshPro e continua sem pacote, conta ou rede.

## Evidencia

- RED: `work/ui-font-red.xml` falhou porque os labels nao tinham fonte.
- GREEN: `work/ui-font-green.xml` passou 1/1.
- Unity Edit Mode: `work/house-dress-editmode-fontfix.xml` passou 73/73.
- Unity Play Mode: `work/house-dress-playmode-fontfix.xml` passou 28/28.
- .NET CoreTests: 14/14 aprovados.

## Teste manual

Abra `Assets/Scenes/Main.unity`, pressione Play e confirme que o titulo, os botoes Casa, Vestir, Foto, idioma, moveis e controles aparecem com texto. Teste tambem o modo Vestir e as proporcoes 16:9, 18:9 e 20:9.
