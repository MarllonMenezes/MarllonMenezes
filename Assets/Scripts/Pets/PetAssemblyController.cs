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
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorProperty = Shader.PropertyToID("_Color");

    [SerializeField] private Transform? mount;

    private IItemCatalog3D? _catalog;
    private GameObject? _activeInstance;
    private string _activePetId = string.Empty;

    /// <summary>Raised after a new instance has been created and loadout hooks invoked.</summary>
    public event Action<GameObject, PetLoadoutData>? PetApplied;

    public string ActivePetId => _activePetId;

    public GameObject? ActiveInstance => _activeInstance;

    /// <summary>
    /// Accessory IDs remain part of the persisted loadout and are broadcast to any
    /// approved consumer, but no accessory geometry is rendered until matching assets
    /// are approved. This explicit status prevents placeholder art from shipping.
    /// </summary>
    public static bool AccessoryRenderingDeferred => true;

    /// <summary>Connects this controller to the shared visual catalog and instance mount.</summary>
    public void Initialize(IItemCatalog3D catalog, Transform instanceMount)
    {
        _catalog = catalog;
        mount = instanceMount;
    }

    /// <summary>Compatibility alias for prefab/bootstrap wiring.</summary>
    public void Configure(IItemCatalog3D catalog, Transform instanceMount) => Initialize(catalog, instanceMount);

    /// <summary>Returns whether the catalog has a usable prefab for the requested pet ID.</summary>
    public bool HasUsablePet(string petId) => IsUsable(ResolveVisual(petId));

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
        try
        {
            ApplyHooks(instance, loadout);
        }
        catch (Exception)
        {
            UnityEngine.Object.Destroy(instance);
            return false;
        }

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
        // Apply colour directly on each renderer with a property block so shared
        // materials are never duplicated or mutated. Empty/default loadouts remain valid.
        ApplyPetColor(instance, loadout.colorId);

        // Prefabs can opt into these hooks without coupling the controller to a specific
        // accessory implementation. Accessory IDs intentionally persist and broadcast,
        // while rendering stays deferred until approved 3D assets are available.
        instance.BroadcastMessage("ApplyPetLoadout", loadout, SendMessageOptions.DontRequireReceiver);
        instance.BroadcastMessage("ApplyPetColor", loadout.colorId, SendMessageOptions.DontRequireReceiver);
        instance.BroadcastMessage("ApplyPetAccessories", loadout.accessoryIds, SendMessageOptions.DontRequireReceiver);
    }

    private static void ApplyPetColor(GameObject instance, string colorId)
    {
        var multiplier = ResolveColorMultiplier(colorId);
        var properties = new MaterialPropertyBlock();
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            var sharedMaterial = renderer.sharedMaterial;
            var baseColor = Color.white;
            if (sharedMaterial != null)
            {
                if (sharedMaterial.HasProperty(BaseColorProperty))
                    baseColor = sharedMaterial.GetColor(BaseColorProperty);
                else if (sharedMaterial.HasProperty(LegacyColorProperty))
                    baseColor = sharedMaterial.GetColor(LegacyColorProperty);
            }

            var color = new Color(
                baseColor.r * multiplier.r,
                baseColor.g * multiplier.g,
                baseColor.b * multiplier.b,
                baseColor.a * multiplier.a);
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorProperty, color);
            // Built-in/legacy shaders use _Color rather than _BaseColor. Setting both
            // keeps the same shared-material path working across imported Kenney assets.
            properties.SetColor(LegacyColorProperty, color);
            renderer.SetPropertyBlock(properties);
            properties.Clear();
        }
    }

    private static Color ResolveColorMultiplier(string colorId)
    {
        if (string.Equals(colorId, "petcolor.cocoa", StringComparison.Ordinal))
            return new Color(0.72f, 0.46f, 0.28f, 1f);

        // petcolor.sunny and unknown future IDs intentionally use the identity colour
        // until a catalog colour has an approved runtime representation.
        return Color.white;
    }
}
