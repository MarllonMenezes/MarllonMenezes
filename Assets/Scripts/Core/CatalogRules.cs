using System.Collections.Generic;
using System.Linq;

namespace AlbaWorld.Core;

public sealed class CatalogValidationResult
{
    public bool IsValid => DuplicateIds.Count == 0;
    public IReadOnlyList<string> DuplicateIds { get; }

    public CatalogValidationResult(IEnumerable<string> duplicateIds)
    {
        DuplicateIds = duplicateIds.ToArray();
    }
}

public static class CatalogRules
{
    public static CatalogValidationResult ValidateIds(IEnumerable<string> ids)
    {
        var duplicates = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id);

        return new CatalogValidationResult(duplicates);
    }
}
