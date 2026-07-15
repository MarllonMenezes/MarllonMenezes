using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AlbaWorld.Tests;

public sealed class PetFollowTests
{
    [UnityTest]
    public IEnumerator FollowControllerMovesTowardAnchorWithoutPhysics()
    {
        using var fixture = PetTestFactory.Create();
        fixture.Controller.TryApply(new AlbaWorld.Core.PetLoadoutData { petId = "pet.dog" });
        fixture.Follow.FollowTarget = fixture.Target.transform;
        fixture.Target.transform.position = new Vector3(3f, 0f, 2f);

        yield return new WaitForSeconds(0.5f);

        Assert.That(Vector3.Distance(fixture.Follow.transform.position, fixture.Target.transform.position), Is.LessThan(4f));
        Assert.That(fixture.Follow.GetComponent<Rigidbody>(), Is.Null);
        Assert.That(fixture.Follow.transform.position.y, Is.GreaterThanOrEqualTo(fixture.Follow.FloorHeight));
    }

    [UnityTest]
    public IEnumerator FollowControllerUsesOffsetAndDoesNotTiltTowardAnchor()
    {
        using var fixture = PetTestFactory.Create();
        fixture.Follow.FollowTarget = fixture.Target.transform;
        fixture.Target.transform.position = new Vector3(0f, 2f, 3f);
        fixture.Follow.FloorHeight = 0f;

        yield return new WaitForSeconds(0.5f);

        Assert.That(fixture.Follow.transform.position.y, Is.GreaterThanOrEqualTo(0f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(fixture.Follow.transform.eulerAngles.x, 0f)), Is.LessThan(0.01f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(fixture.Follow.transform.eulerAngles.z, 0f)), Is.LessThan(0.01f));
    }
}
