using System;
using System.Collections.Generic;
using System.Linq;
using AlbaWorld.Game;
using UnityEngine;

namespace AlbaWorld.Catalog
{
    public interface IItemCatalog3D
    {
        ItemVisual3D? GetVisual(string id);
        IEnumerable<ItemVisual3D> ByCategory(ItemCategory category);
    }

    [CreateAssetMenu(menuName = "Alba World/3D Item Catalog", fileName = "ItemCatalog3D")]
    public sealed class ItemCatalog3D : ScriptableObject, IItemCatalog3D
    {
        public List<ItemVisual3D> items = new();

        [NonSerialized] private Dictionary<string, ItemVisual3D>? _itemsById;
        [NonSerialized] private int _sourceSignature;

        public ItemVisual3D? GetVisual(string id)
        {
            EnsureLookup();
            return id != null && _itemsById!.TryGetValue(id, out var visual) ? visual : null;
        }

        public IEnumerable<ItemVisual3D> ByCategory(ItemCategory category) =>
            items.Where(visual => visual != null && visual.definition != null && visual.definition.category == category);

        private void OnEnable() => _itemsById = null;
        private void OnValidate() => _itemsById = null;

        private void EnsureLookup()
        {
            var signature = ComputeSourceSignature();
            if (_itemsById != null && _sourceSignature == signature)
                return;

            var rebuilt = new Dictionary<string, ItemVisual3D>(StringComparer.Ordinal);
            foreach (var visual in items)
            {
                if (visual == null || string.IsNullOrWhiteSpace(visual.ItemId))
                    continue;

                rebuilt.TryAdd(visual.ItemId, visual);
            }

            _itemsById = rebuilt;
            _sourceSignature = signature;
        }

        private int ComputeSourceSignature()
        {
            unchecked
            {
                var signature = items.Count;
                foreach (var visual in items)
                {
                    signature = signature * 397 ^ (visual == null ? 0 : visual.GetInstanceID());
                    signature = signature * 397 ^ (visual == null ? 0 : StringComparer.Ordinal.GetHashCode(visual.ItemId));
                }

                return signature;
            }
        }
    }
}
