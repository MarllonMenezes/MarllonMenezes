using System;
using System.Collections.Generic;
using System.Linq;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using AlbaWorld.Game;
using UnityEngine;

namespace AlbaWorld.Runtime;

/// <summary>Applies local catalog loadouts to the existing character renderers.</summary>
[DisallowMultipleComponent]
public sealed class CharacterWardrobeController : MonoBehaviour
{
    private ItemCatalog3D _catalog = null!;
    private Transform _character = null!;
    private GameSaveData _save = null!;
    private ISaveService _saveService = null!;
    private CharacterPresetController? _presetController;

    public ItemCategory SelectedCategory { get; private set; } = ItemCategory.Skin;
    public event Action<ItemCategory>? SelectionChanged;
    public event Action<string>? NoticeRequested;

    public void Initialize(ItemCatalog3D catalog, Transform character, GameSaveData save, ISaveService saveService)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _character = character ?? throw new ArgumentNullException(nameof(character));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        ApplySavedVisuals();
    }

    public void BindCharacter(Transform character)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
        ApplySavedVisuals();
    }

    public void AttachPresetController(CharacterPresetController presetController)
    {
        _presetController = presetController;
        ApplySavedVisuals();
    }

    public void SelectCategory(ItemCategory category)
    {
        if (!IsWardrobeCategory(category))
            return;
        SelectedCategory = category;
        SelectionChanged?.Invoke(category);
    }

    public IEnumerable<ItemVisual3D> ItemsForCategory(ItemCategory category) =>
        IsWardrobeCategory(category)
            ? _catalog.ByCategory(category).Where(visual => visual != null && visual.definition != null)
            : Enumerable.Empty<ItemVisual3D>();

    public bool CanUse(string itemId)
    {
        var visual = _catalog.GetVisual(itemId);
        if (visual?.definition == null || !IsWardrobeCategory(visual.definition.category))
            return false;
        if (visual.definition.category == ItemCategory.HumanAccessory &&
            _presetController != null && !_presetController.SupportsAccessory(itemId))
            return false;
        if (!IsBodyCompatible(visual.compatibleBodies))
            return false;
        return visual.definition.free || (_save.unlockedItemIds ?? Array.Empty<string>()).Contains(itemId, StringComparer.Ordinal);
    }

    public bool TryApply(string itemId)
    {
        var visual = _catalog.GetVisual(itemId);
        if (visual?.definition == null || !CanUse(itemId))
            return false;

        if (!SetLoadoutId(visual.definition.category, itemId))
            return false;

        SelectedCategory = visual.definition.category;
        ApplyVisual(visual.definition.category, visual.definition.tint);
        SaveLoadout();
        SelectionChanged?.Invoke(SelectedCategory);
        return true;
    }

    public void SaveLoadout()
    {
        if (_save == null || _saveService == null)
            return;
        _save.selectedSkinId = _save.character.skinId;
        _save.selectedHairId = _save.character.hairId;
        _save.selectedOutfitId = _save.character.outfitId;
        _save.selectedShoesId = _save.character.shoesId;
        _save.selectedAccessoryId = _save.character.accessoryIds?.FirstOrDefault() ?? string.Empty;
        _saveService.Save(_save);
    }

    private void ApplySavedVisuals()
    {
        if (_character == null || _save?.character == null)
            return;

        ApplySaved(ItemCategory.Skin, _save.character.skinId);
        ApplySaved(ItemCategory.Hair, _save.character.hairId);
        ApplySaved(ItemCategory.Outfit, _save.character.outfitId);
        ApplySaved(ItemCategory.Shoes, _save.character.shoesId);
        ApplySaved(ItemCategory.HumanAccessory, _save.character.accessoryIds?.FirstOrDefault() ?? string.Empty);
    }

    private void ApplySaved(ItemCategory category, string itemId)
    {
        var visual = _catalog.GetVisual(itemId);
        if (visual?.definition == null || visual.definition.category != category)
            return;
        ApplyVisual(category, visual.definition.tint);
    }

    private bool SetLoadoutId(ItemCategory category, string itemId)
    {
        switch (category)
        {
            case ItemCategory.Skin:
                _save.character.skinId = itemId;
                return true;
            case ItemCategory.Hair:
                _save.character.hairId = itemId;
                return true;
            case ItemCategory.Outfit:
                _save.character.outfitId = itemId;
                return true;
            case ItemCategory.Shoes:
                _save.character.shoesId = itemId;
                return true;
            case ItemCategory.HumanAccessory:
                _save.character.accessoryIds = new[] { itemId };
                return true;
            default:
                return false;
        }
    }

    private void ApplyVisual(ItemCategory category, Color tint)
    {
        if (_character == null)
            return;

        if (_presetController != null)
        {
            _presetController.ApplyCategoryTint(category, tint);
            return;
        }

        var prefix = string.Equals(_save.character.bodyId, "body.boy", StringComparison.Ordinal) ? "Boy" : "Girl";
        var names = category switch
        {
            ItemCategory.Skin => new[] { prefix + "Head", prefix + "Neck", prefix + "ArmLeft", prefix + "ArmRight", prefix + "LegLeft", prefix + "LegRight" },
            ItemCategory.Hair => new[] { prefix + "Hair" },
            ItemCategory.Outfit => new[] { prefix + "BodySurface" },
            ItemCategory.Shoes => new[] { "Foot.L", "Foot.R" },
            ItemCategory.HumanAccessory => new[] { prefix + "HairBow" },
            _ => Array.Empty<string>()
        };

        var renderers = _character.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => names.Contains(renderer.gameObject.name, StringComparer.Ordinal))
            .ToArray();
        if (renderers.Length == 0)
        {
            NoticeRequested?.Invoke(category.ToString());
            return;
        }

        foreach (var renderer in renderers)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            block.SetColor("_Color", tint);
            renderer.SetPropertyBlock(block);
        }
    }

    private bool IsBodyCompatible(BodyCompatibility compatibility)
    {
        var body = string.Equals(_save.character.bodyId, "body.boy", StringComparison.Ordinal)
            ? BodyCompatibility.Boy
            : BodyCompatibility.Girl;
        return (compatibility & body) != 0;
    }

    private static bool IsWardrobeCategory(ItemCategory category) => category is
        ItemCategory.Skin or ItemCategory.Hair or ItemCategory.Outfit or ItemCategory.Shoes or ItemCategory.HumanAccessory;
}
