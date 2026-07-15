using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlbaWorld.Runtime;

public enum AlbaWorldUiMode
{
    Casa,
    Vestir
}

/// <summary>
/// Owns the mobile-safe HUD. World systems send actions here; the UI never edits save data directly.
/// </summary>
[DisallowMultipleComponent]
public sealed class AlbaWorldUiController : MonoBehaviour
{
    private static readonly Color PanelColor = new(0.06f, 0.07f, 0.13f, 0.95f);
    private static readonly Color PanelSoft = new(0.12f, 0.13f, 0.22f, 0.98f);
    private static readonly Color Pink = new(1f, 0.34f, 0.60f, 1f);
    private static readonly Color Mint = new(0.28f, 0.86f, 0.73f, 1f);

    private sealed class Callbacks
    {
        public Action<string>? AddFurniture;
        public Action<float>? ScaleFurniture;
        public Action? MirrorFurniture;
        public Action? BringForward;
        public Action? SendBackward;
        public Action? RemoveFurniture;
        public Action? SwitchCharacter;
        public Action? Photo;
        public Action? Room;
        public Action? Language;
        public Action<string>? SelectPet;
        public Action? EnterDress;
    }

    private LanguageService _language = null!;
    private RoomFurnitureController _furniture = null!;
    private Callbacks _callbacks = new();
    private GameObject _safeRoot = null!;
    private GameObject _houseRoot = null!;
    private GameObject _dressRoot = null!;
    private TMP_Text _petName = null!;
    private TMP_Text _notice = null!;
    private readonly List<Button> _selectionButtons = new();
    private string _currentPetName = string.Empty;
    private string _pendingNotice = string.Empty;
    private bool _pendingNoticeSuccess;

    public AlbaWorldUiMode Mode { get; private set; } = AlbaWorldUiMode.Casa;
    public event Action<AlbaWorldUiMode>? ModeChanged;

    public void Initialize(
        LanguageService language,
        RoomFurnitureController furniture,
        Action<string> addFurniture,
        Action<float> scaleFurniture,
        Action mirrorFurniture,
        Action bringForward,
        Action sendBackward,
        Action removeFurniture,
        Action switchCharacter,
        Action photo,
        Action room,
        Action languageToggle,
        Action<string> selectPet,
        Action enterDress)
    {
        _language = language ?? throw new ArgumentNullException(nameof(language));
        _furniture = furniture ?? throw new ArgumentNullException(nameof(furniture));
        _callbacks = new Callbacks
        {
            AddFurniture = addFurniture,
            ScaleFurniture = scaleFurniture,
            MirrorFurniture = mirrorFurniture,
            BringForward = bringForward,
            SendBackward = sendBackward,
            RemoveFurniture = removeFurniture,
            SwitchCharacter = switchCharacter,
            Photo = photo,
            Room = room,
            Language = languageToggle,
            SelectPet = selectPet,
            EnterDress = enterDress
        };

        var canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        _safeRoot = new GameObject("Safe Area", typeof(RectTransform));
        _safeRoot.transform.SetParent(transform, false);
        var safeRect = (RectTransform)_safeRoot.transform;
        Anchor(safeRect, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));
        BuildHouseMode();
    }

    public void EnterHouseMode()
    {
        Mode = AlbaWorldUiMode.Casa;
        DestroyModeRoots();
        BuildHouseMode();
        ModeChanged?.Invoke(Mode);
    }

    public void EnterDressMode()
    {
        Mode = AlbaWorldUiMode.Vestir;
        DestroyModeRoots();
        BuildDressMode();
        ModeChanged?.Invoke(Mode);
        _callbacks.EnterDress?.Invoke();
    }

    public void RefreshLanguage()
    {
        if (_language == null)
            return;
        var mode = Mode;
        DestroyModeRoots();
        if (mode == AlbaWorldUiMode.Casa)
            BuildHouseMode();
        else
            BuildDressMode();
        if (!string.IsNullOrWhiteSpace(_pendingNotice))
            ShowNotice(_pendingNotice, _pendingNoticeSuccess);
    }

    public void SetPetName(string petName)
    {
        _currentPetName = petName ?? string.Empty;
        if (_petName != null)
            _petName.text = _currentPetName;
    }

    public void SetFurnitureSelection(bool selected)
    {
        foreach (var button in _selectionButtons)
            button.interactable = selected;
    }

    public void ShowNotice(string message, bool success)
    {
        _pendingNotice = message ?? string.Empty;
        _pendingNoticeSuccess = success;
        if (_notice == null)
            return;

        _notice.text = _pendingNotice;
        _notice.color = success ? Mint : new Color(1f, 0.45f, 0.55f);
        CancelInvoke(nameof(ClearNotice));
        Invoke(nameof(ClearNotice), 2.2f);
    }

    private void BuildHouseMode()
    {
        _houseRoot = new GameObject("Casa Mode", typeof(RectTransform));
        _houseRoot.transform.SetParent(_safeRoot.transform, false);
        Anchor((RectTransform)_houseRoot.transform, Vector2.zero, Vector2.one);

        var top = Panel(_houseRoot.transform, "Top Bar", PanelColor, new Vector2(0f, 0.875f), new Vector2(1f, 1f));
        var title = Label(top.transform, _language.Get("app.title"), 34, Color.white, TextAlignmentOptions.MidlineLeft);
        Anchor(title.rectTransform, new Vector2(0.025f, 0.18f), new Vector2(0.28f, 0.92f));
        var room = Label(top.transform, _language.Get("hud.house"), 16, new Color(0.72f, 0.73f, 0.84f), TextAlignmentOptions.MidlineLeft);
        Anchor(room.rectTransform, new Vector2(0.29f, 0.20f), new Vector2(0.47f, 0.85f));
        _petName = Label(top.transform, _currentPetName, 16, Mint, TextAlignmentOptions.MidlineLeft);
        Anchor(_petName.rectTransform, new Vector2(0.49f, 0.20f), new Vector2(0.64f, 0.85f));
        var offline = Label(top.transform, _language.Get("hud.offline"), 13, Mint, TextAlignmentOptions.MidlineLeft);
        Anchor(offline.rectTransform, new Vector2(0.615f, 0.20f), new Vector2(0.655f, 0.85f));
        AddButton(top.transform, _language.Get("hud.dress"), Pink, EnterDressMode, new Vector2(0.66f, 0.16f), new Vector2(0.78f, 0.86f), 16);
        AddButton(top.transform, _language.Get("hud.photo"), new Color(0.35f, 0.30f, 0.55f), () => _callbacks.Photo?.Invoke(), new Vector2(0.79f, 0.16f), new Vector2(0.89f, 0.86f), 15);
        AddButton(top.transform, _language.Get("hud.language"), new Color(0.22f, 0.28f, 0.48f), () => _callbacks.Language?.Invoke(), new Vector2(0.90f, 0.16f), new Vector2(0.99f, 0.86f), 15);

        var dock = Panel(_houseRoot.transform, "House Dock", PanelColor, new Vector2(0f, 0f), new Vector2(1f, 0.265f));
        var content = new GameObject("Dock Content", typeof(RectTransform));
        content.transform.SetParent(dock.transform, false);
        Anchor((RectTransform)content.transform, new Vector2(0.01f, 0.03f), new Vector2(0.99f, 0.76f));
        AddButton(dock.transform, _language.Get("hud.furniture"), PanelSoft, () => ShowFurniturePage(content.transform), new Vector2(0.02f, 0.80f), new Vector2(0.18f, 0.98f), 15);
        AddButton(dock.transform, _language.Get("hud.actions"), PanelSoft, () => ShowActionsPage(content.transform), new Vector2(0.19f, 0.80f), new Vector2(0.33f, 0.98f), 15);
        _notice = Label(dock.transform, string.Empty, 15, Mint, TextAlignmentOptions.MidlineRight);
        Anchor(_notice.rectTransform, new Vector2(0.35f, 0.80f), new Vector2(0.98f, 0.98f));
        ShowFurniturePage(content.transform);
    }

    private void BuildDressMode()
    {
        _dressRoot = new GameObject("Vestir Mode", typeof(RectTransform));
        _dressRoot.transform.SetParent(_safeRoot.transform, false);
        Anchor((RectTransform)_dressRoot.transform, Vector2.zero, Vector2.one);

        var preview = Panel(_dressRoot.transform, "Character Preview", PanelColor, new Vector2(0f, 0.08f), new Vector2(0.47f, 0.85f));
        var previewTitle = Label(preview.transform, _language.Get("wardrobe.preview"), 22, Color.white, TextAlignmentOptions.Center);
        Anchor(previewTitle.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.98f));
        var hint = Label(preview.transform, _language.Get("wardrobe.choose"), 16, new Color(0.72f, 0.73f, 0.84f), TextAlignmentOptions.Center);
        Anchor(hint.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.16f));

        var panel = Panel(_dressRoot.transform, "Wardrobe Panel", PanelColor, new Vector2(0.50f, 0.08f), new Vector2(1f, 0.85f));
        var heading = Label(panel.transform, _language.Get("hud.dress"), 26, Color.white, TextAlignmentOptions.MidlineLeft);
        Anchor(heading.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
        var categories = new[]
        {
            "wardrobe.skin", "wardrobe.hair", "wardrobe.outfit", "wardrobe.shoes", "wardrobe.accessories"
        };
        for (var index = 0; index < categories.Length; index++)
        {
            var column = index % 3;
            var row = index / 3;
            var min = new Vector2(0.05f + column * 0.31f, 0.68f - row * 0.22f);
            var max = new Vector2(min.x + 0.28f, min.y + 0.17f);
            AddButton(panel.transform, _language.Get(categories[index]), new Color(0.22f + column * 0.07f, 0.28f + row * 0.04f, 0.48f), () => ShowNotice(_language.Get("wardrobe.choose"), true), min, max, 15);
        }
        AddButton(_dressRoot.transform, _language.Get("menu.back"), new Color(0.35f, 0.30f, 0.55f), EnterHouseMode, new Vector2(0.02f, 0f), new Vector2(0.18f, 0.06f), 16);
        AddButton(_dressRoot.transform, _language.Get("hud.save"), Mint, EnterHouseMode, new Vector2(0.82f, 0f), new Vector2(0.98f, 0.06f), 16);
    }

    private void ShowFurniturePage(Transform content)
    {
        ClearDockContent(content);
        var ids = new[]
        {
            "furniture.bed", "furniture.sofa", "furniture.table", "furniture.chair", "furniture.shelf",
            "furniture.lamp", "furniture.plant", "furniture.rug", "furniture.book"
        };
        for (var index = 0; index < ids.Length; index++)
        {
            var column = index % 5;
            var row = index / 5;
            var min = new Vector2(0.02f + column * 0.145f, 0.34f - row * 0.30f);
            var max = new Vector2(min.x + 0.13f, min.y + 0.23f);
            var itemId = ids[index];
            AddButton(content, _language.Get("item." + itemId), new Color(0.22f + column * 0.025f, 0.28f + row * 0.04f, 0.48f + row * 0.03f), () => _callbacks.AddFurniture?.Invoke(itemId), min, max, 13);
        }

        AddControlButton(content, _language.Get("hud.smaller"), new Vector2(0.77f, 0.34f), new Vector2(0.87f, 0.56f), () => _callbacks.ScaleFurniture?.Invoke(-0.1f));
        AddControlButton(content, _language.Get("hud.larger"), new Vector2(0.88f, 0.34f), new Vector2(0.98f, 0.56f), () => _callbacks.ScaleFurniture?.Invoke(0.1f));
        AddControlButton(content, _language.Get("hud.mirror"), new Vector2(0.77f, 0.08f), new Vector2(0.87f, 0.30f), () => _callbacks.MirrorFurniture?.Invoke());
        AddControlButton(content, _language.Get("hud.front"), new Vector2(0.88f, 0.08f), new Vector2(0.98f, 0.30f), () => _callbacks.BringForward?.Invoke());
        AddControlButton(content, _language.Get("hud.back"), new Vector2(0.55f, 0.08f), new Vector2(0.65f, 0.30f), () => _callbacks.SendBackward?.Invoke());
        AddControlButton(content, _language.Get("hud.delete"), new Color(0.70f, 0.25f, 0.38f), new Vector2(0.66f, 0.08f), new Vector2(0.76f, 0.30f), () => _callbacks.RemoveFurniture?.Invoke());
        SetFurnitureSelection(!string.IsNullOrWhiteSpace(_furniture.SelectedInstanceId));
    }

    private void ShowActionsPage(Transform content)
    {
        ClearDockContent(content);
        var petIds = new[] { "pet.cat", "pet.dog", "pet.fox", "pet.panda" };
        for (var index = 0; index < petIds.Length; index++)
        {
            var column = index % 4;
            var min = new Vector2(0.03f + column * 0.18f, 0.34f);
            var max = new Vector2(min.x + 0.16f, 0.62f);
            var petId = petIds[index];
            AddButton(content, _language.Get("item." + petId), new Color(0.25f + column * 0.07f, 0.30f, 0.50f), () => _callbacks.SelectPet?.Invoke(petId), min, max, 14);
        }
        AddButton(content, _language.Get("hud.switchCharacter"), new Color(0.35f, 0.30f, 0.55f), () => _callbacks.SwitchCharacter?.Invoke(), new Vector2(0.03f, 0.08f), new Vector2(0.21f, 0.28f), 13);
        AddButton(content, _language.Get("hud.room"), new Color(0.25f, 0.47f, 0.50f), () => _callbacks.Room?.Invoke(), new Vector2(0.23f, 0.08f), new Vector2(0.41f, 0.28f), 13);
        AddButton(content, _language.Get("hud.photo"), Pink, () => _callbacks.Photo?.Invoke(), new Vector2(0.43f, 0.08f), new Vector2(0.61f, 0.28f), 13);
        AddButton(content, _language.Get("hud.language"), new Color(0.22f, 0.28f, 0.48f), () => _callbacks.Language?.Invoke(), new Vector2(0.63f, 0.08f), new Vector2(0.81f, 0.28f), 13);
    }

    private void AddControlButton(Transform parent, string value, Vector2 min, Vector2 max, Action click) =>
        AddControlButton(parent, value, PanelSoft, min, max, click);

    private void AddControlButton(Transform parent, string value, Color color, Vector2 min, Vector2 max, Action click)
    {
        var button = AddButton(parent, value, color, click, min, max, 12);
        _selectionButtons.Add(button);
    }

    private void ClearDockContent(Transform dock)
    {
        _selectionButtons.Clear();
        for (var index = dock.childCount - 1; index >= 0; index--)
        {
            var child = dock.GetChild(index).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void DestroyModeRoots()
    {
        _selectionButtons.Clear();
        if (_houseRoot != null)
            Destroy(_houseRoot);
        if (_dressRoot != null)
            Destroy(_dressRoot);
        _houseRoot = null!;
        _dressRoot = null!;
        _petName = null!;
        _notice = null!;
    }

    private void ClearNotice()
    {
        _pendingNotice = string.Empty;
        _pendingNoticeSuccess = true;
        if (_notice != null)
            _notice.text = string.Empty;
    }

    private static GameObject Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        Anchor(rect, min, max);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static TMP_Text Label(Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static Button AddButton(Transform parent, string value, Color color, Action click, Vector2 min, Vector2 max, int fontSize = 17)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        Anchor(rect, min, max);
        var layout = go.GetComponent<LayoutElement>();
        layout.minHeight = 44f;
        var image = go.GetComponent<Image>();
        image.color = color;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => click());
        var text = Label(go.transform, value, fontSize, Color.white, TextAlignmentOptions.Center);
        Anchor(text.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
        return button;
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
