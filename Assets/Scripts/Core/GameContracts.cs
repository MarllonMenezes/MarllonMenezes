using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlbaWorld.Game;

public enum ItemCategory
{
    Skin = 0,
    Hair = 1,
    Outfit = 2,
    Shoes = 3,
    HumanAccessory = 4,
    Pet = 5,
    PetAccessory = 6,
    Furniture = 7,
    Decor = 8,
    Body = 9,
    Face = 10,
    PetColor = 11
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
