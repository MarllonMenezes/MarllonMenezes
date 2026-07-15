#if UNITY_EDITOR
using NUnit.Framework;
using AlbaWorld.Runtime;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class WorldSelectionTests
{
    [SetUp]
    public void SetUp() => WorldSelectionContext.Clear();

    [TearDown]
    public void TearDown() => WorldSelectionContext.Clear();

    [Test]
    public void FirstClickSelectsEntityAndSecondClickCanManipulateIt()
    {
        var entity = new GameObject("Furniture");
        var selectable = entity.AddComponent<WorldSelectable>();
        try
        {
            Assert.That(WorldSelectionContext.Select(selectable), Is.True);
            Assert.That(WorldSelectionContext.Current, Is.SameAs(selectable));
            Assert.That(WorldSelectionContext.Select(selectable), Is.False);
            Assert.That(WorldSelectionContext.IsSelected(selectable), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(entity);
        }
    }

    [Test]
    public void SelectingAnotherEntityMovesSelectionAndClearsPrevious()
    {
        var first = new GameObject("Character");
        var second = new GameObject("Pet");
        var firstSelectable = first.AddComponent<WorldSelectable>();
        var secondSelectable = second.AddComponent<WorldSelectable>();
        try
        {
            WorldSelectionContext.Select(firstSelectable);
            WorldSelectionContext.Select(secondSelectable);
            Assert.That(WorldSelectionContext.Current, Is.SameAs(secondSelectable));
            Assert.That(firstSelectable.IsSelected, Is.False);
            Assert.That(secondSelectable.IsSelected, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }
}
#endif
