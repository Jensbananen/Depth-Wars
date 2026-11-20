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

    // Part of the speed taken away by slow towers, 0.5 = half as fast
    [HideInInspector] public float SlowAmount;
    [HideInInspector] public float SlowTimeLeft;
    public float CurrentSpeed => Speed * (1f - SlowAmount);

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
        SlowAmount = 0f;
        SlowTimeLeft = 0f;
    }

    // A stronger slow replaces a weaker one, the same slow only refreshes the timer
    public void ApplySlow(float amount, float duration)
    {
        if (amount > SlowAmount)
        {
            SlowAmount = amount;
            SlowTimeLeft = duration;
        }
        else if (amount == SlowAmount)
        {
            SlowTimeLeft = Mathf.Max(SlowTimeLeft, duration);
        }
    }

    // Called by the game loop every frame
    public void TickEffects(float deltaTime)
    {
        if (SlowTimeLeft > 0f)
        {
            SlowTimeLeft -= deltaTime;
            if (SlowTimeLeft <= 0f)
            {
                SlowAmount = 0f;
            }
        }
    }

}
