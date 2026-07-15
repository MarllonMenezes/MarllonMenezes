using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using AlbaWorld.Editor;
using AlbaWorld.Pets;
using UnityEditor;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class KenneyPetPrefabTests
{
    [Test]
    public void EveryPetPrefabHasMeshRendererAndBoundedTriangles()
    {
        var sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art3D/Pets/Materials/KenneyPets.mat");
        var colormap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art3D/Pets/Textures/colormap.png");
        Assert.That(sharedMaterial, Is.Not.Null, "KenneyPets.mat is missing");
        Assert.That(colormap, Is.Not.Null, "colormap.png is missing");
        Assert.That(sharedMaterial!.shader.name, Is.EqualTo("Universal Render Pipeline/Simple Lit"));

        foreach (var id in KenneyPetIds.All)
        {
            var path = KenneyPetAssetSetup.PrefabPathFor(id);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, id);
            Assert.That(prefab!.transform.localScale, Is.EqualTo(Vector3.one), $"{id} root scale");
            Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero), $"{id} root pivot");
            Assert.That(prefab.GetComponentsInChildren<Camera>(true), Is.Empty, $"{id} cameras");
            Assert.That(prefab.GetComponentsInChildren<Light>(true), Is.Empty, $"{id} lights");

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, id);
            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials, Is.Not.Empty, $"{id} material slots");
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.SameAs(sharedMaterial), $"{id} shared material");
                    Assert.That(material!.GetTexture("_BaseMap"), Is.SameAs(colormap), $"{id} colormap");
                }

                Mesh mesh = renderer switch
                {
                    MeshRenderer => renderer.GetComponent<MeshFilter>()?.sharedMesh,
                    SkinnedMeshRenderer skinned => skinned.sharedMesh,
                    _ => null
                };
                Assert.That(mesh, Is.Not.Null, $"{id} renderer mesh ({renderer.name})");
                var bounds = renderer.bounds;
                Assert.That(IsFinite(bounds.center) && IsFinite(bounds.size) && bounds.size.sqrMagnitude > 0f,
                    Is.True, $"{id} renderer bounds ({renderer.name})");
            }

            var triangles = MeshMetrics.TriangleCount(prefab);
            Assert.That(triangles, Is.LessThanOrEqualTo(7000), id);
        }
    }

    [Test]
    public void SetupIsIdempotentAndPrefabPathRejectsUnknownIds()
    {
        Assert.Throws<ArgumentException>(() => KenneyPetAssetSetup.PrefabPathFor("pet.unknown"));
        Assert.Throws<ArgumentException>(() => KenneyPetAssetSetup.PrefabPathFor("pet."));
        Assert.Throws<ArgumentException>(() => KenneyPetAssetSetup.PrefabPathFor("animal-cat"));

        KenneyPetAssetSetup.Setup();
        var first = PrefabHashes();
        KenneyPetAssetSetup.Setup();
        var second = PrefabHashes();
        Assert.That(second, Is.EqualTo(first), "Running Kenney setup twice must preserve every prefab byte-for-byte.");
    }

    private static Dictionary<string, string> PrefabHashes()
    {
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var id in KenneyPetIds.All)
        {
            var path = KenneyPetAssetSetup.PrefabPathFor(id);
            var absolutePath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, path.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(absolutePath), Is.True, id);
            using var sha = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(absolutePath);
            hashes.Add(id, BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty));
        }
        return hashes;
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
