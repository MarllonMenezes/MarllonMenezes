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
    [SerializeField] private GameObject? _girlPrefab;
    [SerializeField] private GameObject? _boyPrefab;
    [SerializeField] private CharacterPresetCatalog? _characterPresetCatalog;
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
    private CharacterMovementController _movement = null!;
    private CharacterWardrobeController _wardrobe = null!;
    private CharacterPresetController _presetController = null!;
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
        _characterPresetCatalog ??= Resources.Load<CharacterPresetCatalog>("Data/CartoonCityCharacterPresets");
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
        camera.fieldOfView = 38f;
        camera.transform.SetPositionAndRotation(new Vector3(0f, 3.4f, -8.5f), Quaternion.identity);
        camera.transform.LookAt(new Vector3(0f, 1.20f, 0.25f));

        CreateSurface("Floor", PrimitiveType.Cube, new Vector3(0f, -0.18f, 0.5f), new Vector3(11f, 0.3f, 8f), new Color(0.16f, 0.12f, 0.25f));
        CreateSurface("BackWall", PrimitiveType.Cube, new Vector3(0f, 3.2f, 4.1f), new Vector3(11f, 6.8f, 0.25f), new Color(0.11f, 0.09f, 0.20f));
        CreateSurface("LeftWall", PrimitiveType.Cube, new Vector3(-5.5f, 3.1f, 0.5f), new Vector3(0.25f, 6.6f, 7.2f), new Color(0.20f, 0.13f, 0.29f));
        CreateLight("Key Light", new Vector3(-2f, 5.8f, -3f), new Color(1f, 0.73f, 0.83f), 4.8f, 9f);
        CreateLight("Fill Light", new Vector3(3f, 3.6f, 1f), new Color(0.51f, 0.74f, 1f), 3.1f, 7f);

        _petMount = new GameObject("Pet Mount").transform;
        _petMount.SetParent(_worldRoot, false);
        _petMount.localPosition = new Vector3(1.45f, 0.20f, 0.4f);
    }

    private void CreateCharacter()
    {
        var preset = _characterPresetCatalog?.Get(_save.character.characterPresetId);
        var prefab = preset?.prefab;
        if (prefab == null)
            prefab = _save.character.bodyId == "body.boy" ? _boyPrefab : _girlPrefab;
        prefab ??= _girlPrefab ?? _boyPrefab;
        if (prefab == null)
        {
            Debug.LogError("Alba World character prefab is missing.");
            _character = new GameObject("Character 3D");
            return;
        }

        _character = Instantiate(prefab, _worldRoot, false);
        _character.name = "Character 3D";
        if (IsNewWorldPosition())
            _save.playerWorld.position = new SerializableVector3(-1f, 0.22f, -0.8f);
        _character.transform.localPosition = new Vector3(-1f, 0.22f, -0.8f);
        NormalizeHeight(_character, 2.25f);
        var characterSelectable = _character.GetComponent<WorldSelectable>();
        if (characterSelectable == null)
            characterSelectable = _character.AddComponent<WorldSelectable>();
        characterSelectable.Configure(WorldSelectableKind.Character, "character");
        var characterCollider = _character.GetComponent<BoxCollider>();
        if (characterCollider == null)
        {
            characterCollider = _character.AddComponent<BoxCollider>();
            characterCollider.center = new Vector3(0f, 1.05f, 0f);
            characterCollider.size = new Vector3(0.85f, 2.1f, 0.70f);
        }
        _movement = _character.GetComponent<CharacterMovementController>() ?? _character.AddComponent<CharacterMovementController>();
        _movement.Initialize(_character.transform, _save, _saveService, RoomFurnitureController.DefaultWalkableBounds, 0.22f);
        if (_characterPresetCatalog != null && preset != null)
        {
            _presetController = _character.GetComponent<CharacterPresetController>();
            if (_presetController == null)
                _presetController = _character.AddComponent<CharacterPresetController>();
            _presetController.Initialize(_characterPresetCatalog, _character.transform, _save, _saveService);
        }
        if (_ui != null)
            _movement.SetInputEnabled(_ui.Mode == AlbaWorldUiMode.Casa);
        if (_itemCatalog != null)
        {
            _wardrobe = _character.GetComponent<CharacterWardrobeController>() ?? _character.AddComponent<CharacterWardrobeController>();
            _wardrobe.Initialize(_itemCatalog, _character.transform, _save, _saveService);
            if (_presetController != null)
                _wardrobe.AttachPresetController(_presetController);
        }
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
        var selectable = instance.GetComponent<WorldSelectable>();
        if (selectable == null)
            selectable = instance.AddComponent<WorldSelectable>();
        selectable.Configure(WorldSelectableKind.Pet, "pet");
        if (instance.GetComponent<Collider>() == null)
        {
            var collider = instance.AddComponent<BoxCollider>();
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1))
                    bounds.Encapsulate(renderer.bounds);
                collider.center = instance.transform.InverseTransformPoint(bounds.center);
                collider.size = bounds.size;
            }
        }
        var placement = instance.GetComponent<PetPlacementController>();
        if (placement == null)
            placement = instance.AddComponent<PetPlacementController>();
        placement.Initialize(
            instance.transform,
            _save,
            _saveService,
            new Bounds(new Vector3(0f, 0.2f, 0.45f), new Vector3(8.2f, 0.1f, 5.2f)),
            0.2f,
            follow,
            _worldRoot);
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
        if (room?.placements is { Length: > 0 })
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
        _ui.ModeChanged += OnUiModeChanged;
        _ui.Initialize(
            _language,
            _furniture,
            AddFurniture,
            ScaleFurniture,
            MirrorFurniture,
            BringFurnitureForward,
            SendFurnitureBackward,
            RemoveFurniture,
            UndoRemoveFurniture,
            SwitchCharacter,
            CapturePhoto,
            ChangeRoomStyle,
            ToggleLanguage,
            SelectPet,
            () => { },
            SelectCharacterPreset,
            CompleteOnboarding);
        _ui.SetPetName(_language.Get("item." + _save.pet.petId));
        _ui.SetRoomName(_language.Get(_save.activeRoomId == "room.cozy" ? "room.cozy" : "room.sunny"));
        _ui.AttachWardrobe(_wardrobe);
        _ui.AttachCharacterPresets(_presetController);
        if (!_save.onboardingCompleted)
            _ui.EnterWelcomeMode();
        _movement?.SetInputEnabled(_ui.Mode == AlbaWorldUiMode.Casa);
    }

    private void CompleteOnboarding()
    {
        _save.onboardingCompleted = true;
        Persist();
        _ui?.EnterHouseMode();
    }

    private void AddFurniture(string itemId)
    {
        if (_furniture == null)
            return;

        var added = _furniture.TryAddToFirstFreeSlot(itemId);
        ShowNotice(added ? _language.Get("hud.saved") : _language.Get("hud.noFreeSlot"), added);
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

    private void UndoRemoveFurniture()
    {
        if (_furniture == null)
            return;
        var restored = _furniture.TryUndoRemove();
        ShowNotice(restored ? _language.Get("hud.saved") : _language.Get("photo.error"), restored);
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
        var presets = _characterPresetCatalog?.All.OrderBy(preset => preset.sortOrder).ToArray() ?? Array.Empty<CharacterPresetDefinition>();
        if (presets.Length > 1)
        {
            var currentIndex = Array.FindIndex(presets, preset => preset.presetId == _save.character.characterPresetId);
            var next = presets[(currentIndex + 1 + presets.Length) % presets.Length];
            _save.character.characterPresetId = next.presetId;
            _save.character.bodyId = next.presetId.EndsWith(".02", StringComparison.Ordinal) ? "body.boy" : "body.girl";
        }
        else
        {
            _save.character.bodyId = _save.character.bodyId == "body.boy" ? "body.girl" : "body.boy";
        }
        if (_character != null)
            Destroy(_character);
        _movement = null!;
        _presetController = null!;
        CreateCharacter();
        _ui?.AttachWardrobe(_wardrobe);
        _ui?.AttachCharacterPresets(_presetController);
        SetupPetFollow(_petAssembly?.ActiveInstance);
        Persist();
        ShowNotice(_language.Get("hud.saved"), true);
    }

    private void SelectCharacterPreset(string presetId)
    {
        if (_presetController == null || !_presetController.TrySelect(presetId))
        {
            ShowNotice(_language.Get("photo.error"), false);
            return;
        }

        if (_character != null)
            Destroy(_character);
        _movement = null!;
        _presetController = null!;
        CreateCharacter();
        _ui?.AttachWardrobe(_wardrobe);
        _ui?.AttachCharacterPresets(_presetController);
        SetupPetFollow(_petAssembly?.ActiveInstance);
        Persist();
        ShowNotice(_language.Get("hud.saved"), true);
    }

    private void ChangeRoomStyle()
    {
        var floor = _worldRoot.Find("Floor")?.GetComponent<Renderer>();
        _save.activeRoomId = _save.activeRoomId == "room.sunny" ? "room.cozy" : "room.sunny";
        if (floor != null)
            floor.sharedMaterial = CreateMaterial(_save.activeRoomId == "room.sunny" ? new Color(0.16f, 0.12f, 0.25f) : new Color(0.10f, 0.22f, 0.25f));
        _furniture?.SetRoom(_save.activeRoomId);
        Persist();
        _ui?.SetRoomName(_language.Get(_save.activeRoomId == "room.cozy" ? "room.cozy" : "room.sunny"));
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
        _ui?.SetRoomName(_language.Get(_save.activeRoomId == "room.cozy" ? "room.cozy" : "room.sunny"));
        ShowNotice(_language.Get("hud.saved"), true);
    }

    private void ShowNotice(string message, bool success)
    {
        _ui?.ShowNotice(message, success);
    }

    private bool IsNewWorldPosition()
    {
        var position = _save.playerWorld?.position;
        if (position == null || !Mathf.Approximately(position.x, 0f) || !Mathf.Approximately(position.y, 0f) || !Mathf.Approximately(position.z, 0f))
            return false;
        return !(_save.rooms3D ?? Array.Empty<RoomLayoutData>())
            .Any(room => room?.placements is { Length: > 0 });
    }

    private void OnUiModeChanged(AlbaWorldUiMode mode) => _movement?.SetInputEnabled(mode == AlbaWorldUiMode.Casa);

    private void Persist()
    {
        if (_save == null || _saveService == null)
            return;

        _movement?.SavePosition();
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
