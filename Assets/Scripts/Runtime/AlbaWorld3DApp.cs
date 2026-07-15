using System;
using System.Collections.Generic;
using System.Linq;
using AlbaWorld;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Pets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AlbaWorld.Runtime;

/// <summary>
/// The first real Alba World presentation. It is deliberately small, but it is a 3D
/// playable loop: the child can switch the character, choose a Kenney pet, watch it
/// follow the character, save locally, and capture a photo offline.
/// </summary>
[DisallowMultipleComponent]
public sealed class AlbaWorld3DApp : MonoBehaviour
{
    private static readonly Color Ink = new(0.08f, 0.06f, 0.16f, 1f);
    private static readonly Color Lavender = new(0.36f, 0.26f, 0.58f, 1f);
    private static readonly Color PanelColor = new(0.06f, 0.07f, 0.13f, 0.88f);
    private static readonly Color PanelSoft = new(0.12f, 0.13f, 0.22f, 0.92f);
    private static readonly Color Pink = new(1f, 0.34f, 0.60f, 1f);
    private static readonly Color Mint = new(0.28f, 0.86f, 0.73f, 1f);

    [SerializeField] private GameObject? _girlPrefab;
    [SerializeField] private GameObject? _boyPrefab;
    [SerializeField] private ItemCatalog3D? _petCatalog;
    [SerializeField] private ItemCatalog3D? _itemCatalog;

    private LocalSaveService _saveService = null!;
    private GameSaveData _save = null!;
    private LanguageService _language = null!;
    private PhotoExporter _photo = null!;
    private PetAssemblyController _petAssembly = null!;
    private PetPersistenceCoordinator _petFlow = null!;
    private RoomFurnitureController _furniture = null!;
    private GameObject _character = null!;
    private Transform _worldRoot = null!;
    private Transform _petMount = null!;
    private AlbaWorldUiController _ui = null!;
    private GameObject _hud = null!;
    private bool _started;

    private void Start()
    {
        if (_started)
            return;

        _started = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Application.targetFrameRate = 60;
        _saveService = new LocalSaveService();
        _save = SaveMigration.Upgrade(_saveService.Load());
        _language = new LanguageService(_save.languageCode);
        _photo = new PhotoExporter();
        _petCatalog ??= Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        _itemCatalog ??= Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        _girlPrefab ??= Resources.Load<GameObject>("Characters/BodyGirl");
        _boyPrefab ??= Resources.Load<GameObject>("Characters/BodyBoy");

        EnsureEventSystem();
        CreateStudio();
        CreateCharacter();
        CreatePet();
        CreateFurniture();
        CreateHud();
        Persist();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void CreateStudio()
    {
        var existing = GameObject.Find("Alba World 3D");
        _worldRoot = existing != null ? existing.transform : new GameObject("Alba World 3D").transform;
        _worldRoot.SetParent(null, false);

        var camera = Camera.main;
        if (camera == null)
        {
            camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
        }

        camera.backgroundColor = new Color(0.05f, 0.06f, 0.12f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.fieldOfView = 35f;
        camera.transform.SetPositionAndRotation(new Vector3(0f, 3.4f, -8.5f), Quaternion.identity);
        camera.transform.LookAt(new Vector3(0f, 1.25f, 0.45f));

        CreateSurface("Floor", PrimitiveType.Cube, new Vector3(0f, -0.18f, 0.5f), new Vector3(11f, 0.3f, 8f), new Color(0.16f, 0.12f, 0.25f));
        CreateSurface("BackWall", PrimitiveType.Cube, new Vector3(0f, 3.2f, 4.1f), new Vector3(11f, 6.8f, 0.25f), new Color(0.11f, 0.09f, 0.20f));
        CreateSurface("LeftWall", PrimitiveType.Cube, new Vector3(-5.5f, 3.1f, 0.5f), new Vector3(0.25f, 6.6f, 7.2f), new Color(0.20f, 0.13f, 0.29f));
        CreateSurface("Stage", PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0.65f), new Vector3(3.7f, 0.12f, 2.7f), new Color(0.97f, 0.39f, 0.64f));

        CreateLight("Key Light", new Vector3(-2f, 5.8f, -3f), new Color(1f, 0.73f, 0.83f), 4.8f, 9f);
        CreateLight("Fill Light", new Vector3(3f, 3.6f, 1f), new Color(0.51f, 0.74f, 1f), 3.1f, 7f);

        _petMount = new GameObject("Pet Mount").transform;
        _petMount.SetParent(_worldRoot, false);
        _petMount.localPosition = new Vector3(1.45f, 0.20f, 0.4f);
    }

    private void CreateCharacter()
    {
        var prefab = _save.character.bodyId == "body.boy" ? _boyPrefab : _girlPrefab;
        prefab ??= _girlPrefab ?? _boyPrefab;
        if (prefab == null)
        {
            Debug.LogError("Alba World character prefab is missing.");
            _character = new GameObject("Character 3D");
            return;
        }

        _character = Instantiate(prefab, _worldRoot, false);
        _character.name = "Character 3D";
        _character.transform.localPosition = new Vector3(-0.45f, 0.19f, 0.5f);
        NormalizeHeight(_character, 2.25f);
        _character.AddComponent<StudioIdleMotion>().Amplitude = 0.012f;
    }

    private void CreatePet()
    {
        if (_petCatalog == null)
        {
            Debug.LogError("Alba World 3D pet catalog is missing.");
            return;
        }

        _petAssembly = _petMount.gameObject.AddComponent<PetAssemblyController>();
        _petAssembly.Initialize(_petCatalog, _petMount);
        _petFlow = new PetPersistenceCoordinator(_save, _saveService, _petAssembly);
        _petAssembly.PetApplied += OnPetApplied;
        _petFlow.Restore();
        SetupPetFollow(_petAssembly.ActiveInstance);
    }

    private void OnPetApplied(GameObject instance, PetLoadoutData _)
    {
        NormalizeHeight(instance, 1.1f);
        SetupPetFollow(instance);
        _ui?.SetPetName(_language.Get("item." + instance.name));
    }

    private void SetupPetFollow(GameObject? instance)
    {
        if (instance == null || _character == null)
            return;

        var follow = instance.GetComponent<PetFollowController>() ?? instance.AddComponent<PetFollowController>();
        follow.FollowTarget = _character.transform;
        follow.FollowOffset = new Vector3(1.45f, 0f, 0.2f);
        follow.FollowSpeed = 2.2f;
        follow.TurnSpeed = 180f;
        follow.FloorHeight = 0.2f;
        instance.AddComponent<StudioIdleMotion>().Amplitude = 0.025f;
    }

    private void CreateFurniture()
    {
        if (_itemCatalog == null)
        {
            Debug.LogError("Alba World 3D furniture catalog is missing.");
            return;
        }

        var root = new GameObject("Room Furniture").transform;
        root.SetParent(_worldRoot, false);
        _furniture = root.gameObject.AddComponent<RoomFurnitureController>();
        _furniture.Initialize(_itemCatalog, root, _save, _saveService);

        var room = _save.rooms3D?.FirstOrDefault(layout =>
            string.Equals(layout.roomId, _save.activeRoomId, StringComparison.Ordinal));
        if (room != null)
            return;

        // A small, real-model starter layout makes the first launch immediately readable.
        _furniture.TryAdd("furniture.bed", new Vector3(-3.1f, 0.2f, 2.0f));
        _furniture.TryAdd("furniture.sofa", new Vector3(2.3f, 0.2f, 2.2f));
        _furniture.TryAdd("furniture.table", new Vector3(2.1f, 0.2f, -0.9f));
        _furniture.TryAdd("furniture.plant", new Vector3(-3.7f, 0.2f, -1.3f));
        _furniture.TryAdd("furniture.rug", new Vector3(0f, 0.2f, 0.55f));
    }

    private void CreateHud()
    {
        var canvasObject = new GameObject("Alba World HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _hud = canvasObject;
        _ui = canvasObject.AddComponent<AlbaWorldUiController>();
        _ui.Initialize(
            _language,
            _furniture,
            AddFurniture,
            ScaleFurniture,
            MirrorFurniture,
            BringFurnitureForward,
            SendFurnitureBackward,
            RemoveFurniture,
            SwitchCharacter,
            CapturePhoto,
            ChangeRoomStyle,
            ToggleLanguage,
            SelectPet,
            () => { });
        _ui.SetPetName(_language.Get("item." + _save.pet.petId));
    }

    private void AddFurniture(string itemId)
    {
        if (_furniture == null)
            return;

        var position = new Vector3(
            Mathf.Lerp(-3.6f, 3.0f, Mathf.Abs(itemId.GetHashCode() % 100) / 100f),
            0.2f,
            Mathf.Lerp(-1.5f, 2.6f, Mathf.Abs(itemId.GetHashCode() % 100) / 100f));
        var added = _furniture.TryAdd(itemId, position);
        ShowNotice(added ? _language.Get("hud.saved") : _language.Get("photo.error"), added);
    }

    private void ScaleFurniture(float delta)
    {
        if (_furniture == null || string.IsNullOrWhiteSpace(_furniture.SelectedInstanceId))
            return;
        ShowNotice(_furniture.TryScale(_furniture.SelectedInstanceId, delta) ? _language.Get("hud.saved") : _language.Get("photo.error"), true);
    }

    private void MirrorFurniture()
    {
        if (_furniture == null || string.IsNullOrWhiteSpace(_furniture.SelectedInstanceId))
            return;
        ShowNotice(_furniture.TryMirror(_furniture.SelectedInstanceId) ? _language.Get("hud.saved") : _language.Get("photo.error"), true);
    }

    private void BringFurnitureForward()
    {
        if (_furniture == null || string.IsNullOrWhiteSpace(_furniture.SelectedInstanceId))
            return;
        _furniture.TryBringForward(_furniture.SelectedInstanceId);
    }

    private void SendFurnitureBackward()
    {
        if (_furniture == null || string.IsNullOrWhiteSpace(_furniture.SelectedInstanceId))
            return;
        _furniture.TrySendBackward(_furniture.SelectedInstanceId);
    }

    private void RemoveFurniture()
    {
        if (_furniture == null || string.IsNullOrWhiteSpace(_furniture.SelectedInstanceId))
            return;
        ShowNotice(_furniture.TryRemove(_furniture.SelectedInstanceId) ? _language.Get("hud.saved") : _language.Get("photo.error"), true);
    }

    private void SelectPet(string id)
    {
        if (_petFlow == null || !_petFlow.TrySelect(id))
        {
            ShowNotice(_language.Get("hud.petUnavailable"), false);
            return;
        }

        _save.pet.petId = id;
        _save.selectedPetId = id;
        _ui?.SetPetName(_language.Get("item." + id));
        ShowNotice(_language.Get("hud.saved"), true);
    }

    private void SwitchCharacter()
    {
        _save.character.bodyId = _save.character.bodyId == "body.boy" ? "body.girl" : "body.boy";
        if (_character != null)
            Destroy(_character);
        CreateCharacter();
        SetupPetFollow(_petAssembly?.ActiveInstance);
        Persist();
        ShowNotice(_language.Get("hud.saved"), true);
    }

    private void ChangeRoomStyle()
    {
        var stage = _worldRoot.Find("Stage")?.GetComponent<Renderer>();
        _save.activeRoomId = _save.activeRoomId == "room.sunny" ? "room.cozy" : "room.sunny";
        if (stage != null)
            stage.sharedMaterial = CreateMaterial(_save.activeRoomId == "room.sunny" ? new Color(0.97f, 0.39f, 0.64f) : new Color(0.37f, 0.82f, 0.78f));
        _furniture?.SetRoom(_save.activeRoomId);
        Persist();
        ShowNotice(_language.Get("hud.saved"), true);
    }

    private void CapturePhoto()
    {
        var snapshot = new SceneSnapshot { roomId = _save.activeRoomId };
        var success = _photo.CaptureAndSave(snapshot);
        ShowNotice(success ? _language.Get("photo.saved") : _language.Get("photo.error"), success);
    }

    private void ToggleLanguage()
    {
        _language.Set(_language.Code == "pt-BR" ? "en" : "pt-BR");
        _save.languageCode = _language.Code;
        Persist();
        _ui?.RefreshLanguage();
        ShowNotice(_language.Get("hud.saved"), true);
    }

    private void ShowNotice(string message, bool success)
    {
        _ui?.ShowNotice(message, success);
    }

    private void Persist()
    {
        if (_save == null || _saveService == null)
            return;

        _save.languageCode = _language.Code;
        _save.selectedPetId = _save.pet.petId;
        _save.schemaVersion = SaveMigration.CurrentSchemaVersion;
        _saveService.Save(_save);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            Persist();
    }

    private void OnApplicationQuit() => Persist();

    private GameObject CreateSurface(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        var surface = GameObject.CreatePrimitive(type);
        surface.name = name;
        surface.transform.SetParent(_worldRoot, false);
        surface.transform.localPosition = position;
        surface.transform.localScale = scale;
        var renderer = surface.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreateMaterial(color);
        return surface;
    }

    private void CreateLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        var go = new GameObject(name, typeof(Light));
        go.transform.SetParent(_worldRoot, false);
        go.transform.localPosition = position;
        var light = go.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
    }

    private static Material CreateMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        return material;
    }

    private static void NormalizeHeight(GameObject root, float targetHeight)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.y <= 0.0001f)
            return;

        root.transform.localScale *= targetHeight / bounds.size.y;
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

    private static Text Label(Transform parent, string value, int size, Color color, TextAnchor alignment)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button AddButton(Transform parent, string value, Color color, Action click, Vector2 min, Vector2 max, int fontSize = 17)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        Anchor(rect, min, max);
        var image = go.GetComponent<Image>();
        image.color = color;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => click());
        var text = Label(go.transform, value, fontSize, Color.white, TextAnchor.MiddleCenter);
        Anchor(text.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.98f));
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

/// <summary>Small deterministic motion used only for the studio presentation.</summary>
public sealed class StudioIdleMotion : MonoBehaviour
{
    public float Amplitude { get; set; } = 0.02f;
    private Vector3 _start;

    private void Awake() => _start = transform.localPosition;

    private void Update()
    {
        transform.localPosition = _start + Vector3.up * (Mathf.Sin(Time.time * 2.2f) * Amplitude);
    }
}
