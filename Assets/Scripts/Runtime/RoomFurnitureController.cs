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

    public IReadOnlyList<FurniturePlacementData> ActivePlacements =>
        _placements.Values.OrderBy(placement => placement.instanceId, StringComparer.Ordinal).ToArray();

    public string ActiveRoomId => _activeRoomId;
    public string SelectedInstanceId { get; private set; } = string.Empty;

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

        _instances[instanceId] = instance;
        _placements[instanceId] = placement;
        SelectedInstanceId = instanceId;
        SaveActiveRoom();
        return true;
    }

    public bool TrySelect(string instanceId)
    {
        if (!_instances.ContainsKey(instanceId))
            return false;

        SelectedInstanceId = instanceId;
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
        instance.transform.localPosition = local;
        placement.position = ToSerializable(local);
        SelectedInstanceId = instanceId;
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

        instance.transform.localScale = new Vector3(sign * next, next, next);
        placement.scale = new SerializableVector3(sign * next, next, next);
        SelectedInstanceId = instanceId;
        SaveActiveRoom();
        return true;
    }

    public bool TryMirror(string instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance) ||
            !_placements.TryGetValue(instanceId, out var placement))
            return false;

        var scale = placement.scale ?? new SerializableVector3(1f, 1f, 1f);
        scale.x = Mathf.Approximately(scale.x, 0f) ? -1f : -scale.x;
        instance.transform.localScale = new Vector3(scale.x, Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        placement.scale = scale;
        SelectedInstanceId = instanceId;
        SaveActiveRoom();
        return true;
    }

    public bool TryBringForward(string instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance))
            return false;

        instance.transform.SetAsLastSibling();
        SelectedInstanceId = instanceId;
        return true;
    }

    public bool TrySendBackward(string instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance))
            return false;

        instance.transform.SetAsFirstSibling();
        SelectedInstanceId = instanceId;
        return true;
    }

    public bool TryRemove(string instanceId)
    {
        if (!_instances.Remove(instanceId, out var instance))
            return false;

        _placements.Remove(instanceId);
        if (instance != null)
            Destroy(instance);
        if (SelectedInstanceId == instanceId)
            SelectedInstanceId = string.Empty;
        SaveActiveRoom();
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
        var instance = Instantiate(visual.prefab, _roomRoot, false);
        if (instance == null)
            return null;

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
            var size = instance.transform.InverseTransformVector(bounds.size);
            collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        var handle = instance.GetComponent<FurnitureDragHandle>();
        if (handle == null)
            handle = instance.AddComponent<FurnitureDragHandle>();
        handle.Bind(this, placement.instanceId);
        return instance;
    }

    private void LoadRoom(string roomId)
    {
        var room = FindRoom(roomId);
        if (room == null)
            return;

        foreach (var placement in room.placements)
        {
            if (placement == null || _placements.ContainsKey(placement.instanceId))
                continue;

            var visual = GetFurnitureVisual(placement.itemId);
            if (visual == null)
                continue;

            var instance = InstantiatePlacement(visual, placement);
            if (instance == null)
                continue;

            _placements[placement.instanceId] = placement;
            _instances[placement.instanceId] = instance;
        }
    }

    private void Update()
    {
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

        if (!TryGetFloorPoint(ray, out var floorPoint))
            return;

        _draggingId = id;
        _dragOffset = _instances[id].transform.position - floorPoint;
        SelectedInstanceId = id;
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
        SelectedInstanceId = string.Empty;
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
}

public sealed class FurnitureDragHandle : MonoBehaviour
{
    public string InstanceId { get; private set; } = string.Empty;

    public void Bind(RoomFurnitureController controller, string instanceId)
    {
        InstanceId = instanceId;
    }
}
