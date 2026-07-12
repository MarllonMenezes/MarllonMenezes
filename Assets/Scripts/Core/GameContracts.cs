using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlbaWorld.Game;

public enum ItemCategory
{
    Skin,
    Hair,
    Outfit,
    Shoes,
    HumanAccessory,
    Pet,
    PetAccessory,
    Furniture,
    Decor
}

[CreateAssetMenu(menuName = "Alba World/Item Definition", fileName = "ItemDefinition")]
public sealed class ItemDefinition : ScriptableObject
{
    public string itemId = "item.new";
    public ItemCategory category;
    public string displayKey = "item.new";
    public bool free = true;
    public Color tint = Color.white;
    [Range(0.4f, 2f)] public float scale = 1f;
    public int layer = 0;
}

[CreateAssetMenu(menuName = "Alba World/Item Catalog", fileName = "ItemCatalog")]
public sealed class ItemCatalogAsset : ScriptableObject
{
    public List<ItemDefinition> items = new();

    public ItemDefinition? Get(string id) => items.FirstOrDefault(item => item != null && item.itemId == id);

    public IEnumerable<ItemDefinition> ByCategory(ItemCategory category) =>
        items.Where(item => item != null && item.category == category);
}

[Serializable]
public sealed class SceneElementData
{
    public string itemId = string.Empty;
    public float x = 0.5f;
    public float y = 0.5f;
    public float scale = 1f;
    public int order;
}

[Serializable]
public sealed class SceneSnapshot
{
    public string roomId = "room.sunny";
    public List<SceneElementData> elements = new();
}

public interface IItemCatalog
{
    ItemDefinition? Get(string id);
    IEnumerable<ItemDefinition> ByCategory(ItemCategory category);
}

public interface ISaveService
{
    Core.GameSaveData Load();
    void Save(Core.GameSaveData data);
}

public interface IRewardedAdsService
{
    bool IsAvailable { get; }
    void ShowForItem(string itemId, Action<bool> completed);
}

public interface IPhotoExportService
{
    bool CaptureAndSave(SceneSnapshot snapshot);
}
