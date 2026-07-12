using System;
using System.Collections.Generic;
using System.Linq;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Runtime;
using AlbaWorld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AlbaWorld;

public sealed class AlbaWorldApp : MonoBehaviour
{
    private Canvas _canvas = null!;
    private RectTransform _content = null!;
    private RuntimeCatalog _catalog = null!;
    private LocalSaveService _saveService = null!;
    private GameSaveData _save = null!;
    private LanguageService _language = null!;
    private RewardedAdsService _ads = null!;
    private PhotoExporter _photo = null!;
    private readonly List<SceneSnapshot> _rooms = new();
    private int _roomIndex;
    private SceneSnapshot _currentSnapshot = null!;
    private string _selectedSkin = "skin.cream";
    private string _selectedHair = "hair.sunny";
    private string _selectedOutfit = "outfit.pink";
    private string _selectedShoes = "shoes.sun";
    private string _selectedAccessory = "accessory.star";
    private string _selectedPet = "pet.cat";
    private string _selectedPetAccessory = "pet.bow";

    private void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Application.targetFrameRate = 60;
        _catalog = new RuntimeCatalog();
        _saveService = new LocalSaveService();
        _save = _saveService.Load();
        _language = new LanguageService(_save.languageCode);
        _ads = new RewardedAdsService(_save);
        _photo = new PhotoExporter();
        RestoreState();
        _canvas = UiFactory.CreateCanvas();
        EnsureEventSystem();
        var root = UiFactory.Panel(_canvas.transform, "Root", new Color(0.98f, 0.96f, 1f), Vector2.zero, Vector2.one);
        _content = UiFactory.Panel(root, "Content", Color.clear, Vector2.zero, Vector2.one, new Vector2(28, 24), new Vector2(-28, -24));
        ShowHome();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void RestoreState()
    {
        _selectedSkin = string.IsNullOrWhiteSpace(_save.selectedSkinId) ? _selectedSkin : _save.selectedSkinId;
        _selectedHair = string.IsNullOrWhiteSpace(_save.selectedHairId) ? _selectedHair : _save.selectedHairId;
        _selectedOutfit = string.IsNullOrWhiteSpace(_save.selectedOutfitId) ? _selectedOutfit : _save.selectedOutfitId;
        _selectedShoes = string.IsNullOrWhiteSpace(_save.selectedShoesId) ? _selectedShoes : _save.selectedShoesId;
        _selectedAccessory = string.IsNullOrWhiteSpace(_save.selectedAccessoryId) ? _selectedAccessory : _save.selectedAccessoryId;
        _selectedPet = string.IsNullOrWhiteSpace(_save.selectedPetId) ? _selectedPet : _save.selectedPetId;
        _selectedPetAccessory = string.IsNullOrWhiteSpace(_save.selectedPetAccessoryId) ? _selectedPetAccessory : _save.selectedPetAccessoryId;
        for (var i = 0; i < 2; i++)
        {
            var snapshot = new SceneSnapshot { roomId = i == 0 ? "room.sunny" : "room.cozy" };
            if (_save.roomJson != null && _save.roomJson.Length > i && !string.IsNullOrWhiteSpace(_save.roomJson[i]))
            {
                try { snapshot = JsonUtility.FromJson<SceneSnapshot>(_save.roomJson[i]) ?? snapshot; } catch { }
            }
            snapshot.elements ??= new List<SceneElementData>();
            _rooms.Add(snapshot);
        }
    }

    private void Persist()
    {
        _save.languageCode = _language.Code;
        _save.selectedSkinId = _selectedSkin;
        _save.selectedHairId = _selectedHair;
        _save.selectedOutfitId = _selectedOutfit;
        _save.selectedShoesId = _selectedShoes;
        _save.selectedAccessoryId = _selectedAccessory;
        _save.selectedPetId = _selectedPet;
        _save.selectedPetAccessoryId = _selectedPetAccessory;
        _save.roomJson = _rooms.Select(JsonUtility.ToJson).ToArray();
        _saveService.Save(_save);
    }

    private void OnApplicationPause(bool paused) { if (paused) Persist(); }
    private void OnApplicationQuit() => Persist();

    private void Header(string title, Action back)
    {
        var header = UiFactory.Panel(_content, "Header", Color.clear, new Vector2(0, 0.86f), new Vector2(1, 1));
        UiFactory.Button(header, "‹  " + _language.Get("menu.back"), UiFactory.Pink, () => back());
        var backRect = (RectTransform)header.GetChild(0);
        backRect.anchorMin = new Vector2(0, 0.18f); backRect.anchorMax = new Vector2(0.2f, 0.82f); backRect.offsetMin = Vector2.zero; backRect.offsetMax = Vector2.zero;
        var titleRect = UiFactory.Panel(header, "Title", Color.clear, new Vector2(0.2f, 0), new Vector2(0.8f, 1));
        UiFactory.Label(titleRect, title, 30, UiFactory.Ink);
    }

    private void ShowHome()
    {
        UiFactory.Clear(_content);
        var hero = UiFactory.Panel(_content, "Hero", UiFactory.Lavender, new Vector2(0, 0.56f), new Vector2(1, 1));
        var title = UiFactory.Label(hero, _language.Get("app.title"), 48, UiFactory.Ink);
        title.fontStyle = FontStyle.Bold;
        var subtitle = UiFactory.Label(hero, _language.Get("app.subtitle"), 22, new Color(0.35f, 0.29f, 0.47f));
        var subRect = (RectTransform)subtitle.transform; subRect.anchorMin = new Vector2(0.2f, 0.12f); subRect.anchorMax = new Vector2(0.8f, 0.44f); subRect.offsetMin = Vector2.zero; subRect.offsetMax = Vector2.zero;
        var actions = UiFactory.Panel(_content, "Actions", Color.clear, new Vector2(0, 0.06f), new Vector2(1, 0.52f));
        AddHomeButton(actions, _language.Get("menu.avatar"), UiFactory.Pink, new Vector2(0.02f, 0.53f), new Vector2(0.32f, 0.94f), ShowAvatar);
        AddHomeButton(actions, _language.Get("menu.pet"), UiFactory.Mint, new Vector2(0.34f, 0.53f), new Vector2(0.66f, 0.94f), ShowPet);
        AddHomeButton(actions, _language.Get("menu.house"), new Color(1f, 0.93f, 0.74f), new Vector2(0.68f, 0.53f), new Vector2(0.98f, 0.94f), ShowHouse);
        AddHomeButton(actions, _language.Get("menu.photo"), new Color(0.85f, 0.91f, 1f), new Vector2(0.02f, 0.06f), new Vector2(0.32f, 0.47f), ShowPhoto);
        AddHomeButton(actions, _language.Get("menu.settings"), new Color(0.91f, 0.88f, 0.97f), new Vector2(0.34f, 0.06f), new Vector2(0.66f, 0.47f), ShowSettings);
        var hint = UiFactory.Label(actions, "Offline • 2D • 6–12", 16, new Color(0.42f, 0.38f, 0.52f));
        var hintRect = (RectTransform)hint.transform; hintRect.anchorMin = new Vector2(0.68f, 0.06f); hintRect.anchorMax = new Vector2(0.98f, 0.47f); hintRect.offsetMin = Vector2.zero; hintRect.offsetMax = Vector2.zero;
    }

    private static void AddHomeButton(Transform parent, string text, Color color, Vector2 min, Vector2 max, Action click)
    {
        var button = UiFactory.Button(parent, text, color, () => click());
        var rect = (RectTransform)button.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private void ShowAvatar()
    {
        UiFactory.Clear(_content);
        Header(_language.Get("avatar.title"), ShowHome);
        var preview = UiFactory.Panel(_content, "Preview", new Color(0.94f, 0.91f, 1f), new Vector2(0, 0.05f), new Vector2(0.38f, 0.84f));
        RenderCharacter(preview, new Vector2(0.5f, 0.46f), 1.55f);
        var choices = UiFactory.Panel(_content, "Choices", Color.clear, new Vector2(0.4f, 0.05f), new Vector2(1, 0.84f));
        AddChoiceRow(choices, "Skin", ItemCategory.Skin, _selectedSkin, id => { _selectedSkin = id; Persist(); ShowAvatar(); });
        AddChoiceRow(choices, "Hair", ItemCategory.Hair, _selectedHair, id => { _selectedHair = id; Persist(); ShowAvatar(); });
        AddChoiceRow(choices, "Outfit", ItemCategory.Outfit, _selectedOutfit, id => { _selectedOutfit = id; Persist(); ShowAvatar(); });
        AddChoiceRow(choices, "Accessory", ItemCategory.HumanAccessory, _selectedAccessory, id => { _selectedAccessory = id; Persist(); ShowAvatar(); });
    }

    private void AddChoiceRow(Transform parent, string heading, ItemCategory category, string selected, Action<string> choose)
    {
        var row = UiFactory.Panel(parent, heading, Color.clear, new Vector2(0, 0.73f - parent.childCount * 0.22f), new Vector2(1, 0.93f - parent.childCount * 0.22f));
        UiFactory.Label(row, heading, 18, UiFactory.Ink, TextAnchor.MiddleLeft);
        var items = _catalog.ByCategory(category).ToList();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var button = UiFactory.Button(row, _language.Get(item.displayKey) + (item.free ? "" : "  ★"), item.itemId == selected ? UiFactory.Pink : Color.white, () => ChooseItem(item, choose));
            var rect = (RectTransform)button.transform; rect.anchorMin = new Vector2(0.18f + i * 0.2f, 0.08f); rect.anchorMax = new Vector2(0.36f + i * 0.2f, 0.88f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
    }

    private void ChooseItem(ItemDefinition item, Action<string> choose)
    {
        if (item.free || _save.unlockedItemIds.Contains(item.itemId)) { choose(item.itemId); return; }
        if (_ads.IsAvailable && _adsRemaining() > 0)
            _ads.ShowForItem(item.itemId, success => { if (success) { _save.unlockedItemIds = _save.unlockedItemIds.Append(item.itemId).Distinct().ToArray(); Persist(); choose(item.itemId); } });
    }

    private int _adsRemaining()
    {
        var limiter = new DailyRewardLimiter(_save.dailyRewardLimit, DateTime.TryParse(_save.lastRewardDate, out var date) ? date : null, _save.rewardsUsedToday);
        return limiter.Remaining(DateTime.Now);
    }

    private void RenderCharacter(Transform parent, Vector2 center, float scale)
    {
        var root = new GameObject("Character", typeof(RectTransform)); root.transform.SetParent(parent, false);
        var rootRect = (RectTransform)root.transform; rootRect.anchorMin = center; rootRect.anchorMax = center; rootRect.sizeDelta = new Vector2(220, 300) * scale;
        var skin = _catalog.Get(_selectedSkin)?.tint ?? new Color(0.98f, 0.75f, 0.58f);
        var outfit = _catalog.Get(_selectedOutfit)?.tint ?? UiFactory.Pink;
        var hair = _catalog.Get(_selectedHair)?.tint ?? new Color(0.25f, 0.12f, 0.08f);
        UiFactory.Image(root.transform, "Body", ColorSpriteFactory.Square, outfit, new Vector2(0.5f, 0.25f), new Vector2(110, 130) * scale);
        UiFactory.Image(root.transform, "Head", ColorSpriteFactory.Circle, skin, new Vector2(0.5f, 0.68f), new Vector2(116, 116) * scale);
        UiFactory.Image(root.transform, "Hair", ColorSpriteFactory.Circle, hair, new Vector2(0.5f, 0.80f), new Vector2(122, 70) * scale);
        var pet = _catalog.Get(_selectedAccessory)?.tint ?? Color.yellow;
        UiFactory.Image(root.transform, "Accessory", ColorSpriteFactory.Circle, pet, new Vector2(0.72f, 0.78f), new Vector2(32, 32) * scale);
    }

    private void ShowPet()
    {
        UiFactory.Clear(_content);
        Header(_language.Get("pet.title"), ShowHome);
        var preview = UiFactory.Panel(_content, "Preview", UiFactory.Mint, new Vector2(0, 0.05f), new Vector2(0.42f, 0.84f));
        var pet = _catalog.Get(_selectedPet);
        UiFactory.Image(preview, "Pet", ColorSpriteFactory.Circle, pet?.tint ?? Color.white, new Vector2(0.5f, 0.53f), new Vector2(220, 220));
        UiFactory.Label(preview, _language.Get(pet?.displayKey ?? "item.pet.cat"), 24, UiFactory.Ink);
        var choices = UiFactory.Panel(_content, "Choices", Color.clear, new Vector2(0.44f, 0.05f), new Vector2(1, 0.84f));
        AddChoiceRow(choices, "Pet", ItemCategory.Pet, _selectedPet, id => { _selectedPet = id; Persist(); ShowPet(); });
        AddChoiceRow(choices, "Accessory", ItemCategory.PetAccessory, _selectedPetAccessory, id => { _selectedPetAccessory = id; Persist(); ShowPet(); });
    }

    private void ShowHouse()
    {
        UiFactory.Clear(_content);
        Header(_language.Get("house.title"), ShowHome);
        BuildRoomTabs(_content);
        _currentSnapshot = _rooms[_roomIndex];
        BuildScenePanel(_content, new Vector2(0, 0.05f), new Vector2(0.68f, 0.78f), showCharacters: true);
        var items = UiFactory.Panel(_content, "Furniture", Color.clear, new Vector2(0.7f, 0.05f), new Vector2(1, 0.78f));
        UiFactory.Label(items, "Itens", 20, UiFactory.Ink, TextAnchor.UpperCenter);
        var furniture = _catalog.All().Where(item => item.category is ItemCategory.Furniture or ItemCategory.Decor).ToList();
        for (var i = 0; i < furniture.Count; i++)
        {
            var item = furniture[i];
            var button = UiFactory.Button(items, _language.Get(item.displayKey) + (item.free ? "" : "  ★"), item.free ? Color.white : UiFactory.Pink, () => AddFurniture(item));
            var rect = (RectTransform)button.transform; rect.anchorMin = new Vector2(0.05f, 0.78f - (i % 4) * 0.18f); rect.anchorMax = new Vector2(0.95f, 0.91f - (i % 4) * 0.18f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        var photo = UiFactory.Button(_content, "📸  " + _language.Get("menu.photo"), new Color(0.85f, 0.91f, 1f), ShowPhoto);
        var photoRect = (RectTransform)photo.transform; photoRect.anchorMin = new Vector2(0.7f, 0.82f); photoRect.anchorMax = new Vector2(1, 0.94f); photoRect.offsetMin = Vector2.zero; photoRect.offsetMax = Vector2.zero;
    }

    private void BuildRoomTabs(Transform parent)
    {
        var tabs = UiFactory.Panel(parent, "Rooms", Color.clear, new Vector2(0, 0.79f), new Vector2(0.68f, 0.86f));
        for (var i = 0; i < 2; i++)
        {
            var index = i;
            var button = UiFactory.Button(tabs, _language.Get(i == 0 ? "room.sunny" : "room.cozy"), i == _roomIndex ? UiFactory.Pink : Color.white, () => { _roomIndex = index; Persist(); ShowHouse(); });
            var rect = (RectTransform)button.transform; rect.anchorMin = new Vector2(i * 0.5f, 0); rect.anchorMax = new Vector2((i + 1) * 0.5f, 1); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
    }

    private void BuildScenePanel(Transform parent, Vector2 min, Vector2 max, bool showCharacters)
    {
        var scene = UiFactory.Panel(parent, "Scene", _roomIndex == 0 ? new Color(0.76f, 0.91f, 1f) : new Color(0.92f, 0.78f, 0.66f), min, max);
        var floor = UiFactory.Panel(scene, "Floor", _roomIndex == 0 ? new Color(0.98f, 0.84f, 0.69f) : new Color(0.70f, 0.53f, 0.38f), new Vector2(0, 0), new Vector2(1, 0.25f));
        if (showCharacters)
        {
            RenderCharacter(scene, new Vector2(0.30f, 0.48f), 0.72f);
            var pet = _catalog.Get(_selectedPet);
            UiFactory.Image(scene, "Pet", ColorSpriteFactory.Circle, pet?.tint ?? Color.white, new Vector2(0.58f, 0.32f), new Vector2(90, 90));
        }
        RenderSnapshot(scene, _currentSnapshot);
    }

    private void RenderSnapshot(Transform scene, SceneSnapshot snapshot)
    {
        foreach (var element in snapshot.elements)
        {
            var item = _catalog.Get(element.itemId);
            if (item == null) continue;
            var image = UiFactory.Image(scene, item.itemId, ColorSpriteFactory.Square, item.tint, new Vector2(element.x, element.y), new Vector2(86, 58) * element.scale);
            image.transform.SetSiblingIndex(Mathf.Clamp(element.order + 2, 0, scene.childCount - 1));
            var drag = image.gameObject.AddComponent<DraggableSceneElement>();
            drag.ItemId = item.itemId;
            drag.Changed = pos => { element.x = pos.x; element.y = pos.y; Persist(); };
            UiFactory.Label(image.transform, _language.Get(item.displayKey), 12, UiFactory.Ink);
        }
    }

    private void AddFurniture(ItemDefinition item)
    {
        if (!item.free && !_save.unlockedItemIds.Contains(item.itemId))
        {
            if (_adsRemaining() <= 0) return;
            _ads.ShowForItem(item.itemId, success => { if (success) { _save.unlockedItemIds = _save.unlockedItemIds.Append(item.itemId).Distinct().ToArray(); AddFurniture(item); } });
            return;
        }
        _currentSnapshot.elements.Add(new SceneElementData { itemId = item.itemId, x = 0.5f, y = 0.35f, scale = 1f, order = _currentSnapshot.elements.Count });
        Persist();
        ShowHouse();
    }

    private void ShowPhoto()
    {
        UiFactory.Clear(_content);
        Header(_language.Get("photo.title"), ShowHome);
        _currentSnapshot = _rooms[_roomIndex];
        BuildScenePanel(_content, new Vector2(0.05f, 0.18f), new Vector2(0.75f, 0.84f), showCharacters: true);
        var side = UiFactory.Panel(_content, "PhotoActions", Color.clear, new Vector2(0.77f, 0.18f), new Vector2(1, 0.84f));
        UiFactory.Label(side, _language.Get("photo.offline"), 20, UiFactory.Ink);
        var save = UiFactory.Button(side, "⬇  " + _language.Get("photo.save"), UiFactory.Mint, SavePhoto);
        var saveRect = (RectTransform)save.transform; saveRect.anchorMin = new Vector2(0.05f, 0.18f); saveRect.anchorMax = new Vector2(0.95f, 0.42f); saveRect.offsetMin = Vector2.zero; saveRect.offsetMax = Vector2.zero;
    }

    private void SavePhoto()
    {
        var success = _photo.CaptureAndSave(_currentSnapshot);
        var notice = UiFactory.Label(_content, success ? _language.Get("photo.saved") : "Não foi possível salvar", 24, success ? new Color(0.20f, 0.52f, 0.35f) : Color.red);
        var rect = (RectTransform)notice.transform; rect.anchorMin = new Vector2(0.05f, 0.05f); rect.anchorMax = new Vector2(0.75f, 0.16f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private void ShowSettings()
    {
        UiFactory.Clear(_content);
        Header(_language.Get("menu.settings"), ShowHome);
        var panel = UiFactory.Panel(_content, "Settings", Color.clear, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.76f));
        UiFactory.Label(panel, _language.Get("common.language"), 26, UiFactory.Ink);
        var pt = UiFactory.Button(panel, _language.Get("language.pt"), _language.Current == AlbaLanguage.PortugueseBrazil ? UiFactory.Pink : Color.white, () => ChangeLanguage("pt-BR"));
        var ptRect = (RectTransform)pt.transform; ptRect.anchorMin = new Vector2(0.08f, 0.35f); ptRect.anchorMax = new Vector2(0.46f, 0.62f); ptRect.offsetMin = Vector2.zero; ptRect.offsetMax = Vector2.zero;
        var en = UiFactory.Button(panel, _language.Get("language.en"), _language.Current == AlbaLanguage.English ? UiFactory.Pink : Color.white, () => ChangeLanguage("en"));
        var enRect = (RectTransform)en.transform; enRect.anchorMin = new Vector2(0.54f, 0.35f); enRect.anchorMax = new Vector2(0.92f, 0.62f); enRect.offsetMin = Vector2.zero; enRect.offsetMax = Vector2.zero;
    }

    private void ChangeLanguage(string code)
    {
        _language.Set(code);
        Persist();
        ShowSettings();
    }
}
