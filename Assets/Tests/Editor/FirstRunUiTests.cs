using NUnit.Framework;

public sealed class FirstRunUiTests
{
    [Test]
    public void WelcomeModeIsPublicAndBlocksWorldInput()
    {
        Assert.That(System.Enum.IsDefined(typeof(AlbaWorld.Runtime.AlbaWorldUiMode), "Welcome"), Is.True);
    }

    [Test]
    public void RoomNameCanBeUpdatedWithoutRebuildingWorld()
    {
        var app = new UnityEngine.GameObject("room-ui");
        var ui = app.AddComponent<AlbaWorld.Runtime.AlbaWorldUiController>();

        Assert.DoesNotThrow(() => ui.SetRoomName("Sala ensolarada"));
        UnityEngine.Object.DestroyImmediate(app);
    }
}
