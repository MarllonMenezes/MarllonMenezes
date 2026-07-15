using System;
using UnityEngine;

namespace AlbaWorld.Catalog
{
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
}
