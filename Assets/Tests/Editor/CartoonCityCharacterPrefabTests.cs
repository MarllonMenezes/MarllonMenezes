#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using AlbaWorld.Catalog;
using UnityEditor;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class CartoonCityCharacterPrefabTests
{
    private const string CatalogPath = "Assets/Resources/Data/CartoonCityCharacterPresets.asset";
    private const string PrefabPath = "Assets/Art3D/Characters/Prefabs/CartoonCityChar01.prefab";

    [Test]
    public void PilotPresetAndPrefabAreDeterministicAndRenderable()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CharacterPresetCatalog>(CatalogPath);
        Assert.That(catalog, Is.Not.Null, "Cartoon City preset catalog is missing");
        var definition = catalog!.Get("cartooncity.char.01");
        Assert.That(definition, Is.Not.Null);
        Assert.That(definition!.prefab, Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)));
        Assert.That(definition.sourceAsset, Is.EqualTo("Character_1_2_2.fbx"));
        Assert.That(definition.free, Is.True);

        var prefab = definition.prefab;
        Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(prefab.GetComponentsInChildren<Camera>(true), Is.Empty);
        Assert.That(prefab.GetComponentsInChildren<Light>(true), Is.Empty);

        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1))
            bounds.Encapsulate(renderer.bounds);
        Assert.That(IsFinite(bounds.center) && IsFinite(bounds.size), Is.True);
        Assert.That(bounds.size.y, Is.GreaterThan(0.25f));
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) &&
        value.sqrMagnitude > 0.0001f;
}
#endif
