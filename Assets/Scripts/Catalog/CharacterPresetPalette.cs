using System;
using UnityEngine;

namespace AlbaWorld.Catalog
{
    [Serializable]
    public sealed class CharacterPresetPalette
    {
        public string paletteId = "default";
        public string displayKey = "character.palette.default";
        public Color skinTint = new(0.72f, 0.42f, 0.25f, 1f);
        public Color hairTint = new(0.12f, 0.06f, 0.03f, 1f);
        public Color outfitTint = new(0.98f, 0.35f, 0.63f, 1f);
        public Color shoesTint = new(0.98f, 0.55f, 0.15f, 1f);
    }
}
