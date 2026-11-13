using UnityEngine;

[CreateAssetMenu(fileName = "New TowerData", menuName = "Create TowerData")]
public class TowerData : ScriptableObject
{
    public string TowerName;
    public GameObject TowerPrefab;
    public int Cost;

    // Optional, when empty the shop renders a picture of the TowerPrefab
    public Sprite Icon;

    // Tag of the surface this tower can stand on, e.g. CanPlaceClifftower for cliff towers
    public string PlacementTag = "CanPlace";

    // Radius of the space the tower needs on the ground, bigger towers need more room.
    // Never smaller than the tower itself, so the tower can't stick out over the path.
    public float FootprintRadius = 0.7f;

    // Upgrades for level 1 to 5 in order, a placed tower starts at level 0
    public TowerUpgrade[] Upgrades;
}

[System.Serializable]
public class TowerUpgrade
{
    public int Cost = 200;
    // Added on top of the current stats
    public float Damage;
    public float Range;
    public float FireRate;
}
