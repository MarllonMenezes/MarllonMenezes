using NUnit.Framework;
using AlbaWorld.Runtime;

namespace AlbaWorld.Tests;

public sealed class CartoonCityLocalizationTests
{
    [Test]
    public void AllCartoonCityPresetAndPaletteLabelsHavePortugueseAndEnglishFallbacks()
    {
        var portuguese = new LanguageService("pt-BR");
        var english = new LanguageService("en");
        for (var index = 1; index <= 16; index++)
        {
            var key = $"character.preset.cartooncity.{index:00}";
            Assert.That(portuguese.Get(key), Is.Not.EqualTo(key), key);
            Assert.That(english.Get(key), Is.Not.EqualTo(key), key);
        }

        Assert.That(portuguese.Get("character.palette.default"), Is.Not.EqualTo("character.palette.default"));
        Assert.That(portuguese.Get("character.palette.pastel"), Is.Not.EqualTo("character.palette.pastel"));
        Assert.That(english.Get("wardrobe.palette"), Is.EqualTo("Colors"));
    }
}
