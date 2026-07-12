using System.Collections.Generic;
using System.Linq;
using AlbaWorld.Game;
using UnityEngine;

namespace AlbaWorld.Runtime;

public sealed class RuntimeCatalog : IItemCatalog
{
    private readonly Dictionary<string, ItemDefinition> _items = new();

    public RuntimeCatalog()
    {
        Add("skin.cream", ItemCategory.Skin, "skin.cream", true, new Color(0.98f, 0.75f, 0.58f), 0);
        Add("skin.brown", ItemCategory.Skin, "skin.brown", true, new Color(0.55f, 0.30f, 0.18f), 0);
        Add("skin.gold", ItemCategory.Skin, "skin.gold", true, new Color(0.78f, 0.48f, 0.27f), 0);
        Add("skin.deep", ItemCategory.Skin, "skin.deep", true, new Color(0.28f, 0.13f, 0.08f), 0);
        Add("skin.rose", ItemCategory.Skin, "skin.rose", true, new Color(0.72f, 0.36f, 0.25f), 0);
        Add("skin.honey", ItemCategory.Skin, "skin.honey", true, new Color(0.91f, 0.61f, 0.35f), 0);

        Add("hair.sunny", ItemCategory.Hair, "item.hair.sunny", true, new Color(0.25f, 0.12f, 0.08f), 3);
        Add("hair.bubble", ItemCategory.Hair, "item.hair.bubble", true, new Color(0.86f, 0.30f, 0.30f), 3);
        Add("hair.rainbow", ItemCategory.Hair, "item.hair.rainbow", false, new Color(0.38f, 0.25f, 0.80f), 3);
        Add("hair.cloud", ItemCategory.Hair, "item.hair.cloud", true, new Color(0.95f, 0.90f, 0.86f), 3);
        Add("hair.mint", ItemCategory.Hair, "item.hair.mint", false, new Color(0.30f, 0.82f, 0.72f), 3);
        Add("outfit.pink", ItemCategory.Outfit, "item.outfit.pink", true, new Color(0.94f, 0.39f, 0.63f), 2);
        Add("outfit.mint", ItemCategory.Outfit, "item.outfit.mint", true, new Color(0.30f, 0.78f, 0.66f), 2);
        Add("outfit.blue", ItemCategory.Outfit, "item.outfit.blue", false, new Color(0.25f, 0.46f, 0.87f), 2);
        Add("outfit.sun", ItemCategory.Outfit, "item.outfit.sun", true, new Color(0.98f, 0.70f, 0.24f), 2);
        Add("outfit.lilac", ItemCategory.Outfit, "item.outfit.lilac", false, new Color(0.65f, 0.48f, 0.88f), 2);
        Add("shoes.sun", ItemCategory.Shoes, "shoes.sun", true, new Color(1f, 0.79f, 0.20f), 2);
        Add("shoes.mint", ItemCategory.Shoes, "shoes.mint", true, new Color(0.30f, 0.75f, 0.70f), 2);
        Add("shoes.rose", ItemCategory.Shoes, "shoes.rose", true, new Color(0.94f, 0.42f, 0.56f), 2);
        Add("accessory.star", ItemCategory.HumanAccessory, "accessory.star", true, new Color(1f, 0.74f, 0.22f), 4);
        Add("accessory.flower", ItemCategory.HumanAccessory, "accessory.flower", true, new Color(0.97f, 0.35f, 0.53f), 4);
        Add("accessory.glasses", ItemCategory.HumanAccessory, "accessory.glasses", false, new Color(0.15f, 0.14f, 0.23f), 4);
        Add("accessory.ribbon", ItemCategory.HumanAccessory, "accessory.ribbon", true, new Color(0.30f, 0.55f, 0.95f), 4);

        Add("pet.cat", ItemCategory.Pet, "item.pet.cat", true, new Color(0.96f, 0.76f, 0.55f), 1);
        Add("pet.dog", ItemCategory.Pet, "item.pet.dog", true, new Color(0.68f, 0.43f, 0.25f), 1);
        Add("pet.bow", ItemCategory.PetAccessory, "item.pet.bow", true, new Color(0.95f, 0.25f, 0.45f), 3);
        Add("pet.cap", ItemCategory.PetAccessory, "item.pet.cap", false, new Color(0.20f, 0.52f, 0.90f), 3);
        Add("pet.bandana", ItemCategory.PetAccessory, "item.pet.bandana", true, new Color(0.28f, 0.74f, 0.55f), 3);

        Add("furniture.bed", ItemCategory.Furniture, "item.furniture.bed", true, new Color(0.91f, 0.56f, 0.76f), 1);
        Add("furniture.sofa", ItemCategory.Furniture, "item.furniture.sofa", true, new Color(0.54f, 0.44f, 0.90f), 1);
        Add("furniture.table", ItemCategory.Furniture, "item.furniture.table", true, new Color(0.87f, 0.61f, 0.38f), 1);
        Add("furniture.plant", ItemCategory.Decor, "item.furniture.plant", true, new Color(0.32f, 0.72f, 0.40f), 2);
        Add("furniture.lamp", ItemCategory.Decor, "item.furniture.lamp", true, new Color(1f, 0.78f, 0.24f), 2);
        Add("furniture.rug", ItemCategory.Decor, "item.furniture.rug", false, new Color(0.42f, 0.75f, 0.88f), 0);
        Add("furniture.book", ItemCategory.Decor, "books", true, new Color(0.88f, 0.36f, 0.42f), 2);
        Add("furniture.picture", ItemCategory.Decor, "picture", true, new Color(0.90f, 0.53f, 0.26f), 2);
        Add("furniture.chair", ItemCategory.Furniture, "item.furniture.chair", true, new Color(0.37f, 0.64f, 0.86f), 1);
        Add("furniture.shelf", ItemCategory.Furniture, "item.furniture.shelf", false, new Color(0.63f, 0.40f, 0.25f), 1);
        Add("furniture.cushion", ItemCategory.Decor, "item.furniture.cushion", true, new Color(0.96f, 0.52f, 0.70f), 1);
        Add("furniture.clock", ItemCategory.Decor, "item.furniture.clock", true, new Color(0.95f, 0.77f, 0.38f), 2);
    }

    private void Add(string id, ItemCategory category, string displayKey, bool free, Color tint, int layer)
    {
        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.name = id;
        item.itemId = id;
        item.category = category;
        item.displayKey = displayKey;
        item.free = free;
        item.tint = tint;
        item.layer = layer;
        _items[id] = item;
    }

    public ItemDefinition? Get(string id) => _items.TryGetValue(id, out var item) ? item : null;
    public IEnumerable<ItemDefinition> ByCategory(ItemCategory category) => _items.Values.Where(item => item.category == category);
    public IEnumerable<ItemDefinition> All() => _items.Values;
}
