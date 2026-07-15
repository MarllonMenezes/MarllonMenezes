#if UNITY_EDITOR
using AlbaWorld.Runtime;
using NUnit.Framework;

namespace AlbaWorld.Tests.Editor;

public sealed class HouseDressLayoutContractTests
{
    [Test]
    public void UiModesExposeCasaAndVestir()
    {
        Assert.That(System.Enum.IsDefined(typeof(AlbaWorldUiMode), "Casa"), Is.True);
        Assert.That(System.Enum.IsDefined(typeof(AlbaWorldUiMode), "Vestir"), Is.True);
    }

    [Test]
    public void WalkableZoneHasRoomForCharacter()
    {
        var bounds = RoomFurnitureController.DefaultWalkableBounds;
        Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(3.2f));
        Assert.That(bounds.size.z, Is.GreaterThanOrEqualTo(2.6f));
    }
}
#endif
