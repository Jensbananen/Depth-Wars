using UnityEngine;

public class TowerBehaviour : MonoBehaviour
{
    // Set by TowerPlacement when the tower is placed
    [HideInInspector] public TowerData Data;

    public float Range = 5f;
    // Shots per second
    public float FireRate = 1f;
    public float Damage = 10f;
}
