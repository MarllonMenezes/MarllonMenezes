using System;
using UnityEngine;

namespace AlbaWorld.Catalog;

[Serializable]
public sealed class CharacterPresetPalette
{
    public string paletteId = "default";
    public string displayKey = "character.palette.default";
    public Color skinTint = new(0.72f, 0.42f, 0.25f, 1f);
    public Color hairTint = new(0.12f, 0.06f, 0.03f, 1f);
    public Color outfitTint = new(0.98f, 0.35f, 0.63f, 1f);
    public Color shoesTint = new(0.98f, 0.55f, 0.15f, 1f);
}

[CreateAssetMenu(menuName = "Alba World/Character Preset", fileName = "CharacterPreset")]
public sealed class CharacterPresetDefinition : ScriptableObject
{
    public string presetId = "cartooncity.char.01";
    public string displayKey = "character.preset.cartooncity.01";
    public string sourceAsset = "Character_1_2_2.fbx";
    public GameObject prefab = null!;
    public bool free = true;
    public int sortOrder;
    public CharacterPresetPalette[] palettes = { new() };
    public string[] compatibleAccessoryIds = Array.Empty<string>();

    public CharacterPresetPalette DefaultPalette =>
        palettes != null && palettes.Length > 0 && palettes[0] != null ? palettes[0] : new CharacterPresetPalette();

#if UNITY_EDITOR
    public static CharacterPresetDefinition TestOnly(string id)
    {
        var definition = CreateInstance<CharacterPresetDefinition>();
        definition.presetId = id;
        return definition;
    }
#endif
}
