using System;
using AlbaWorld.Catalog;
using AlbaWorld.Game;
using AlbaWorld.Pets;
using UnityEngine;

namespace AlbaWorld.Tests;

public static class PetTestFactory
{
    public static Fixture Create()
    {
        var root = new GameObject("PetControllerTestRoot");
        var templates = new GameObject("InMemoryPetTemplates");
        templates.transform.SetParent(root.transform, false);
        templates.SetActive(false);

        var catalog = ScriptableObject.CreateInstance<ItemCatalog3D>();
        catalog.hideFlags = HideFlags.HideAndDontSave;
        foreach (var id in KenneyPetIds.All)
            catalog.items.Add(CreateVisual(id, templates.transform));

        var rig = new GameObject("PetRig");
        rig.transform.SetParent(root.transform, false);
        var mount = new GameObject("PetMount");
        mount.transform.SetParent(rig.transform, false);
        var target = new GameObject("PetTarget");
        target.transform.SetParent(root.transform, false);

        var controller = rig.AddComponent<PetAssemblyController>();
        controller.Initialize(catalog, mount.transform);
        var follow = rig.AddComponent<PetFollowController>();
        return new Fixture(root, catalog, controller, follow, target, mount.transform);
    }

    private static ItemVisual3D CreateVisual(string itemId, Transform templateRoot)
    {
        var definition = ScriptableObject.CreateInstance<ItemDefinition>();
        definition.hideFlags = HideFlags.HideAndDontSave;
        definition.itemId = itemId;
        definition.category = ItemCategory.Pet;

        var prefab = new GameObject($"{itemId}.InMemoryPrefab");
        prefab.transform.SetParent(templateRoot, false);
        prefab.AddComponent<MeshFilter>();
        prefab.AddComponent<MeshRenderer>();

        var visual = ScriptableObject.CreateInstance<ItemVisual3D>();
        visual.hideFlags = HideFlags.HideAndDontSave;
        visual.definition = definition;
        visual.prefab = prefab;
        return visual;
    }

    public sealed class Fixture : IDisposable
    {
        private readonly GameObject _root;
        private readonly ItemCatalog3D _catalog;

        internal Fixture(
            GameObject root,
            ItemCatalog3D catalog,
            PetAssemblyController controller,
            PetFollowController follow,
            GameObject target,
            Transform mount)
        {
            _root = root;
            _catalog = catalog;
            Controller = controller;
            Follow = follow;
            Target = target;
            Mount = mount;
        }

        public PetAssemblyController Controller { get; }
        public PetFollowController Follow { get; }
        public GameObject Target { get; }
        public Transform Mount { get; }

        public void Dispose()
        {
            if (_root != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_root);
                else
                    UnityEngine.Object.DestroyImmediate(_root);
            }

            if (_catalog != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_catalog);
                else
                    UnityEngine.Object.DestroyImmediate(_catalog);
            }

            GC.SuppressFinalize(this);
        }
    }
}
