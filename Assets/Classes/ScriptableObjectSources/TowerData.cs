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
}
