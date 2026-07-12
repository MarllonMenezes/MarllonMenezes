using UnityEngine;
using UnityEngine.UI;

namespace AlbaWorld.UI;

public static class UiFactory
{
    public static readonly Color Ink = new(0.18f, 0.15f, 0.25f);
    public static readonly Color Lavender = new(0.94f, 0.91f, 1f);
    public static readonly Color Mint = new(0.84f, 0.97f, 0.92f);
    public static readonly Color Pink = new(1f, 0.87f, 0.92f);

    public static Canvas CreateCanvas()
    {
        var go = new GameObject("Alba World Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max, Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin ?? Vector2.zero;
        rect.offsetMax = offsetMax ?? Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    public static Text Label(Transform parent, string text, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.resizeTextForBestFit = size <= 22;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = size;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(12, 6);
        rect.offsetMax = new Vector2(-12, -6);
        return label;
    }

    public static Button Button(Transform parent, string text, Color color, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        var label = Label(go.transform, text, 22, Ink);
        label.fontStyle = FontStyle.Bold;
        button.colors = new ColorBlock { normalColor = color, highlightedColor = Color.Lerp(color, Color.white, 0.18f), pressedColor = Color.Lerp(color, Color.black, 0.08f), selectedColor = color, disabledColor = new Color(color.r, color.g, color.b, 0.55f), colorMultiplier = 1, fadeDuration = 0.08f };
        return button;
    }

    public static Image Image(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        return image;
    }

    public static void Clear(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
            Object.Destroy(parent.GetChild(i).gameObject);
    }
}
