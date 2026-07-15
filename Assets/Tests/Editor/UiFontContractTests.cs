#if UNITY_EDITOR
using System.Linq;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AlbaWorld.Tests.Editor;

public sealed class UiFontContractTests
{
    [Test]
    public void GeneratedLabelsAlwaysUseAnAvailableRuntimeFont()
    {
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(catalog, Is.Not.Null);

        var root = new GameObject("UiFontContractRoot");
        var furnitureRoot = new GameObject("UiFontContractFurniture");
        try
        {
            var furniture = furnitureRoot.AddComponent<RoomFurnitureController>();
            furniture.Initialize(catalog!, furnitureRoot.transform, new GameSaveData(), new MemorySaveService());
            var ui = root.AddComponent<AlbaWorldUiController>();
            ui.Initialize(new LanguageService("pt-BR"), furniture, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp);

            var labels = root.GetComponentsInChildren<Text>(true);
            Assert.That(labels, Is.Not.Empty);
            Assert.That(labels.All(label => label.font != null), Is.True,
                "Every runtime label needs a built-in font; a missing font renders only the button backgrounds.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(furnitureRoot);
        }
    }

    private static void NoOp() { }
    private static void NoOp(string _) { }
    private static void NoOp(float _) { }

    private sealed class MemorySaveService : ISaveService
    {
        public GameSaveData Load() => new();
        public void Save(GameSaveData data) { }
    }
}
#endif
