#if UNITY_EDITOR
using System;
using System.Linq;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlbaWorld.Tests.Editor;

public sealed class ResponsiveLayoutContractTests
{
    private GameObject _root = null!;
    private GameObject _furnitureRoot = null!;

    [Test]
    public void GeneratedHouseAndDressLayoutsHaveReadableControls()
    {
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(catalog, Is.Not.Null);
        _root = new GameObject("ResponsiveLayoutTestRoot");
        _furnitureRoot = new GameObject("ResponsiveFurnitureRoot");
        var furniture = _furnitureRoot.AddComponent<RoomFurnitureController>();
        furniture.Initialize(catalog!, _furnitureRoot.transform, new GameSaveData(), new MemorySaveService());
        var ui = _root.AddComponent<AlbaWorldUiController>();
        ui.Initialize(new LanguageService("pt-BR"), furniture, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp);

        var canvas = _root.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Assert.That(_root.transform.Find("Safe Area/Casa Mode/Top Bar"), Is.Not.Null);
        Assert.That(_root.transform.Find("Safe Area/Casa Mode/House Dock/Dock Content"), Is.Not.Null);
        Assert.That(_root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "PetCard"), Is.False);
        AssertButtonsReadable(canvas!);

        ui.EnterDressMode();
        Assert.That(_root.transform.Find("Safe Area/Vestir Mode/Character Preview"), Is.Not.Null);
        Assert.That(_root.transform.Find("Safe Area/Vestir Mode/Wardrobe Panel/Wardrobe Content"), Is.Not.Null);
        AssertButtonsReadable(canvas!);

        UnityEngine.Object.DestroyImmediate(_root);
        UnityEngine.Object.DestroyImmediate(_furnitureRoot);
    }

    private static void AssertButtonsReadable(Canvas canvas)
    {
        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
        {
            var rect = button.GetComponent<RectTransform>();
            Assert.That(rect.anchorMin.x, Is.InRange(0f, 1f), button.name);
            Assert.That(rect.anchorMax.x, Is.InRange(0f, 1f), button.name);
            Assert.That(rect.anchorMin.y, Is.InRange(0f, 1f), button.name);
            Assert.That(rect.anchorMax.y, Is.InRange(0f, 1f), button.name);
            Assert.That(button.GetComponent<LayoutElement>(), Is.Not.Null, button.name);
            Assert.That(button.GetComponentInChildren<TMP_Text>(true), Is.Not.Null, button.name);
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
