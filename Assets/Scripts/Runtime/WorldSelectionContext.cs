using System;
using System.Linq;
using UnityEngine;

namespace AlbaWorld.Runtime;

public enum WorldSelectableKind
{
    Character,
    Pet,
    Furniture
}

[DisallowMultipleComponent]
public sealed class WorldSelectable : MonoBehaviour
{
    [SerializeField] private WorldSelectableKind _kind;
    [SerializeField] private string _entityId = string.Empty;

    public WorldSelectableKind Kind => _kind;
    public string EntityId => _entityId;
    public bool IsSelected { get; private set; }
    public event Action<bool>? SelectionChanged;

    public void Configure(WorldSelectableKind kind, string entityId)
    {
        _kind = kind;
        _entityId = entityId ?? string.Empty;
    }

    internal void SetSelected(bool selected)
    {
        if (IsSelected == selected)
            return;
        IsSelected = selected;
        SelectionChanged?.Invoke(selected);
    }

    private void OnDestroy()
    {
        if (WorldSelectionContext.Current == this)
            WorldSelectionContext.Clear();
    }
}

public static class WorldSelectionContext
{
    public static WorldSelectable? Current { get; private set; }
    public static bool WasSelectedThisFrame => _selectionFrame == Time.frameCount;
    private static int _selectionFrame = -1;

    public static bool Select(WorldSelectable selectable)
    {
        if (selectable == null)
            return false;
        if (Current == selectable)
            return false;

        Current?.SetSelected(false);
        Current = selectable;
        Current.SetSelected(true);
        _selectionFrame = Time.frameCount;
        return true;
    }

    public static bool IsSelected(WorldSelectable selectable) => Current == selectable;

    public static bool TryFindSelectable(Ray ray, out WorldSelectable selectable)
    {
        var hits = Physics.RaycastAll(ray, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            .OrderBy(hit => hit.distance);
        foreach (var hit in hits)
        {
            var candidate = hit.collider.GetComponentInParent<WorldSelectable>();
            if (candidate == null)
                continue;
            selectable = candidate;
            Select(candidate);
            return true;
        }

        selectable = null!;
        return false;
    }

    public static void Clear()
    {
        Current?.SetSelected(false);
        Current = null;
        _selectionFrame = -1;
    }
}
