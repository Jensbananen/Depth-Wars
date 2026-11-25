using UnityEngine;

// Enemies can take less damage of some types, see Enemy.Resistances
public enum DamageType
{
    Bullet,
    Melee,
    Laser,
    Explosion,
    Collision   // friendly units running into an enemy
}

[System.Serializable]
public struct DamageResistance
{
    public DamageType Type;
    // Part of the damage that is ignored, 0.25 = 25% less
    [Range(0f, 0.9f)] public float Amount;
}

public static class DamageTypes
{
    // Name in its own color, for text in the UI
    public static string GetColoredName(DamageType type)
    {
        string color;
        switch (type)
        {
            case DamageType.Melee: color = "#FFFFFF"; break;
            case DamageType.Laser: color = "#4DE6FF"; break;
            case DamageType.Explosion: color = "#FF8C33"; break;
            case DamageType.Collision: color = "#7DFF7D"; break;
            default: color = "#FFE066"; break;
        }
        return $"<color={color}>{type}</color>";
    }
}
