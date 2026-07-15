using System;
using AlbaWorld.Core;
using AlbaWorld.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AlbaWorld.Runtime;

/// <summary>Moves the character through the open centre of the room without physics.</summary>
[DisallowMultipleComponent]
public sealed class CharacterMovementController : MonoBehaviour
{
    private Transform _character = null!;
    private GameSaveData _save = null!;
    private ISaveService _saveService = null!;
    private Bounds _walkableBounds;
    private float _floorY;
    private float _speed = 3.4f;
    private Vector3 _destination;
    private bool _hasDestination;
    private bool _inputEnabled = true;

    public bool IsMoving => _hasDestination && Vector3.Distance(_character.localPosition, _destination) > 0.01f;

    public void Initialize(Transform character, GameSaveData save, ISaveService saveService, Bounds walkableBounds, float floorY, float speed = 3.4f)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        _walkableBounds = walkableBounds;
        _floorY = floorY;
        _speed = Mathf.Max(0.1f, speed);
        RestorePosition();
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (!enabled)
            _hasDestination = false;
    }

    public void SetDestination(Vector3 worldPosition)
    {
        if (!_inputEnabled)
            return;

        var local = _character.parent == null
            ? worldPosition
            : _character.parent.InverseTransformPoint(worldPosition);
        _destination = ClampPosition(local);
        _hasDestination = true;
        if (!IsMoving)
            SavePosition();
    }

    public void RestorePosition()
    {
        var saved = _save.playerWorld?.position;
        var local = saved == null ? Vector3.zero : new Vector3(saved.x, saved.y, saved.z);
        local = ClampPosition(local);
        _character.localPosition = local;
        _destination = local;
        _hasDestination = false;
    }

    public void SavePosition()
    {
        if (_character == null || _save == null || _saveService == null)
            return;

        var local = ClampPosition(_character.localPosition);
        _character.localPosition = local;
        _save.playerWorld ??= new PlayerWorldStateData();
        _save.playerWorld.position = new SerializableVector3(local.x, local.y, local.z);
        _save.playerWorld.yaw = _character.localEulerAngles.y;
        _saveService.Save(_save);
    }

    private void Update()
    {
        if (_character == null)
            return;

        if (_inputEnabled && !IsPointerOverUi())
        {
            ReadKeyboardDestination();
            ReadPointerDestination();
        }

        if (!_inputEnabled || !_hasDestination)
            return;

        var current = _character.localPosition;
        var next = Vector3.MoveTowards(current, _destination, _speed * Time.deltaTime);
        next = ClampPosition(next);
        _character.localPosition = next;

        var direction = _destination - current;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            _character.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (Vector3.Distance(next, _destination) <= 0.01f)
        {
            _character.localPosition = _destination;
            _hasDestination = false;
            SavePosition();
        }
    }

    private void ReadKeyboardDestination()
    {
        var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude <= 0.0001f)
            return;

        var next = _character.localPosition + input.normalized * (_speed * 0.12f);
        _destination = ClampPosition(next);
        _hasDestination = true;
    }

    private void ReadPointerDestination()
    {
        Vector2 screenPosition;
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;
            screenPosition = touch.position;
        }
        else
        {
            if (!Input.GetMouseButtonDown(0))
                return;
            screenPosition = Input.mousePosition;
        }

        var camera = Camera.main;
        if (camera == null)
            return;

        var ray = camera.ScreenPointToRay(screenPosition);
        var plane = new Plane(Vector3.up, new Vector3(0f, _floorY, 0f));
        if (!plane.Raycast(ray, out var distance))
            return;

        SetDestination(ray.GetPoint(distance));
    }

    private static bool IsPointerOverUi() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    private Vector3 ClampPosition(Vector3 local)
    {
        local.x = Mathf.Clamp(local.x, _walkableBounds.min.x, _walkableBounds.max.x);
        local.y = _floorY;
        local.z = Mathf.Clamp(local.z, _walkableBounds.min.z, _walkableBounds.max.z);
        return local;
    }
}
