using System;
using System.Collections.Generic;
using System.Linq;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using AlbaWorld.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AlbaWorld.Runtime;

/// <summary>Owns furniture instances and bounded per-room layout persistence.</summary>
public sealed class RoomFurnitureController : MonoBehaviour
{
    public static Bounds DefaultWalkableBounds => new(
        new Vector3(0f, 0.22f, 0.25f),
        new Vector3(3.8f, 0.1f, 3.0f));

    [SerializeField] private float _minimumX = -4.4f;
    [SerializeField] private float _maximumX = 4.1f;
    [SerializeField] private float _minimumZ = -2.1f;
    [SerializeField] private float _maximumZ = 3.0f;
    [SerializeField] private float _floorY = 0.22f;

    private readonly Dictionary<string, GameObject> _instances = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FurniturePlacementData> _placements = new(StringComparer.Ordinal);
    private ItemCatalog3D _catalog = null!;
    private Transform _roomRoot = null!;
    private GameSaveData _save = null!;
    private ISaveService _saveService = null!;
    private string _activeRoomId = "room.sunny";
    private string _draggingId = string.Empty;
    private Vector3 _dragOffset;
    private Vector3 _lastValidLocalPosition;
    private FurniturePlacementData? _removedPlacement;
    private ItemVisual3D? _removedVisual;
    private float _undoExpiresAt;
    private GameObject? _selectionMarker;

    private static readonly Vector3[] PerimeterSlots =
    {
        new(-3.45f, 0.22f, 2.45f), new(-1.05f, 0.22f, 2.55f), new(1.15f, 0.22f, 2.55f),
        new(3.00f, 0.22f, 2.35f), new(3.55f, 0.22f, 0.55f), new(3.35f, 0.22f, -1.55f),
        new(1.15f, 0.22f, -1.70f), new(-1.25f, 0.22f, -1.70f), new(-3.20f, 0.22f, -1.55f),
        new(-3.85f, 0.22f, 0.35f)
    };

    public IReadOnlyList<FurniturePlacementData> ActivePlacements =>
        _placements.Values.OrderBy(placement => placement.instanceId, StringComparer.Ordinal).ToArray();

    public string ActiveRoomId => _activeRoomId;
    public string SelectedInstanceId { get; private set; } = string.Empty;
    public bool HasSelection => !string.IsNullOrWhiteSpace(SelectedInstanceId) && _instances.ContainsKey(SelectedInstanceId);
    public event Action<string>? SelectionChanged;

    public void Initialize(ItemCatalog3D catalog, Transform roomRoot, GameSaveData save, ISaveService saveService)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _roomRoot = roomRoot ?? throw new ArgumentNullException(nameof(roomRoot));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        SetRoom(string.IsNullOrWhiteSpace(save.activeRoomId) ? "room.sunny" : save.activeRoomId);
    }

    public void SetRoom(string roomId)
    {
        roomId = string.IsNullOrWhiteSpace(roomId) ? "room.sunny" : roomId;
        if (_roomRoot == null || _save == null)
            return;

        if (_instances.Count > 0)
            SaveActiveRoom();

        ClearInstances();
        _activeRoomId = roomId;
        _save.activeRoomId = roomId;
        LoadRoom(roomId);
        SaveActiveRoom();
    }

    public bool TryAdd(string itemId, Vector3 worldPosition)
    {
        var visual = GetFurnitureVisual(itemId);
        if (visual?.prefab == null)
            return false;

        var instanceId = $"{itemId}-{Guid.NewGuid():N}";
        var placement = new FurniturePlacementData
        {
            instanceId = instanceId,
            itemId = itemId,
            position = ToSerializable(ClampLocal(_roomRoot.InverseTransformPoint(worldPosition))),
            scale = new SerializableVector3(1f, 1f, 1f),
            yaw = 0f
        };

        var instance = InstantiatePlacement(visual, placement);
        if (instance == null)
            return false;

        if (!IsValidPlacement(instance, placement.itemId, instanceId))
        {
            Destroy(instance);
            return false;
        }

        _instances[instanceId] = instance;
        _placements[instanceId] = placement;
        SetSelected(instanceId);
        SaveActiveRoom();
        return true;
    }

    public bool TryAddToFirstFreeSlot(string itemId)
    {
        foreach (var slot in PerimeterSlots)
        {
            if (TryAdd(itemId, slot))
                return true;
        }

        return false;
    }

    public bool TrySelect(string instanceId)
    {
        if (!_instances.ContainsKey(instanceId))
            return false;

        SetSelected(instanceId);
        return true;
    }

    public bool TryMove(string instanceId, Vector3 worldPosition)
    {
        return Move(instanceId, worldPosition, true);
    }

    private bool Move(string instanceId, Vector3 worldPosition, bool save)
    {
        if (!_instances.TryGetValue(instanceId, out var instance) ||
            !_placements.TryGetValue(instanceId, out var placement))
            return false;

        var local = ClampLocal(_roomRoot.InverseTransformPoint(worldPosition));
        var previousPosition = instance.transform.localPosition;
        instance.transform.localPosition = local;
        if (!IsValidPlacement(instance, placement.itemId, instanceId))
        {
            instance.transform.localPosition = previousPosition;
            return false;
        }

        placement.position = ToSerializable(local);
        SetSelected(instanceId);
        if (save)
            SaveActiveRoom();
        return true;
    }

    public bool TryScale(string instanceId, float delta)
    {
        if (!_instances.TryGetValue(instanceId, out var instance) ||
            !_placements.TryGetValue(instanceId, out var placement))
            return false;

        var visual = GetFurnitureVisual(placement.itemId);
        if (visual == null)
            return false;

        var current = Mathf.Abs(placement.scale.x);
        var next = Mathf.Clamp(current + delta, visual.placement.minimumScale, visual.placement.maximumScale);
        var sign = Mathf.Sign(placement.scale.x);
        if (Mathf.Approximately(sign, 0f))
            sign = 1f;

        var previousScale = instance.transform.localScale;
        var previousPlacementScale = placement.scale;
        instance.transform.localScale = new Vector3(sign * next, next, next);
        if (!IsValidPlacement(instance, placement.itemId, instanceId))
        {
            instance.transform.localScale = previousScale;
            placement.scale = previousPlacementScale;
            return false;
        }

        placement.scale = new SerializableVector3(sign * next, next, next);
        SetSelected(instanceId);
        SaveActiveRoom();
        return true;
    }

    public bool TryMirror(string instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance) ||
            !_placements.TryGetValue(instanceId, out var placement))
            return false;

        var scale = placement.scale ?? new SerializableVector3(1f, 1f, 1f);
        var previousScale = instance.transform.localScale;
        var previousPlacementScale = new SerializableVector3(scale.x, scale.y, scale.z);
        scale.x = Mathf.Approximately(scale.x, 0f) ? -1f : -scale.x;
        instance.transform.localScale = new Vector3(scale.x, Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        if (!IsValidPlacement(instance, placement.itemId, instanceId))
        {
            instance.transform.localScale = previousScale;
            placement.scale = previousPlacementScale;
            return false;
        }

        placement.scale = scale;
        SetSelected(instanceId);
        SaveActiveRoom();
        return true;
    }

    public bool TryBringForward(string instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance))
            return false;

        instance.transform.SetAsLastSibling();
        SetSelected(instanceId);
        return true;
    }

    public bool TrySendBackward(string instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance))
            return false;

        instance.transform.SetAsFirstSibling();
        SetSelected(instanceId);
        return true;
    }

    public bool TryRemove(string instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance) || !_placements.TryGetValue(instanceId, out var placement))
            return false;

        _removedPlacement = ClonePlacement(placement);
        _removedVisual = GetFurnitureVisual(placement.itemId);
        _undoExpiresAt = Time.unscaledTime + 4f;
        _instances.Remove(instanceId);
        _placements.Remove(instanceId);
        if (instance != null)
            Destroy(instance);
        if (SelectedInstanceId == instanceId)
            SetSelected(string.Empty);
        SaveActiveRoom();
        return true;
    }

    public bool TryUndoRemove()
    {
        if (_removedPlacement == null || _removedVisual == null || Time.unscaledTime > _undoExpiresAt)
        {
            ClearUndo();
            return false;
        }

        if (_instances.ContainsKey(_removedPlacement.instanceId))
        {
            ClearUndo();
            return false;
        }

        var instance = InstantiatePlacement(_removedVisual, _removedPlacement);
        if (instance == null || !IsValidPlacement(instance, _removedPlacement.itemId, _removedPlacement.instanceId))
        {
            if (instance != null)
                Destroy(instance);
            return false;
        }

        _instances[_removedPlacement.instanceId] = instance;
        _placements[_removedPlacement.instanceId] = _removedPlacement;
        SetSelected(_removedPlacement.instanceId);
        SaveActiveRoom();
        ClearUndo();
        return true;
    }

    private ItemVisual3D? GetFurnitureVisual(string itemId)
    {
        var visual = _catalog.GetVisual(itemId);
        if (visual == null || visual.prefab == null)
            return null;
        return visual.definition.category is ItemCategory.Furniture or ItemCategory.Decor ? visual : null;
    }

    private GameObject? InstantiatePlacement(ItemVisual3D visual, FurniturePlacementData placement)
    {
        var visualRoot = Instantiate(visual.prefab, _roomRoot, false);
        if (visualRoot == null)
            return null;

        var instance = new GameObject(placement.instanceId);
        instance.transform.SetParent(_roomRoot, false);
        visualRoot.transform.SetParent(instance.transform, false);
        visualRoot.name = "Visual";

        var visualRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (visualRenderers.Length > 0)
        {
            var visualBounds = visualRenderers[0].bounds;
            foreach (var renderer in visualRenderers.Skip(1))
                visualBounds.Encapsulate(renderer.bounds);
            var worldOffset = visualBounds.center - visualRoot.transform.position;
            var parentOffset = visualRoot.transform.parent == null
                ? worldOffset
                : visualRoot.transform.parent.InverseTransformVector(worldOffset);
            visualRoot.transform.localPosition -= parentOffset;
        }

        instance.name = placement.instanceId;
        instance.transform.localPosition = ClampLocal(ToVector(placement.position));
        var scale = placement.scale ?? new SerializableVector3(1f, 1f, 1f);
        instance.transform.localScale = new Vector3(scale.x, Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        instance.transform.localRotation = Quaternion.Euler(0f, placement.yaw, 0f);
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            var collider = instance.GetComponent<BoxCollider>();
            if (collider == null)
                collider = instance.AddComponent<BoxCollider>();
            collider.center = instance.transform.InverseTransformPoint(bounds.center);
            var corners = BoundsCorners(bounds);
            var localBounds = new Bounds(instance.transform.InverseTransformPoint(corners[0]), Vector3.zero);
            foreach (var corner in corners.Skip(1))
                localBounds.Encapsulate(instance.transform.InverseTransformPoint(corner));
            collider.center = localBounds.center;
            collider.size = localBounds.size;
        }

        var handle = instance.GetComponent<FurnitureDragHandle>();
        if (handle == null)
            handle = instance.AddComponent<FurnitureDragHandle>();
        handle.Bind(this, placement.instanceId);
        var selectable = instance.GetComponent<WorldSelectable>();
        if (selectable == null)
            selectable = instance.AddComponent<WorldSelectable>();
        selectable.Configure(WorldSelectableKind.Furniture, placement.instanceId);
        return instance;
    }

    private void LoadRoom(string roomId)
    {
        var room = FindRoom(roomId);
        if (room == null)
            return;

        foreach (var placement in room.placements ?? Array.Empty<FurniturePlacementData>())
        {
            if (placement == null || _placements.ContainsKey(placement.instanceId))
                continue;

            var visual = GetFurnitureVisual(placement.itemId);
            if (visual == null)
                continue;

            placement.position ??= new SerializableVector3(0f, _floorY, 0f);
            placement.scale ??= new SerializableVector3(1f, 1f, 1f);
            placement.position = ToSerializable(ClampLocal(ToVector(placement.position)));
            var instance = InstantiatePlacement(visual, placement);
            if (instance == null || !IsValidPlacement(instance, placement.itemId, placement.instanceId))
            {
                if (instance != null)
                    Destroy(instance);
                instance = TryInstantiateAtFreeSlot(visual, placement);
            }

            if (instance == null)
                continue;

            _placements[placement.instanceId] = placement;
            _instances[placement.instanceId] = instance;
        }
    }

    private void Update()
    {
        if (_removedPlacement != null && Time.unscaledTime > _undoExpiresAt)
            ClearUndo();

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (!IsPointerOverUi(touch.fingerId))
                    BeginDrag(touch.position);
            }
            else if (touch.phase is TouchPhase.Moved or TouchPhase.Stationary)
            {
                DragToScreen(touch.position);
            }
            else if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled)
            {
                EndDrag();
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUi())
                BeginDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            DragToScreen(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        var ray = camera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out var hit))
            return;

        var id = hit.collider.GetComponentInParent<FurnitureDragHandle>()?.InstanceId;
        if (string.IsNullOrWhiteSpace(id) || !_instances.ContainsKey(id))
            return;

        var selectable = _instances[id].GetComponent<WorldSelectable>();
        if (selectable != null && !WorldSelectionContext.IsSelected(selectable))
        {
            WorldSelectionContext.Select(selectable);
            SetSelected(id);
            return;
        }
        if (WorldSelectionContext.WasSelectedThisFrame)
            return;

        if (!TryGetFloorPoint(ray, out var floorPoint))
            return;

        _draggingId = id;
        _dragOffset = _instances[id].transform.position - floorPoint;
        _lastValidLocalPosition = _instances[id].transform.localPosition;
        SetSelected(id);
    }

    private void DragToScreen(Vector2 screenPosition)
    {
        if (string.IsNullOrWhiteSpace(_draggingId))
            return;

        var camera = Camera.main;
        if (camera == null || !TryGetFloorPoint(camera.ScreenPointToRay(screenPosition), out var floorPoint))
            return;

        Move(_draggingId, floorPoint + _dragOffset, false);
    }

    private void EndDrag()
    {
        if (string.IsNullOrWhiteSpace(_draggingId))
            return;

        _draggingId = string.Empty;
        SaveActiveRoom();
    }

    private bool TryGetFloorPoint(Ray ray, out Vector3 point)
    {
        var plane = new Plane(Vector3.up, new Vector3(0f, _floorY, 0f));
        if (plane.Raycast(ray, out var distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = default;
        return false;
    }

    private static bool IsPointerOverUi(int fingerId = -1)
    {
        if (EventSystem.current == null)
            return false;
        return fingerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    private void SaveActiveRoom()
    {
        var room = FindRoom(_activeRoomId) ?? new RoomLayoutData { roomId = _activeRoomId };
        room.placements = ActivePlacements.ToArray();
        _save.rooms3D = (_save.rooms3D ?? Array.Empty<RoomLayoutData>())
            .Where(existing => existing != null && !string.Equals(existing.roomId, _activeRoomId, StringComparison.Ordinal))
            .Append(room)
            .ToArray();
        _save.activeRoomId = _activeRoomId;
        _saveService.Save(_save);
    }

    private RoomLayoutData? FindRoom(string roomId) =>
        (_save.rooms3D ?? Array.Empty<RoomLayoutData>())
        .FirstOrDefault(room => room != null && string.Equals(room.roomId, roomId, StringComparison.Ordinal));

    private void ClearInstances()
    {
        foreach (var instance in _instances.Values)
        {
            if (instance != null)
                Destroy(instance);
        }

        _instances.Clear();
        _placements.Clear();
        SetSelected(string.Empty);
        ClearUndo();
    }

    private Vector3 ClampLocal(Vector3 local)
    {
        local.x = Mathf.Clamp(local.x, _minimumX, _maximumX);
        local.y = _floorY;
        local.z = Mathf.Clamp(local.z, _minimumZ, _maximumZ);
        return local;
    }

    private static SerializableVector3 ToSerializable(Vector3 value) => new(value.x, value.y, value.z);

    private static Vector3 ToVector(SerializableVector3? value) =>
        value == null ? Vector3.zero : new Vector3(value.x, value.y, value.z);

    private GameObject? TryInstantiateAtFreeSlot(ItemVisual3D visual, FurniturePlacementData source)
    {
        foreach (var slot in PerimeterSlots)
        {
            source.position = ToSerializable(slot);
            var candidate = InstantiatePlacement(visual, source);
            if (candidate != null && IsValidPlacement(candidate, source.itemId, source.instanceId))
                return candidate;
            if (candidate != null)
                Destroy(candidate);
        }

        return null;
    }

    private bool IsValidPlacement(GameObject instance, string itemId, string instanceId)
    {
        Physics.SyncTransforms();
        var candidate = GetRoomLocalBounds(instance);
        var room = new Bounds(
            new Vector3((_minimumX + _maximumX) * 0.5f, _floorY, (_minimumZ + _maximumZ) * 0.5f),
            new Vector3(_maximumX - _minimumX + 2f, 10f, _maximumZ - _minimumZ + 3f));
        if (!ContainsXZ(room, candidate))
        {
            return false;
        }

        if (!AllowsWalkableZone(itemId) && DefaultWalkableBounds.Contains(candidate.center))
        {
            return false;
        }

        foreach (var pair in _instances)
        {
            if (pair.Key == instanceId || pair.Value == null)
                continue;
            var otherBounds = GetRoomLocalBounds(pair.Value);
            if (OverlapsXZ(candidate, otherBounds))
            {
                return false;
            }
        }

        return true;
    }

    private Bounds GetRoomLocalBounds(GameObject instance)
    {
        var collider = instance.GetComponent<BoxCollider>();
        if (collider != null)
        {
            return TransformBoundsToRoom(collider.bounds);
        }

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(instance.transform.localPosition, Vector3.one * 0.1f);

        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        return TransformBoundsToRoom(bounds);
    }

    private Bounds TransformBoundsToRoom(Bounds worldBounds)
    {
        var corners = new[]
        {
            new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z),
            new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z),
            new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z),
            new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z),
            new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z),
            new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z),
            new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z),
            new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z)
        };
        var local = _roomRoot.InverseTransformPoint(corners[0]);
        var bounds = new Bounds(local, Vector3.zero);
        foreach (var corner in corners.Skip(1))
            bounds.Encapsulate(_roomRoot.InverseTransformPoint(corner));
        return bounds;
    }

    private static bool ContainsXZ(Bounds outer, Bounds inner) =>
        inner.min.x >= outer.min.x && inner.max.x <= outer.max.x &&
        inner.min.z >= outer.min.z && inner.max.z <= outer.max.z;

    private static bool OverlapsXZ(Bounds first, Bounds second) =>
        first.min.x < second.max.x && first.max.x > second.min.x &&
        first.min.z < second.max.z && first.max.z > second.min.z;

    private static bool AllowsWalkableZone(string itemId) => string.Equals(itemId, "furniture.rug", StringComparison.Ordinal);

    private static Vector3[] BoundsCorners(Bounds bounds) => new[]
    {
        new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
        new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
        new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
        new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
        new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
        new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
        new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
        new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
    };

    private void SetSelected(string instanceId)
    {
        if (SelectedInstanceId == instanceId && (string.IsNullOrWhiteSpace(instanceId) || _selectionMarker != null))
            return;

        DestroySelectionMarker();
        SelectedInstanceId = instanceId ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(SelectedInstanceId) && _instances.TryGetValue(SelectedInstanceId, out var instance))
        {
            _selectionMarker = CreateSelectionMarker(instance);
            var selectable = instance.GetComponent<WorldSelectable>();
            if (selectable != null)
                WorldSelectionContext.Select(selectable);
        }
        else if (string.IsNullOrWhiteSpace(SelectedInstanceId) && WorldSelectionContext.Current?.Kind == WorldSelectableKind.Furniture)
        {
            WorldSelectionContext.Clear();
        }
        SelectionChanged?.Invoke(SelectedInstanceId);
    }

    private GameObject CreateSelectionMarker(GameObject instance)
    {
        var marker = new GameObject("Selection Ring", typeof(LineRenderer));
        marker.transform.SetParent(instance.transform, false);
        var line = marker.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 32;
        line.widthMultiplier = 0.035f;
        line.startColor = new Color(1f, 0.35f, 0.60f, 0.95f);
        line.endColor = line.startColor;
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
        if (shader != null)
            line.material = new Material(shader);
        var radius = 0.75f;
        var collider = instance.GetComponent<BoxCollider>();
        if (collider != null)
            radius = Mathf.Max(collider.size.x, collider.size.z) * 0.55f;
        for (var index = 0; index < line.positionCount; index++)
        {
            var angle = index * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0.02f, Mathf.Sin(angle) * radius));
        }

        return marker;
    }

    private void DestroySelectionMarker()
    {
        if (_selectionMarker == null)
            return;
        if (Application.isPlaying)
            Destroy(_selectionMarker);
        else
            DestroyImmediate(_selectionMarker);
        _selectionMarker = null;
    }

    private void ClearUndo()
    {
        _removedPlacement = null;
        _removedVisual = null;
        _undoExpiresAt = 0f;
    }

    private static FurniturePlacementData ClonePlacement(FurniturePlacementData source) => new()
    {
        instanceId = source.instanceId,
        itemId = source.itemId,
        position = source.position == null ? new SerializableVector3() : new SerializableVector3(source.position.x, source.position.y, source.position.z),
        scale = source.scale == null ? new SerializableVector3(1f, 1f, 1f) : new SerializableVector3(source.scale.x, source.scale.y, source.scale.z),
        yaw = source.yaw,
        supportInstanceId = source.supportInstanceId,
        supportPointId = source.supportPointId
    };
}

public sealed class FurnitureDragHandle : MonoBehaviour
{
    public string InstanceId { get; private set; } = string.Empty;

    public void Bind(RoomFurnitureController controller, string instanceId)
    {
        InstanceId = instanceId;
    }
}
