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
    public IEnumerator InvalidFirstPetFallsBackToCat()
    {
        using var fixture = PetTestFactory.Create();

        Assert.That(fixture.Controller.TryApply(new PetLoadoutData { petId = "pet.missing" }), Is.False);
        yield return null;

        Assert.That(fixture.Controller.ActivePetId, Is.EqualTo("pet.cat"));
        Assert.That(fixture.Controller.ActiveInstance, Is.Not.Null);
    }
}
