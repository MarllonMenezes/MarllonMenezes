using System;
using System.Linq;
using NUnit.Framework;
using AlbaWorld.Pets;
using UnityEditor;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class KenneyPetPrefabTests
{
    [Test]
    public void EveryPetPrefabHasMeshRendererAndBoundedTriangles()
    {
        var setupType = FindType("AlbaWorld.Editor.KenneyPetAssetSetup");
        Assert.That(setupType, Is.Not.Null, "KenneyPetAssetSetup is unavailable");
        var pathMethod = setupType!.GetMethod("PrefabPathFor");
        Assert.That(pathMethod, Is.Not.Null, "PrefabPathFor is unavailable");

        var metricsType = FindType("AlbaWorld.Editor.MeshMetrics");
        Assert.That(metricsType, Is.Not.Null, "MeshMetrics is unavailable");
        var triangleMethod = metricsType!.GetMethod("TriangleCount");
        Assert.That(triangleMethod, Is.Not.Null, "TriangleCount is unavailable");

        foreach (var id in KenneyPetIds.All)
        {
            var path = (string)pathMethod.Invoke(null, new object[] { id });
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, id);
            Assert.That(prefab!.GetComponentInChildren<MeshRenderer>(), Is.Not.Null, id);
            var triangles = (int)triangleMethod.Invoke(null, new object[] { prefab });
            Assert.That(triangles, Is.LessThanOrEqualTo(7000), id);
        }
    }

    private static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, throwOnError: false))
            .FirstOrDefault(type => type != null);
    }
}
