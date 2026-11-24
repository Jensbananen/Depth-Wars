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

    [Header("Summoning")]
    // Every SummonInterval seconds SummonCount enemies appear at this enemy's spot on the path,
    // each one picked at random from SummonIDs. 0 means it never summons.
    public int[] SummonIDs;
    public int SummonCount = 1;
    public float SummonInterval = 0f;

    [Header("Stun")]
    // Every StunInterval seconds the towers within StunRadius stop for StunDuration seconds
    public float StunInterval = 0f;
    public float StunRadius = 6f;
    public float StunDuration = 1.5f;

    private float SummonTimer;
    private float StunTimer;
    private CircleIndicator StunPulse;
    private float StunPulseTimeLeft;
    private const float StunPulseDuration = 0.3f;

    // Middle of the model, towers aim here so big enemies aren't hit at their feet
    public Vector3 AimPoint => transform.position + AimOffset;
    private Vector3 AimOffset;

    void Awake()
    {
        Renderer model = GetComponentInChildren<Renderer>();
        AimOffset = model != null ? model.bounds.center - transform.position : Vector3.up * 0.5f;
        gameObject.AddComponent<EnemyHealthBar>();
    }

    public void Init()
    {
        Health = MaxHealth;
        transform.position = GameLoopManager.NodePositions[0];
        NodeIndex = 0;
        SlowAmount = 0f;
        SlowTimeLeft = 0f;
        SummonTimer = SummonInterval;
        StunTimer = StunInterval;
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

        UpdateStunPulse(deltaTime);

        // Abilities stop once the enemy reached the end of the path
        if (NodeIndex >= GameLoopManager.NodePositions.Length)
        {
            return;
        }

        if (SummonInterval > 0f && SummonIDs != null && SummonIDs.Length > 0)
        {
            SummonTimer -= deltaTime;
            if (SummonTimer <= 0f)
            {
                SummonTimer = SummonInterval;
                for (int i = 0; i < SummonCount; i++)
                {
                    int id = SummonIDs[Random.Range(0, SummonIDs.Length)];
                    GameLoopManager.EnqueueSummonAt(id, transform.position, NodeIndex);
                }
            }
        }

        if (StunInterval > 0f)
        {
            StunTimer -= deltaTime;
            if (StunTimer <= 0f)
            {
                StunTimer = StunInterval;
                StunTowers();
            }
        }
    }

    private void StunTowers()
    {
        float radiusSqr = StunRadius * StunRadius;
        foreach (TowerBehaviour tower in GameLoopManager.TowersInGame)
        {
            Vector3 offset = tower.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= radiusSqr)
            {
                tower.Stun(StunDuration);
            }
        }

        // Short purple ring so the player sees where the stun reached
        if (StunPulse == null)
        {
            StunPulse = CircleIndicator.Create("Stun Pulse", 0.12f);
            StunPulse.transform.SetParent(transform, false);
            StunPulse.SetColor(new Color(0.75f, 0.3f, 1f));
            StunPulse.SetRadius(StunRadius);
        }
        StunPulse.SetVisible(true);
        StunPulseTimeLeft = StunPulseDuration;
    }

    private void UpdateStunPulse(float deltaTime)
    {
        if (StunPulse == null || StunPulseTimeLeft <= 0f)
        {
            return;
        }

        StunPulseTimeLeft -= deltaTime;
        if (StunPulseTimeLeft <= 0f)
        {
            StunPulse.SetVisible(false);
        }
    }

}
