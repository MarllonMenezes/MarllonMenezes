using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlbaWorld.Catalog;

public interface ICharacterPresetCatalog
{
    CharacterPresetDefinition? Get(string id);
    IEnumerable<CharacterPresetDefinition> All { get; }
}

[CreateAssetMenu(menuName = "Alba World/Character Preset Catalog", fileName = "CharacterPresetCatalog")]
public sealed class CharacterPresetCatalog : ScriptableObject, ICharacterPresetCatalog
{
    public List<CharacterPresetDefinition> presets = new();

    [NonSerialized] private Dictionary<string, CharacterPresetDefinition>? _byId;
    [NonSerialized] private int _sourceSignature;

    public IEnumerable<CharacterPresetDefinition> All => presets.Where(preset => preset != null);

    public CharacterPresetDefinition? Get(string id)
    {
        EnsureLookup();
        return id != null && _byId!.TryGetValue(id, out var preset) ? preset : null;
    }

    private void OnEnable() => _byId = null;
    private void OnValidate() => _byId = null;

    private void EnsureLookup()
    {
        var signature = ComputeSourceSignature();
        if (_byId != null && signature == _sourceSignature)
            return;

        var rebuilt = new Dictionary<string, CharacterPresetDefinition>(StringComparer.Ordinal);
        foreach (var preset in presets)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.presetId))
                continue;
            rebuilt.TryAdd(preset.presetId, preset);
        }

        _byId = rebuilt;
        _sourceSignature = signature;
    }

    private int ComputeSourceSignature()
    {
        unchecked
        {
            var signature = presets.Count;
            foreach (var preset in presets)
            {
                signature = signature * 397 ^ (preset == null ? 0 : preset.GetInstanceID());
                signature = signature * 397 ^ (preset == null ? 0 : StringComparer.Ordinal.GetHashCode(preset.presetId));
            }

            return signature;
        }
    }

#if UNITY_EDITOR
    public static CharacterPresetCatalog TestOnly(params string[] ids)
    {
        var catalog = CreateInstance<CharacterPresetCatalog>();
        foreach (var id in ids ?? Array.Empty<string>())
            catalog.presets.Add(CharacterPresetDefinition.TestOnly(id));
        return catalog;
    }
#endif
}
