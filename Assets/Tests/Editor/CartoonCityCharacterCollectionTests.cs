#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using AlbaWorld.Catalog;
using UnityEditor;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class CartoonCityCharacterCollectionTests
{
    [Test]
    public void FreeCatalogContainsAllImportedCharacterPresets()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CharacterPresetCatalog>("Assets/Resources/Data/CartoonCityCharacterPresets.asset");
        Assert.That(catalog, Is.Not.Null);

        var presets = catalog!.All.ToArray();
        Assert.That(presets.Length, Is.GreaterThanOrEqualTo(16));
        Assert.That(presets.Select(preset => preset.presetId).Distinct().Count(), Is.EqualTo(presets.Length));
        Assert.That(presets.All(preset => preset.free), Is.True);
        Assert.That(presets.All(preset => preset.prefab != null), Is.True);
        Assert.That(presets.All(preset => preset.compatibleAccessoryIds != null), Is.True);
        foreach (var preset in presets)
        {
            var renderers = preset.prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, preset.presetId);
            Assert.That(preset.prefab.GetComponentsInChildren<Camera>(true), Is.Empty, preset.presetId);
            Assert.That(preset.prefab.GetComponentsInChildren<Light>(true), Is.Empty, preset.presetId);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            Assert.That(bounds.size.y, Is.GreaterThan(0.25f), preset.presetId);
        }
    }
}
#endif
