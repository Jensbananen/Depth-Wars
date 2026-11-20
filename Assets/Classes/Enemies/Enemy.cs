using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Enemy : MonoBehaviour
{
    public int NodeIndex;
    public float MaxHealth;
    public float Health;
    public float Speed;
    public int ID;
    // Lives the player loses when this enemy reaches the end of the path
    public int LivesDamage = 1;
    // Money the player gets for killing this enemy
    public int MoneyReward = 10;

    // Middle of the model, towers aim here so big enemies aren't hit at their feet
    public Vector3 AimPoint => transform.position + AimOffset;
    private Vector3 AimOffset;

    void Awake()
    {
        Renderer model = GetComponentInChildren<Renderer>();
        AimOffset = model != null ? model.bounds.center - transform.position : Vector3.up * 0.5f;
    }

    public void Init()
    {
        Health = MaxHealth;
        transform.position = GameLoopManager.NodePositions[0];
        NodeIndex = 0;
    }

}
