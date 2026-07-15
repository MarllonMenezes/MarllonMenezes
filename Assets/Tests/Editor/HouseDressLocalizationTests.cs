#if UNITY_EDITOR
using AlbaWorld.Runtime;
using NUnit.Framework;

namespace AlbaWorld.Tests.Editor;

public sealed class HouseDressLocalizationTests
{
    [Test]
    public void PortugueseHouseDressCopyIsReadableAndComplete()
    {
        var service = new LanguageService("pt-BR");
        var keys = new[]
        {
            "hud.house", "hud.dress", "hud.furniture", "hud.actions", "hud.delete",
            "hud.undo", "hud.moveHint", "hud.noFreeSlot", "wardrobe.skin", "wardrobe.hair",
            "wardrobe.outfit", "wardrobe.shoes", "wardrobe.accessories", "welcome.title", "welcome.subtitle",
            "welcome.select", "welcome.drag", "welcome.modes", "welcome.play", "welcome.language", "room.sunny", "room.cozy"
        };

        foreach (var key in keys)
        {
            var value = service.Get(key);
            Assert.That(value, Does.Not.EqualTo(key), key);
            Assert.That(value, Does.Not.Contain("Ã"), key);
            Assert.That(value, Does.Not.Contain("Â"), key);
            Assert.That(value.IndexOf('\uFFFD'), Is.EqualTo(-1), key);
        }
    }
}
#endif
