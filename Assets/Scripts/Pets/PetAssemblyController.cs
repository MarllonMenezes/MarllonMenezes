using System;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using UnityEngine;

namespace AlbaWorld.Pets;

/// <summary>
/// Resolves a pet loadout into one visual instance below a controlled mount.
/// </summary>
[DisallowMultipleComponent]
public sealed class PetAssemblyController : MonoBehaviour
{
    private const string DefaultPetId = "pet.cat";

    [SerializeField] private Transform? mount;

    private IItemCatalog3D? _catalog;
    private GameObject? _activeInstance;
    private string _activePetId = string.Empty;

    /// <summary>Raised after a new instance has been created and loadout hooks invoked.</summary>
    public event Action<GameObject, PetLoadoutData>? PetApplied;

    public string ActivePetId => _activePetId;

    public GameObject? ActiveInstance => _activeInstance;

    /// <summary>Connects this controller to the shared visual catalog and instance mount.</summary>
    public void Initialize(IItemCatalog3D catalog, Transform instanceMount)
    {
        _catalog = catalog;
        mount = instanceMount;
    }

    /// <summary>Compatibility alias for prefab/bootstrap wiring.</summary>
    public void Configure(IItemCatalog3D catalog, Transform instanceMount) => Initialize(catalog, instanceMount);

    /// <summary>
    /// Applies a pet loadout atomically. Invalid selections preserve an existing valid
    /// instance; before the first selection they fall back to the default cat visual.
    /// </summary>
    public bool TryApply(PetLoadoutData loadout)
    {
        if (loadout == null || _catalog == null)
            return false;

        var requestedId = string.IsNullOrWhiteSpace(loadout.petId) ? DefaultPetId : loadout.petId;
        var visual = ResolveVisual(requestedId);
        var usedFallback = false;
        if (!IsUsable(visual))
        {
            // Never replace a valid active pet because a later request is bad.
            if (_activeInstance != null)
                return false;

            requestedId = DefaultPetId;
            visual = ResolveVisual(requestedId);
            usedFallback = true;
            if (!IsUsable(visual))
                return false;
        }

        var parent = mount != null ? mount : transform;
        GameObject? instance;
        try
        {
            instance = UnityEngine.Object.Instantiate(visual!.prefab, parent, false);
        }
        catch (Exception)
        {
            return false;
        }

        if (instance == null)
            return false;

        instance.name = requestedId;
        ApplyHooks(instance, loadout);

        // Commit only after Instantiate and hooks have succeeded. The old instance is
        // owned by this controller and is the only object that may be destroyed here.
        var previous = _activeInstance;
        _activeInstance = instance;
        _activePetId = requestedId;
        if (previous != null)
            UnityEngine.Object.Destroy(previous);

        PetApplied?.Invoke(instance, loadout);
        return !usedFallback || requestedId == loadout.petId;
    }

    private ItemVisual3D? ResolveVisual(string id) => _catalog?.GetVisual(id);

    private static bool IsUsable(ItemVisual3D? visual) => visual != null && visual.prefab != null;

    private static void ApplyHooks(GameObject instance, PetLoadoutData loadout)
    {
        // Prefabs can opt into these hooks without coupling the controller to a specific
        // colour/accessory implementation. Empty loadouts remain valid and are no-ops.
        instance.BroadcastMessage("ApplyPetLoadout", loadout, SendMessageOptions.DontRequireReceiver);
        instance.BroadcastMessage("ApplyPetColor", loadout.colorId, SendMessageOptions.DontRequireReceiver);
        instance.BroadcastMessage("ApplyPetAccessories", loadout.accessoryIds, SendMessageOptions.DontRequireReceiver);
    }
}
