using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlbaWorld.Catalog;

public static class CatalogValidation
{
    private const float RotationTolerance = 0.0001f;

    public static IReadOnlyList<string> Validate(ItemCatalog3D catalog, bool requirePrefabs)
    {
        var errors = new List<string>();
        if (catalog == null)
        {
            errors.Add("Missing catalog.");
            return errors;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < catalog.items.Count; index++)
        {
            var visual = catalog.items[index];
            if (visual == null)
            {
                errors.Add($"Missing item visual at index {index}.");
                continue;
            }

            if (visual.definition == null)
            {
                errors.Add($"Missing definition at index {index}.");
                continue;
            }

            var id = visual.definition.itemId;
            if (string.IsNullOrWhiteSpace(id))
                errors.Add($"Blank item ID at index {index}.");
            else if (!ids.Add(id))
                errors.Add($"Duplicate item ID: {id}");

            if (requirePrefabs && visual.prefab == null)
                errors.Add($"Missing prefab: {DisplayId(id, index)}");

            var placement = visual.placement;
            if (placement == null)
            {
                errors.Add($"Missing placement rules: {DisplayId(id, index)}");
                continue;
            }

            if (!IsFinite(placement.minimumScale) || !IsFinite(placement.maximumScale) ||
                placement.minimumScale <= 0f || placement.maximumScale < placement.minimumScale)
            {
                errors.Add($"Invalid scale range: {DisplayId(id, index)}");
            }

            if (!IsValidRotationStep(placement.rotationStep))
                errors.Add($"Invalid rotation step: {DisplayId(id, index)}");
        }

        return errors;
    }

    private static string DisplayId(string id, int index) =>
        string.IsNullOrWhiteSpace(id) ? $"index {index}" : id;

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsValidRotationStep(float step)
    {
        if (!IsFinite(step) || step <= 0f)
            return false;

        var turns = 360f / step;
        return Mathf.Abs(turns - Mathf.Round(turns)) <= RotationTolerance;
    }
}
