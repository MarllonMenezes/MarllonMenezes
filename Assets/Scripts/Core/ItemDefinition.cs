using UnityEngine;

namespace AlbaWorld.Game
{
    [CreateAssetMenu(menuName = "Alba World/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string itemId = "item.new";
        public ItemCategory category;
        public string displayKey = "item.new";
        public bool free = true;
        public Color tint = Color.white;
        [Range(0.4f, 2f)] public float scale = 1f;
        public int layer;
    }
}
