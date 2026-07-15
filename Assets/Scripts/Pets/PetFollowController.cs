using UnityEngine;

namespace AlbaWorld.Pets;

/// <summary>Moves a visual-only pet toward a target anchor without physics or networking.</summary>
[DisallowMultipleComponent]
public sealed class PetFollowController : MonoBehaviour
{
    [SerializeField] private Transform? followTarget;
    [SerializeField] private Vector3 followOffset = new(0f, 0f, -1.25f);
    [SerializeField, Min(0f)] private float followSpeed = 4f;
    [SerializeField, Min(0f)] private float turnSpeed = 12f;
    [SerializeField] private float floorHeight;
    [SerializeField] private bool followEnabled = true;

    public bool FollowEnabled
    {
        get => followEnabled;
        set => followEnabled = value;
    }

    public Transform? FollowTarget
    {
        get => followTarget;
        set => followTarget = value;
    }

    public Vector3 FollowOffset
    {
        get => followOffset;
        set => followOffset = value;
    }

    public float FollowSpeed
    {
        get => followSpeed;
        set => followSpeed = Mathf.Max(0f, value);
    }

    public float TurnSpeed
    {
        get => turnSpeed;
        set => turnSpeed = Mathf.Max(0f, value);
    }

    public float FloorHeight
    {
        get => floorHeight;
        set => floorHeight = value;
    }

    private void LateUpdate()
    {
        if (!followEnabled || followTarget == null)
            return;

        var current = transform.position;
        var desired = followTarget.TransformPoint(followOffset);
        // Pets stay grounded even when the anchor has a different vertical position.
        desired.y = floorHeight;
        var next = Vector3.MoveTowards(current, desired, followSpeed * Time.deltaTime);
        next.y = floorHeight;
        transform.position = next;

        var movement = next - current;
        movement.y = 0f;
        if (movement.sqrMagnitude <= 0.000001f)
            return;

        var heading = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;
        var angles = transform.eulerAngles;
        angles.y = Mathf.MoveTowardsAngle(angles.y, heading, turnSpeed * Time.deltaTime);
        // Preserve x/z tilt: the controller rotates only around the vertical axis.
        transform.eulerAngles = angles;
    }
}
