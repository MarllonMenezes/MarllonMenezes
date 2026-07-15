using AlbaWorld.Game;
using UnityEngine;

namespace AlbaWorld.Catalog
{
    [CreateAssetMenu(menuName = "Alba World/3D Item Visual", fileName = "ItemVisual3D")]
    public sealed class ItemVisual3D : ScriptableObject
    {
        public ItemDefinition definition = null!;
        public GameObject prefab = null!;
        public GameObject girlPrefabOverride = null!;
        public GameObject boyPrefabOverride = null!;
        public EquipmentSlot equipmentSlot;
        public BodyCompatibility compatibleBodies = BodyCompatibility.Both;
        public PlacementRules placement = new();

        public string ItemId => definition == null ? string.Empty : definition.itemId;

        public GameObject PrefabForBody(string bodyId) => bodyId switch
        {
            "body.girl" when girlPrefabOverride != null => girlPrefabOverride,
            "body.boy" when boyPrefabOverride != null => boyPrefabOverride,
            _ => prefab
        };

#if UNITY_EDITOR
        public static ItemVisual3D TestOnly(string id)
        {
            var definition = CreateInstance<ItemDefinition>();
            definition.itemId = id;
            var visual = CreateInstance<ItemVisual3D>();
            visual.definition = definition;
            return visual;
        }
#endif
    }
}
