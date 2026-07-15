using System.Collections.Generic;
using System.Reflection;
using AlbaWorld.Runtime;

namespace AlbaWorld.Tests;

/// <summary>Reads the locale dictionaries committed in <see cref="LanguageService"/>.</summary>
public static class LocalizationTestData
{
    public static bool Has(string locale, string key)
    {
        if (string.IsNullOrWhiteSpace(locale) || string.IsNullOrWhiteSpace(key))
            return false;

        var fieldName = locale switch
        {
            "pt-BR" => "_pt",
            "en" => "_en",
            _ => null
        };
        if (fieldName == null)
            return false;

        var field = typeof(LanguageService).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            return false;

        var dictionaries = field.GetValue(new LanguageService(locale)) as IReadOnlyDictionary<string, string>;
        return dictionaries != null
            && dictionaries.TryGetValue(key, out var value)
            && !string.IsNullOrWhiteSpace(value);
    }
}
