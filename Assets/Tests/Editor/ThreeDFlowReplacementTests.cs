using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class ThreeDFlowReplacementTests
{
    [Test]
    public void ProceduralPrototypeRuntimeFilesAreRetired()
    {
        Assert.That(File.Exists("Assets/Scripts/AlbaWorldApp.cs"), Is.False,
            "The rejected procedural AlbaWorldApp must not remain in the runtime project.");
        Assert.That(File.Exists("Assets/Scripts/UI/UiFactory.cs"), Is.False,
            "The rejected runtime UI factory must not remain in the runtime project.");
        Assert.That(File.Exists("Assets/Scripts/UI/DraggableSceneElement.cs"), Is.False,
            "The rejected 2D drag implementation must not remain in the runtime project.");
        Assert.That(File.Exists("Assets/Scripts/Runtime/ColorSpriteFactory.cs"), Is.False,
            "The rejected procedural sprite factory must not remain in the runtime project.");
        Assert.That(File.Exists("Assets/Scripts/Runtime/AlbaWorld3DApp.cs"), Is.True);
    }

    [Test]
    public void MainSceneContainsOnlyThe3DCompositionRoot()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
        Assert.That(scene.IsValid, Is.True);
        Assert.That(GameObject.Find("Alba World 3D"), Is.Not.Null);
        Assert.That(GameObject.Find("Alba World App"), Is.Null);
    }
}
