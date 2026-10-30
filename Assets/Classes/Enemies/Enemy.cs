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

    public void Init()
    {
        Health = MaxHealth;
        transform.position = GameLoopManager.NodePositions[0];
        NodeIndex = 0;
    }

}
