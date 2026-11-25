using UnityEngine;

[CreateAssetMenu(fileName = "New TowerData", menuName = "Create TowerData")]
public class TowerData : ScriptableObject
{
    public string TowerName;
    public GameObject TowerPrefab;
    public int Cost;

    // Price in coins in the shop, 0 means the player owns it from the start
    public int CoinPrice;

    // Optional, when empty the shop renders a picture of the TowerPrefab
    public Sprite Icon;

    // Tag of the surface this tower can stand on, e.g. CanPlaceClifftower for cliff towers
    public string PlacementTag = "CanPlace";

    // Radius of the space the tower needs on the ground, bigger towers need more room.
    // Never smaller than the tower itself, so the tower can't stick out over the path.
    public float FootprintRadius = 0.7f;

    // Upgrades for level 1 to 5 in order, a placed tower starts at level 0
    public TowerUpgrade[] Upgrades;

    public bool IsCliffTower => PlacementTag == "CanPlaceClifftower";

    // Stats of the tower at level 0, one per line
    public string GetStatsText()
    {
        return TowerPrefab.GetComponent<TowerBehaviour>().GetStatsText() + $"\nPlaced on: {(IsCliffTower ? "Cliffs" : "Ground")}";
    }
}

[System.Serializable]
public class TowerUpgrade
{
    public int Cost = 200;
    // Added on top of the current stats
    public float Damage;
    public float Range;
    public float FireRate;
    // Extra part of the speed taken away, 0.05 = 5% slower
    public float Slow;
    // Extra fire rate for the other towers in range, 0.05 = 5% faster
    public float FireRateBuff;
    public int MoneyPerWave;
    // Seconds longer between two units, stronger units come less often
    public float SummonInterval;
}
