using System;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AlbaWorld.Pets;

/// <summary>Allows a selected pet to be placed manually and restores that choice offline.</summary>
[DisallowMultipleComponent]
public sealed class PetPlacementController : MonoBehaviour
{
    private Transform _pet = null!;
    private Transform _coordinateRoot = null!;
    private GameSaveData _save = null!;
    private ISaveService _saveService = null!;
    private PetFollowController _follow = null!;
    private Bounds _bounds;
    private float _floorY;
    private bool _dragging;
    private Vector3 _dragOffset;

    public void Initialize(
        Transform pet,
        GameSaveData save,
        ISaveService saveService,
        Bounds bounds,
        float floorY,
        PetFollowController follow,
        Transform? coordinateRoot = null)
    {
        _pet = pet ?? throw new ArgumentNullException(nameof(pet));
        _coordinateRoot = coordinateRoot ?? pet.parent ?? pet;
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        _bounds = bounds;
        _floorY = floorY;
        _follow = follow ?? throw new ArgumentNullException(nameof(follow));
        _follow.FollowEnabled = _save.pet.followCharacter;
        if (!_follow.FollowEnabled)
            RestoreManualPosition();
    }

    public bool SetManualPosition(Vector3 worldPosition)
    {
        if (_pet == null || _save?.pet == null)
            return false;

        var local = ClampLocal(_coordinateRoot.InverseTransformPoint(worldPosition));
        _pet.position = _coordinateRoot.TransformPoint(local);
        _follow.FollowEnabled = false;
        _save.pet.followCharacter = false;
        _save.pet.position = new SerializableVector3(local.x, local.y, local.z);
        _saveService.Save(_save);
        return true;
    }

    public void SetFollowMode(bool enabled)
    {
        _save.pet.followCharacter = enabled;
        _follow.FollowEnabled = enabled;
        if (!enabled)
            _save.pet.position = ToSerializable(_coordinateRoot.InverseTransformPoint(_pet.position));
        _saveService.Save(_save);
    }

    private void RestoreManualPosition()
    {
        var position = _save.pet.position ?? new SerializableVector3(1.45f, _floorY, 0.2f);
        var local = ClampLocal(new Vector3(position.x, position.y, position.z));
        _pet.position = _coordinateRoot.TransformPoint(local);
    }

    private void Update()
    {
        if (_pet == null || IsPointerOverUi())
            return;

        if (TryReadPointer(out var phase, out var screenPosition))
        {
            var camera = Camera.main;
            if (camera == null)
                return;
            var ray = camera.ScreenPointToRay(screenPosition);
            if (phase == PointerPhase.Began)
            {
                if (!WorldSelectionContext.TryFindSelectable(ray, out var selectable) || selectable.gameObject != _pet.gameObject)
                    return;
                if (WorldSelectionContext.WasSelectedThisFrame)
                    return;
                if (!TryGetFloorPoint(ray, out var floorPoint))
                    return;
                _dragging = true;
                _dragOffset = _pet.position - floorPoint;
                SetFollowMode(false);
            }
            else if (_dragging && phase is PointerPhase.Moved or PointerPhase.Stationary)
            {
                if (TryGetFloorPoint(ray, out var floorPoint))
                    SetManualPosition(floorPoint + _dragOffset);
            }
            else if (_dragging && phase is PointerPhase.Ended or PointerPhase.Canceled)
            {
                _dragging = false;
                SetManualPosition(_pet.position);
            }
        }
    }

    private bool TryGetFloorPoint(Ray ray, out Vector3 point)
    {
        var plane = new Plane(Vector3.up, _coordinateRoot.TransformPoint(new Vector3(0f, _floorY, 0f)));
        if (plane.Raycast(ray, out var distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = default;
        return false;
    }

    private Vector3 ClampLocal(Vector3 local)
    {
        local.x = Mathf.Clamp(local.x, _bounds.min.x, _bounds.max.x);
        local.y = _floorY;
        local.z = Mathf.Clamp(local.z, _bounds.min.z, _bounds.max.z);
        return local;
    }

    private static SerializableVector3 ToSerializable(Vector3 value) => new(value.x, value.y, value.z);

    private static bool IsPointerOverUi() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    private static bool TryReadPointer(out PointerPhase phase, out Vector2 position)
    {
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            phase = touch.phase switch
            {
                TouchPhase.Began => PointerPhase.Began,
                TouchPhase.Moved => PointerPhase.Moved,
                TouchPhase.Stationary => PointerPhase.Stationary,
                TouchPhase.Ended => PointerPhase.Ended,
                _ => PointerPhase.Canceled
            };
            position = touch.position;
            return true;
        }

        phase = Input.GetMouseButtonDown(0) ? PointerPhase.Began :
            Input.GetMouseButton(0) ? PointerPhase.Moved :
            Input.GetMouseButtonUp(0) ? PointerPhase.Ended : PointerPhase.None;
        position = Input.mousePosition;
        return phase != PointerPhase.None;
    }

    private enum PointerPhase
    {
        None,
        Began,
        Moved,
        Stationary,
        Ended,
        Canceled
    }
}
