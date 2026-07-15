using System;

namespace AlbaWorld.Catalog;

public enum EquipmentSlot
{
    None,
    Body,
    Face,
    Hair,
    Outfit,
    Shoes,
    Head,
    FaceAccessory,
    Back,
    Hand,
    Pet,
    PetAccessory
}

public enum PlacementKind
{
    None,
    Floor,
    Surface,
    Wall
}

[Flags]
public enum BodyCompatibility
{
    None = 0,
    Girl = 1,
    Boy = 2,
    Both = Girl | Boy
}

[Serializable]
public sealed class PlacementRules
{
    public PlacementKind kind = PlacementKind.None;
    public float minimumScale = 0.8f;
    public float maximumScale = 1.2f;
    public float rotationStep = 45f;
    public bool canHostSmallObjects;
}
