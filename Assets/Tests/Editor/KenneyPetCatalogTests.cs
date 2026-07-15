#if UNITY_EDITOR
using System;
using System.Reflection;
using AlbaWorld.Catalog;
using AlbaWorld.Editor;
using AlbaWorld.Game;
using AlbaWorld.Pets;
using AlbaWorld.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class KenneyPetCatalogTests
{
    [Test]
    public void KenneyCreditIsLocalizedInPortugueseAndEnglish()
    {
        Assert.That(LocalizationTestData.Has("pt-BR", "credits.kenney"), Is.True);
        Assert.That(LocalizationTestData.Has("en", "credits.kenney"), Is.True);

        var portuguese = new LanguageService("pt-BR");
        var english = new LanguageService("en");
        Assert.That(portuguese.Get("credits.kenney"), Does.Contain("Kenney").And.Contain("www.kenney.nl"));
        Assert.That(english.Get("credits.kenney"), Does.Contain("Kenney").And.Contain("www.kenney.nl"));
    }

    [Test]
    public void CatalogContainsEveryKenneyPetWithBothTranslations()
    {
        var catalog = LoadCatalogForTests();
        foreach (var id in KenneyPetIds.All)
        {
            var visual = catalog.GetVisual(id);
            Assert.That(visual, Is.Not.Null, id);
            Assert.That(visual!.definition.category, Is.EqualTo(ItemCategory.Pet), id);
            Assert.That(visual.equipmentSlot, Is.EqualTo(EquipmentSlot.Pet), id);
            Assert.That(visual.prefab, Is.Not.Null, id);
            Assert.That(AssetDatabase.GetAssetPath(visual.prefab), Is.EqualTo(KenneyPetAssetSetup.PrefabPathFor(id)), id);
            Assert.That(LocalizationTestData.Has("pt-BR", visual.definition.displayKey), Is.True, id);
            Assert.That(LocalizationTestData.Has("en", visual.definition.displayKey), Is.True, id);
        }
    }

    [Test]
    public void BuilderFailsWhenRequiredKenneyPetPrefabIsMissing()
    {
        const string id = "pet.beaver";
        var prefabPath = KenneyPetAssetSetup.PrefabPathFor(id);
        var visualPath = $"Assets/Resources/Data/Visuals/{id}.asset";
        var visual = AssetDatabase.LoadAssetAtPath<ItemVisual3D>(visualPath);
        Assert.That(visual, Is.Not.Null, visualPath);
        var originalPrefab = visual!.prefab;
        Assert.That(originalPrefab, Is.Not.Null, prefabPath);

        var temporaryFolder = $"Assets/Tests/TempKenneyCatalogBuilder-{Guid.NewGuid():N}";
        var temporaryPrefabPath = $"{temporaryFolder}/beaver.prefab";
        var temporaryFolderName = temporaryFolder[(temporaryFolder.LastIndexOf('/') + 1)..];
        var moved = false;

        AssetDatabase.CreateFolder("Assets/Tests", temporaryFolderName);
        try
        {
            var moveError = AssetDatabase.MoveAsset(prefabPath, temporaryPrefabPath);
            Assert.That(moveError, Is.Empty, $"Could not move {prefabPath} for the negative test.");
            moved = true;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var invocation = Assert.Throws<TargetInvocationException>(InvokeCatalogBuilder);
            Assert.That(invocation!.InnerException, Is.TypeOf<BuildFailedException>());
            Assert.That(invocation.InnerException!.Message, Does.Contain("Missing Kenney pet prefab"));
            Assert.That(invocation.InnerException.Message, Does.Contain(id));
        }
        finally
        {
            if (moved)
            {
                var restoreError = AssetDatabase.MoveAsset(temporaryPrefabPath, prefabPath);
                if (!string.IsNullOrEmpty(restoreError))
                    throw new AssertionException($"Could not restore {prefabPath} after the negative test: {restoreError}");

                moved = false;
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            if (!moved)
                AssetDatabase.DeleteAsset(temporaryFolder);

            var restoredVisual = AssetDatabase.LoadAssetAtPath<ItemVisual3D>(visualPath);
            if (restoredVisual != null)
            {
                restoredVisual.prefab = originalPrefab;
                EditorUtility.SetDirty(restoredVisual);
                AssetDatabase.SaveAssetIfDirty(restoredVisual);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }

    private static ItemCatalog3D LoadCatalogForTests()
    {
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(catalog, Is.Not.Null, "Generated 3D catalog is missing.");
        return catalog!;
    }

    private static void InvokeCatalogBuilder()
    {
        var builderType = Type.GetType("AlbaWorld.Editor.AlbaCatalogBuilder, Assembly-CSharp-Editor");
        Assert.That(builderType, Is.Not.Null, "AlbaCatalogBuilder editor type is unavailable.");
        var build = builderType!.GetMethod("Build", BindingFlags.Public | BindingFlags.Static);
        Assert.That(build, Is.Not.Null, "AlbaCatalogBuilder.Build is unavailable.");
        build!.Invoke(null, null);
    }
}
#endif
