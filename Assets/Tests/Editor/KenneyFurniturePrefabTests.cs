#if UNITY_EDITOR
using System.Linq;
using AlbaWorld.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class KenneyFurniturePrefabTests
{
    [Test]
    public void EveryApprovedFurnitureItemHasAUsablePrefab()
    {
        foreach (var id in KenneyFurnitureAssetSetup.AllIds)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyFurnitureAssetSetup.PrefabPathFor(id));
            Assert.That(prefab, Is.Not.Null, id);
            Assert.That(prefab!.GetComponentsInChildren<MeshRenderer>(true).Any(), Is.True, id);
            var triangles = prefab.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.sharedMesh != null)
                .Sum(filter => filter.sharedMesh!.triangles.Length / 3);
            Assert.That(triangles, Is.GreaterThan(0), id);
        }
    }
}
#endif
