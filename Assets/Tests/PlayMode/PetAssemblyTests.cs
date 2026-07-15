using System.Collections;
using AlbaWorld.Core;
using AlbaWorld.Pets;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AlbaWorld.Tests;

public sealed class PetAssemblyTests
{
    [UnityTest]
    public IEnumerator ApplyingEachPetCreatesTheRequestedPrefab()
    {
        using var fixture = PetTestFactory.Create();
        foreach (var id in KenneyPetIds.All)
        {
            Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = id }), Is.True, id);
            yield return null;
            Assert.That(fixture.Controller.ActivePetId, Is.EqualTo(id));
            Assert.That(fixture.Controller.ActiveInstance, Is.Not.Null);
            Assert.That(fixture.Controller.ActiveInstance!.transform.parent, Is.SameAs(fixture.Mount));
        }
    }

    [UnityTest]
    public IEnumerator PetColorLoadoutUsesAPropertyBlockMultiplierWithoutDuplicatingMaterials()
    {
        using var fixture = PetTestFactory.Create();
        Assert.That(fixture.Controller.TryApply(new PetLoadoutData
        {
            petId = "pet.dog",
            colorId = "petcolor.cocoa"
        }), Is.True);
        yield return null;

        var renderer = fixture.Controller.ActiveInstance!.GetComponentInChildren<Renderer>(true);
        Assert.That(renderer, Is.Not.Null);
        var properties = new MaterialPropertyBlock();
        renderer!.GetPropertyBlock(properties);
        var color = properties.GetColor(Shader.PropertyToID("_BaseColor"));
        Assert.That(color.r, Is.EqualTo(0.72f).Within(0.001f));
        Assert.That(color.g, Is.EqualTo(0.46f).Within(0.001f));
        Assert.That(color.b, Is.EqualTo(0.28f).Within(0.001f));
        Assert.That(color.a, Is.EqualTo(1f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator PetAccessoriesRemainPersistedButRenderingIsExplicitlyDeferred()
    {
        using var fixture = PetTestFactory.Create();
        Assert.That(fixture.Controller.TryApply(new PetLoadoutData
        {
            petId = "pet.cat",
            accessoryIds = new[] { "pet.bow" }
        }), Is.True);
        yield return null;

        var status = typeof(PetAssemblyController).GetProperty("AccessoryRenderingDeferred");
        Assert.That(status, Is.Not.Null);
        Assert.That(status!.GetValue(null), Is.EqualTo(true));
        Assert.That(fixture.Controller.ActiveInstance!.transform.Find("pet.bow"), Is.Null);
    }

    [UnityTest]
    public IEnumerator InvalidPetKeepsThePreviousValidInstance()
    {
        using var fixture = PetTestFactory.Create();
        Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = "pet.dog" }), Is.True);
        yield return null;
        var previous = fixture.Controller.ActiveInstance;

        Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = "pet.missing" }), Is.False);
        yield return null;

        Assert.That(fixture.Controller.ActivePetId, Is.EqualTo("pet.dog"));
        Assert.That(fixture.Controller.ActiveInstance, Is.SameAs(previous));
    }

    [UnityTest]
    public IEnumerator NullPrefabKeepsThePreviousValidInstance()
    {
        using var fixture = PetTestFactory.Create();
        Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = "pet.dog" }), Is.True);
        yield return null;
        var previous = fixture.Controller.ActiveInstance;
        fixture.Catalog.GetVisual("pet.dog")!.prefab = null;

        Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = "pet.dog" }), Is.False);
        yield return null;

        Assert.That(fixture.Controller.ActivePetId, Is.EqualTo("pet.dog"));
        Assert.That(fixture.Controller.ActiveInstance, Is.SameAs(previous));
    }

    [UnityTest]
    public IEnumerator SuccessfulReplacementDestroysThePreviousInstanceAndLeavesOneMountChild()
    {
        using var fixture = PetTestFactory.Create();
        Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = "pet.dog" }), Is.True);
        yield return null;
        var previous = fixture.Controller.ActiveInstance;
        Assert.That(fixture.Mount.childCount, Is.EqualTo(1));

        Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = "pet.cat" }), Is.True);
        yield return null;

        Assert.That(previous == null, Is.True);
        Assert.That(fixture.Mount.childCount, Is.EqualTo(1));
        Assert.That(fixture.Controller.ActivePetId, Is.EqualTo("pet.cat"));
        Assert.That(fixture.Controller.ActiveInstance, Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator InvalidFirstPetFallsBackToCat()
    {
        using var fixture = PetTestFactory.Create();

        Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = "pet.missing" }), Is.False);
        yield return null;

        Assert.That(fixture.Controller.ActivePetId, Is.EqualTo("pet.cat"));
        Assert.That(fixture.Controller.ActiveInstance, Is.Not.Null);
    }
}
