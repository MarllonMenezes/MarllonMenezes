using System;
using System.Collections.Generic;
using System.Linq;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using AlbaWorld.Game;
using UnityEngine;

namespace AlbaWorld.Runtime;

/// <summary>Owns the selected Cartoon City preset and its local material palette.</summary>
[DisallowMultipleComponent]
public sealed class CharacterPresetController : MonoBehaviour
{
    private CharacterPresetCatalog _catalog = null!;
    private Transform _character = null!;
    private GameSaveData _save = null!;
    private ISaveService _saveService = null!;

    public string CurrentPresetId => _save?.character?.characterPresetId ?? string.Empty;
    public string CurrentPaletteId => _save?.character?.presetColorId ?? "default";
    public event Action<string>? PresetChanged;
    public event Action<string>? PaletteChanged;

    public void Initialize(CharacterPresetCatalog catalog, Transform character, GameSaveData save, ISaveService saveService)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _character = character ?? throw new ArgumentNullException(nameof(character));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        ApplySavedPalette();
    }

    public IEnumerable<CharacterPresetDefinition> Presets() =>
        _catalog == null ? Enumerable.Empty<CharacterPresetDefinition>() : _catalog.All.OrderBy(preset => preset.sortOrder);

    public IEnumerable<CharacterPresetPalette> Palettes()
    {
        var definition = _catalog?.Get(CurrentPresetId);
        return definition?.palettes?.Where(palette => palette != null) ?? Enumerable.Empty<CharacterPresetPalette>();
    }

    public bool TrySelect(string presetId)
    {
        var preset = _catalog?.Get(presetId);
        if (preset == null || (!preset.free && !IsUnlocked(presetId)))
            return false;

        _save.character.characterPresetId = preset.presetId;
        _save.character.presetColorId = "default";
        _save.character.bodyId = preset.presetId.EndsWith(".02", StringComparison.Ordinal)
            ? "body.boy"
            : "body.girl";
        ApplySavedPalette();
        _saveService.Save(_save);
        PresetChanged?.Invoke(preset.presetId);
        return true;
    }

    public bool TrySelectPalette(string paletteId)
    {
        var palette = Palettes().FirstOrDefault(candidate => string.Equals(candidate.paletteId, paletteId, StringComparison.Ordinal));
        if (palette == null)
            return false;

        _save.character.presetColorId = palette.paletteId;
        ApplyPalette(palette);
        _saveService.Save(_save);
        PaletteChanged?.Invoke(palette.paletteId);
        return true;
    }

    public void ApplyCategoryTint(ItemCategory category, Color tint)
    {
        if (_character == null)
            return;

        // The free Cartoon City pilot is a single skinned mesh. Applying the
        // chosen catalog tint to that mesh keeps the wardrobe responsive until
        // a preset supplies separate compatible accessory slots.
        ApplyTintToRenderers(tint);
    }

    public bool SupportsAccessory(string itemId)
    {
        var preset = _catalog?.Get(CurrentPresetId);
        return preset != null && (preset.compatibleAccessoryIds ?? Array.Empty<string>())
            .Contains(itemId, StringComparer.Ordinal);
    }

    private void ApplySavedPalette()
    {
        var preset = _catalog?.Get(CurrentPresetId);
        if (preset == null)
            return;

        var paletteId = string.IsNullOrWhiteSpace(CurrentPaletteId) ? "default" : CurrentPaletteId;
        var palette = preset.palettes?.FirstOrDefault(candidate => candidate != null && candidate.paletteId == paletteId)
                      ?? preset.DefaultPalette;
        if (string.Equals(palette.paletteId, "default", StringComparison.Ordinal))
        {
            ClearTintOverrides();
            return;
        }

        ApplyPalette(palette);
    }

    private void ApplyPalette(CharacterPresetPalette palette)
    {
        ApplyTintToRenderers(palette.outfitTint);
    }

    private void ApplyTintToRenderers(Color tint)
    {
        foreach (var renderer in _character.GetComponentsInChildren<Renderer>(true))
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            block.SetColor("_Color", tint);
            renderer.SetPropertyBlock(block);
        }
    }

    private void ClearTintOverrides()
    {
        foreach (var renderer in _character.GetComponentsInChildren<Renderer>(true))
            renderer.SetPropertyBlock(null);
    }

    private bool IsUnlocked(string itemId) =>
        (_save.unlockedItemIds ?? Array.Empty<string>()).Contains(itemId, StringComparer.Ordinal);
}
