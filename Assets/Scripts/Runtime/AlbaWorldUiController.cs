using System;
using System.Collections.Generic;
using System.Linq;
using AlbaWorld.Catalog;
using AlbaWorld.Game;
using UnityEngine;
using UnityEngine.UI;

namespace AlbaWorld.Runtime;

public enum AlbaWorldUiMode
{
    Welcome,
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
        public Action? UndoRemoveFurniture;
        public Action? SwitchCharacter;
        public Action? Photo;
        public Action? Room;
        public Action? Language;
        public Action<string>? SelectPet;
        public Action? EnterDress;
        public Action<string>? SelectCharacterPreset;
        public Action? StartGame;
    }

    private LanguageService _language = null!;
    private RoomFurnitureController _furniture = null!;
    private CharacterWardrobeController? _wardrobe;
    private CharacterPresetController? _presetController;
    private Callbacks _callbacks = new();
    private GameObject _safeRoot = null!;
    private GameObject _welcomeRoot = null!;
    private GameObject _houseRoot = null!;
    private GameObject _dressRoot = null!;
    private Text _petName = null!;
    private Text _roomName = null!;
    private Text _notice = null!;
    private readonly List<Button> _selectionButtons = new();
    private string _currentPetName = string.Empty;
    private string _currentRoomName = string.Empty;
    private string _pendingNotice = string.Empty;
    private bool _pendingNoticeSuccess;

    public AlbaWorldUiMode Mode { get; private set; } = AlbaWorldUiMode.Casa;
    public event Action<AlbaWorldUiMode>? ModeChanged;

    public void AttachWardrobe(CharacterWardrobeController wardrobe)
    {
        if (_wardrobe != null)
            _wardrobe.NoticeRequested -= OnWardrobeNotice;
        _wardrobe = wardrobe;
        if (_wardrobe != null)
            _wardrobe.NoticeRequested += OnWardrobeNotice;
    }

    public void AttachCharacterPresets(CharacterPresetController controller)
    {
        _presetController = controller;
    }

    public void Initialize(
        LanguageService language,
        RoomFurnitureController furniture,
        Action<string> addFurniture,
        Action<float> scaleFurniture,
        Action mirrorFurniture,
        Action bringForward,
        Action sendBackward,
        Action removeFurniture,
        Action undoRemoveFurniture,
        Action switchCharacter,
        Action photo,
        Action room,
        Action languageToggle,
        Action<string> selectPet,
        Action enterDress,
        Action<string>? selectCharacterPreset = null,
        Action? startGame = null)
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
            UndoRemoveFurniture = undoRemoveFurniture,
            SwitchCharacter = switchCharacter,
            Photo = photo,
            Room = room,
            Language = languageToggle,
            SelectPet = selectPet,
            EnterDress = enterDress,
            SelectCharacterPreset = selectCharacterPreset,
            StartGame = startGame
        };
        _furniture.SelectionChanged += OnFurnitureSelectionChanged;

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
        _currentRoomName = _language.Get("room.sunny");
        BuildHouseMode();
    }

    public void EnterWelcomeMode()
    {
        Mode = AlbaWorldUiMode.Welcome;
        DestroyModeRoots();
        BuildWelcomeMode();
        ModeChanged?.Invoke(Mode);
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
        if (mode == AlbaWorldUiMode.Welcome)
            BuildWelcomeMode();
        else if (mode == AlbaWorldUiMode.Casa)
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

    public void SetRoomName(string roomName)
    {
        _currentRoomName = string.IsNullOrWhiteSpace(roomName) ? _language?.Get("hud.room") ?? "Room" : roomName;
        if (_roomName != null)
            _roomName.text = _currentRoomName;
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
        var title = Label(top.transform, _language.Get("app.title"), 34, Color.white, TextAnchor.MiddleLeft);
        Anchor(title.rectTransform, new Vector2(0.025f, 0.18f), new Vector2(0.28f, 0.92f));
        var roomButton = AddButton(top.transform, _currentRoomName,
            new Color(0.16f, 0.19f, 0.31f), () => _callbacks.Room?.Invoke(),
            new Vector2(0.29f, 0.16f), new Vector2(0.47f, 0.86f), 14);
        _roomName = roomButton.GetComponentInChildren<Text>();
        _petName = Label(top.transform, _currentPetName, 16, Mint, TextAnchor.MiddleLeft);
        Anchor(_petName.rectTransform, new Vector2(0.49f, 0.20f), new Vector2(0.64f, 0.85f));
        var offline = Label(top.transform, _language.Get("hud.offline"), 13, Mint, TextAnchor.MiddleLeft);
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
        _notice = Label(dock.transform, string.Empty, 15, Mint, TextAnchor.MiddleRight);
        Anchor(_notice.rectTransform, new Vector2(0.35f, 0.80f), new Vector2(0.98f, 0.98f));
        ShowFurniturePage(content.transform);
    }

    private void BuildWelcomeMode()
    {
        _welcomeRoot = new GameObject("Welcome Mode", typeof(RectTransform));
        _welcomeRoot.transform.SetParent(_safeRoot.transform, false);
        Anchor((RectTransform)_welcomeRoot.transform, Vector2.zero, Vector2.one);

        var backdrop = Panel(_welcomeRoot.transform, "Welcome Backdrop", new Color(0.035f, 0.045f, 0.10f, 0.96f), Vector2.zero, Vector2.one);
        var card = Panel(_welcomeRoot.transform, "Welcome Card", new Color(0.08f, 0.10f, 0.20f, 0.98f), new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f));
        var title = Label(card.transform, _language.Get("welcome.title"), 42, Color.white, TextAnchor.MiddleCenter);
        Anchor(title.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f));
        var subtitle = Label(card.transform, _language.Get("welcome.subtitle"), 20, new Color(0.72f, 0.86f, 0.95f), TextAnchor.MiddleCenter);
        Anchor(subtitle.rectTransform, new Vector2(0.10f, 0.70f), new Vector2(0.90f, 0.80f));

        var tips = new[] { "welcome.select", "welcome.drag", "welcome.modes" };
        for (var index = 0; index < tips.Length; index++)
        {
            var min = new Vector2(0.08f + index * 0.30f, 0.40f);
            var max = new Vector2(min.x + 0.26f, 0.65f);
            var tip = Panel(card.transform, $"Tip {index + 1}", new Color(0.12f, 0.15f, 0.27f, 1f), min, max);
            var number = Label(tip.transform, (index + 1).ToString(), 30, Pink, TextAnchor.UpperCenter);
            Anchor(number.rectTransform, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.92f));
            var message = Label(tip.transform, _language.Get(tips[index]), 16, Color.white, TextAnchor.MiddleCenter);
            Anchor(message.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.60f));
        }

        AddButton(card.transform, _language.Get("welcome.play"), Pink, () => _callbacks.StartGame?.Invoke(), new Vector2(0.27f, 0.14f), new Vector2(0.73f, 0.30f), 22);
        AddButton(card.transform, _language.Get("welcome.language"), new Color(0.18f, 0.24f, 0.42f), () => _callbacks.Language?.Invoke(), new Vector2(0.38f, 0.04f), new Vector2(0.62f, 0.11f), 14);
        var offline = Label(backdrop.transform, _language.Get("hud.offline"), 14, Mint, TextAnchor.MiddleRight);
        Anchor(offline.rectTransform, new Vector2(0.78f, 0.02f), new Vector2(0.97f, 0.06f));
    }

    private void BuildDressMode()
    {
        _dressRoot = new GameObject("Vestir Mode", typeof(RectTransform));
        _dressRoot.transform.SetParent(_safeRoot.transform, false);
        Anchor((RectTransform)_dressRoot.transform, Vector2.zero, Vector2.one);

        var preview = Panel(_dressRoot.transform, "Character Preview", PanelColor, new Vector2(0f, 0.08f), new Vector2(0.47f, 0.85f));
        var previewTitle = Label(preview.transform, _language.Get("wardrobe.preview"), 22, Color.white, TextAnchor.MiddleCenter);
        Anchor(previewTitle.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.98f));
        var hint = Label(preview.transform, _language.Get("wardrobe.choose"), 16, new Color(0.72f, 0.73f, 0.84f), TextAnchor.MiddleCenter);
        Anchor(hint.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.16f));

        var panel = Panel(_dressRoot.transform, "Wardrobe Panel", PanelColor, new Vector2(0.50f, 0.08f), new Vector2(1f, 0.85f));
        var heading = Label(panel.transform, _language.Get("hud.dress"), 26, Color.white, TextAnchor.MiddleLeft);
        Anchor(heading.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
        ShowPresetButtons(panel.transform);
        ShowPaletteButtons(panel.transform);
        var categories = new[]
        {
            ItemCategory.Skin, ItemCategory.Hair, ItemCategory.Outfit, ItemCategory.Shoes, ItemCategory.HumanAccessory
        };
        var categoryKeys = new[]
        {
            "wardrobe.skin", "wardrobe.hair", "wardrobe.outfit", "wardrobe.shoes", "wardrobe.accessories"
        };
        var content = new GameObject("Wardrobe Content", typeof(RectTransform));
        content.transform.SetParent(panel.transform, false);
        Anchor((RectTransform)content.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.36f));
        for (var index = 0; index < categories.Length; index++)
        {
            var category = categories[index];
            var min = new Vector2(0.05f + index * 0.19f, 0.40f);
            var max = new Vector2(min.x + 0.17f, 0.54f);
            AddButton(panel.transform, _language.Get(categoryKeys[index]), new Color(0.22f + index * 0.04f, 0.28f, 0.48f), () =>
            {
                _wardrobe?.SelectCategory(category);
                ShowWardrobeItems(content.transform, category);
            }, min, max, 14);
        }
        ShowWardrobeItems(content.transform, ItemCategory.Skin);
        AddButton(_dressRoot.transform, _language.Get("menu.back"), new Color(0.35f, 0.30f, 0.55f), EnterHouseMode, new Vector2(0.02f, 0f), new Vector2(0.18f, 0.06f), 16);
        AddButton(_dressRoot.transform, _language.Get("hud.save"), Mint, EnterHouseMode, new Vector2(0.82f, 0f), new Vector2(0.98f, 0.06f), 16);
    }

    private void ShowPresetButtons(Transform parent)
    {
        var label = Label(parent, _language.Get("hud.switchCharacter"), 13, new Color(0.72f, 0.73f, 0.84f), TextAnchor.MiddleLeft);
        Anchor(label.rectTransform, new Vector2(0.05f, 0.75f), new Vector2(0.20f, 0.86f));
        var presets = _presetController?.Presets().ToArray() ?? Array.Empty<CharacterPresetDefinition>();
        for (var index = 0; index < presets.Length; index++)
        {
            var preset = presets[index];
            var min = new Vector2(0.21f + index * 0.19f, 0.72f);
            var max = new Vector2(min.x + 0.17f, 0.87f);
            var value = _language.Get(preset.displayKey);
            if (value == preset.displayKey)
                value = preset.presetId;
            AddButton(parent, value, new Color(0.35f + index * 0.05f, 0.30f, 0.55f), () => _callbacks.SelectCharacterPreset?.Invoke(preset.presetId), min, max, 12);
        }
    }

    private void ShowPaletteButtons(Transform parent)
    {
        var palettes = _presetController?.Palettes().ToArray() ?? Array.Empty<CharacterPresetPalette>();
        if (palettes.Length <= 1)
            return;

        var label = Label(parent, _language.Get("wardrobe.palette"), 13, new Color(0.72f, 0.73f, 0.84f), TextAnchor.MiddleLeft);
        Anchor(label.rectTransform, new Vector2(0.05f, 0.57f), new Vector2(0.20f, 0.68f));
        for (var index = 0; index < palettes.Length; index++)
        {
            var palette = palettes[index];
            var min = new Vector2(0.21f + index * 0.19f, 0.55f);
            var max = new Vector2(min.x + 0.17f, 0.70f);
            var value = _language.Get(palette.displayKey);
            if (value == palette.displayKey)
                value = palette.paletteId;
            AddButton(parent, value, palette.outfitTint, () =>
            {
                var applied = _presetController != null && _presetController.TrySelectPalette(palette.paletteId);
                ShowNotice(applied ? _language.Get("hud.saved") : _language.Get("photo.error"), applied);
            }, min, max, 12);
        }
    }

    private void ShowWardrobeItems(Transform content, ItemCategory category)
    {
        ClearContent(content);
        if (_wardrobe == null)
        {
            AddButton(content, _language.Get("wardrobe.choose"), PanelSoft, () => { }, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.65f), 15);
            return;
        }

        var items = _wardrobe.ItemsForCategory(category).ToArray();
        for (var index = 0; index < items.Length; index++)
        {
            var visual = items[index];
            var column = index % 4;
            var row = index / 4;
            var min = new Vector2(0.02f + column * 0.245f, 0.62f - row * 0.34f);
            var max = new Vector2(min.x + 0.22f, min.y + 0.27f);
            var itemId = visual.ItemId;
            var label = _language.Get(visual.definition.displayKey);
            if (label == visual.definition.displayKey)
                label = itemId;
            var button = AddButton(content, label, visual.definition.tint, () =>
            {
                var applied = _wardrobe.TryApply(itemId);
                ShowNotice(applied ? _language.Get("hud.saved") : _language.Get("photo.error"), applied);
            }, min, max, 13);
            button.interactable = _wardrobe.CanUse(itemId);
        }
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
        AddButton(content, _language.Get("hud.undo"), PanelSoft, () => _callbacks.UndoRemoveFurniture?.Invoke(), new Vector2(0.44f, 0.08f), new Vector2(0.54f, 0.30f), 12);
        SetFurnitureSelection(_furniture.HasSelection);
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

    private static void ClearContent(Transform content)
    {
        for (var index = content.childCount - 1; index >= 0; index--)
        {
            var child = content.GetChild(index).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void DestroyModeRoots()
    {
        _selectionButtons.Clear();
        if (_welcomeRoot != null)
            DestroyObject(_welcomeRoot);
        if (_houseRoot != null)
            DestroyObject(_houseRoot);
        if (_dressRoot != null)
            DestroyObject(_dressRoot);
        _welcomeRoot = null!;
        _houseRoot = null!;
        _dressRoot = null!;
        _petName = null!;
        _roomName = null!;
        _notice = null!;
    }

    private static void DestroyObject(GameObject target)
    {
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private void OnFurnitureSelectionChanged(string _) => SetFurnitureSelection(_furniture.HasSelection);

    private void OnWardrobeNotice(string _) => ShowNotice(_language.Get("wardrobe.visualUnavailable"), false);

    private void OnDestroy()
    {
        if (_furniture != null)
            _furniture.SelectionChanged -= OnFurnitureSelectionChanged;
        if (_wardrobe != null)
            _wardrobe.NoticeRequested -= OnWardrobeNotice;
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

    private static Text Label(Transform parent, string value, float size, Color color, TextAnchor alignment)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = Mathf.RoundToInt(size);
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(size * 0.65f));
        text.resizeTextMaxSize = Mathf.RoundToInt(size);
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
        var text = Label(go.transform, value, fontSize, Color.white, TextAnchor.MiddleCenter);
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
