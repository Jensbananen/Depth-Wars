using UnityEngine;
using System.Collections.Generic;

public enum TargetMode
{
    First,      // furthest along the path
    Last,       // least far along the path
    Strongest,  // most HP left
    Closest     // nearest to the tower
}

public class TowerBehaviour : MonoBehaviour
{
    // Set by TowerPlacement when the tower is placed
    [HideInInspector] public TowerData Data;

    public float Range = 5f;
    // Shots per second
    public float FireRate = 1f;
    public float Damage = 10f;
    public DamageType DamageType = DamageType.Bullet;
    public TargetMode TargetMode = TargetMode.First;

    // 0 only hits the target, otherwise every enemy this close to the target is hit too
    public float SplashRadius = 0f;
    // Part of the enemy's speed taken away on a hit, 0.5 = half as fast
    [Range(0f, 1f)] public float SlowAmount = 0f;
    public float SlowDuration = 0f;

    [Header("Support")]
    // Extra fire rate for the other towers in range, 0.1 = 10% faster
    public float FireRateBuff = 0f;
    // Money after every wave
    public int MoneyPerWave = 0;
    // Unit for each level, every SummonInterval seconds one walks onto the path during a wave
    public FriendlyUnit[] UnitPrefabs;
    public float SummonInterval = 8f;

    // From the strongest tower with a FireRateBuff nearby, set by the game loop every frame
    [HideInInspector] public float ReceivedBuff;

    // Starts at 0, goes up by one for every bought upgrade
    [HideInInspector] public int Level;
    // Money spent on upgrades, so selling gives part of it back
    [HideInInspector] public int UpgradeCost;

    public int MaxLevel => Data != null && Data.Upgrades != null ? Data.Upgrades.Length : 0;
    public TowerUpgrade NextUpgrade => Level < MaxLevel ? Data.Upgrades[Level] : null;

    public bool IsStunned => StunTimeLeft > 0f;
    // Support towers don't shoot
    public bool Attacks => FireRate > 0f;
    public float CurrentFireRate => FireRate * (1f + ReceivedBuff);
    public bool HasUnits => UnitPrefabs != null && UnitPrefabs.Length > 0;

    // Part of the tower that turns towards the target, the shot also starts here
    [SerializeField] private Transform HeadPivot;
    [SerializeField] private float TurnSpeed = 720f;
    [SerializeField] private Color ShotColor = new Color(1f, 0.9f, 0.4f);
    [SerializeField] private float ShotDuration = 0.05f;
    [SerializeField] private float ShotWidth = 0.05f;

    private Enemy Target;
    private float FireCooldown;
    private LineRenderer ShotLine;
    private float ShotTimeLeft;
    // Set by the final boss, the tower does nothing while this is above 0
    private float StunTimeLeft;
    private CircleIndicator StunRing;
    private float SummonTimer;

    // The unit of this level, or of the last one that has its own unit
    public FriendlyUnit GetUnit(int level)
    {
        return UnitPrefabs[Mathf.Min(level, UnitPrefabs.Length - 1)];
    }

    // What this tower does right now, one thing per line
    public string GetStatsText()
    {
        List<string> lines = new List<string>();
        if (Attacks)
        {
            lines.Add($"Damage: {Damage:0.##} ({DamageTypes.GetColoredName(DamageType)})");
            lines.Add($"Fire rate: {CurrentFireRate:0.##} /s" + (ReceivedBuff > 0f ? $" (+{ReceivedBuff * 100f:0}%)" : ""));
        }
        if (FireRateBuff > 0f) lines.Add($"Boost: +{FireRateBuff * 100f:0}% fire rate");
        if (Attacks || FireRateBuff > 0f) lines.Add($"Range: {Range:0.##}");
        if (SplashRadius > 0f) lines.Add($"Splash: {SplashRadius:0.##}");
        if (SlowAmount > 0f) lines.Add($"Slow: {SlowAmount * 100f:0}% for {SlowDuration:0.##}s");
        if (MoneyPerWave > 0) lines.Add($"Money: ${MoneyPerWave} per wave");
        if (HasUnits)
        {
            FriendlyUnit unit = GetUnit(Level);
            lines.Add($"Unit: {unit.UnitName} ({unit.Health:0} HP)");
            lines.Add($"Every {SummonInterval:0.##}s");
            lines.Add($"Damage: {DamageTypes.GetColoredName(DamageType.Collision)}");
        }
        return string.Join("\n", lines);
    }

    public void Stun(float duration)
    {
        StunTimeLeft = Mathf.Max(StunTimeLeft, duration);

        // Purple ring around the tower while it is stunned
        if (StunRing == null)
        {
            StunRing = CircleIndicator.Create("Stun Ring", 0.08f);
            StunRing.transform.SetParent(transform, false);
            StunRing.SetColor(new Color(0.75f, 0.3f, 1f));
            StunRing.SetRadius(0.6f);
        }
        StunRing.SetVisible(true);
    }

    public void ApplyUpgrade(TowerUpgrade upgrade)
    {
        Damage += upgrade.Damage;
        Range += upgrade.Range;
        FireRate += upgrade.FireRate;
        SlowAmount = Mathf.Clamp01(SlowAmount + upgrade.Slow);
        FireRateBuff += upgrade.FireRateBuff;
        MoneyPerWave += upgrade.MoneyPerWave;
        SummonInterval += upgrade.SummonInterval;
        UpgradeCost += upgrade.Cost;
        Level++;
    }

    // Called by the game loop every frame for placed towers
    public void Tick()
    {
        float deltaTime = Time.deltaTime;
        // At most one frame below 0, so waiting for a target doesn't save up shots
        FireCooldown = Mathf.Max(FireCooldown - deltaTime, -deltaTime);

        UpdateShotLine(deltaTime);

        if (StunTimeLeft > 0f)
        {
            StunTimeLeft -= deltaTime;
            if (StunTimeLeft <= 0f)
            {
                StunRing.SetVisible(false);
            }
            return;
        }

        if (HasUnits && GameLoopManager.IsWaveRunning)
        {
            SummonTimer -= deltaTime;
            if (SummonTimer <= 0f)
            {
                SummonTimer = SummonInterval;
                GameLoopManager.SummonUnit(GetUnit(Level));
            }
        }

        if (!Attacks)
        {
            return;
        }

        Target = FindTarget();
        if (Target == null)
        {
            return;
        }

        Vector3 aimPoint = Target.AimPoint;
        TurnTowards(aimPoint, deltaTime);

        // A fast tower can shoot more than once per frame at a high game speed
        while (FireCooldown <= 0f)
        {
            Fire(Target, aimPoint);
            FireCooldown += 1f / CurrentFireRate;
        }
    }

    // The enemy in range that fits the TargetMode best
    private Enemy FindTarget()
    {
        Enemy best = null;
        float bestScore = float.MinValue;
        float bestTieBreak = float.MinValue;
        float rangeSqr = Range * Range;
        Vector3 position = transform.position;

        foreach (Enemy enemy in EntitySummoner.EnemiesInGame)
        {
            Vector3 offset = enemy.transform.position - position;
            offset.y = 0f;
            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr > rangeSqr)
            {
                continue;
            }

            // Higher score wins, on a tie the enemy furthest along the path wins
            float progress = GetPathProgress(enemy);
            float score;
            switch (TargetMode)
            {
                case TargetMode.Last: score = -progress; break;
                case TargetMode.Strongest: score = enemy.Health; break;
                case TargetMode.Closest: score = -distanceSqr; break;
                default: score = progress; break;
            }

            if (score > bestScore || (score == bestScore && progress > bestTieBreak))
            {
                best = enemy;
                bestScore = score;
                bestTieBreak = progress;
            }
        }

        return best;
    }

    // How far the enemy is along the path, bigger is further
    private float GetPathProgress(Enemy enemy)
    {
        int nodeIndex = enemy.NodeIndex;
        float distanceToNode = nodeIndex < GameLoopManager.NodePositions.Length
            ? Vector3.Distance(GameLoopManager.NodePositions[nodeIndex], enemy.transform.position)
            : 0f;
        return nodeIndex * 1000f - distanceToNode;
    }

    private void TurnTowards(Vector3 point, float deltaTime)
    {
        if (HeadPivot == null)
        {
            return;
        }

        Vector3 direction = point - HeadPivot.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        HeadPivot.rotation = Quaternion.RotateTowards(HeadPivot.rotation, targetRotation, TurnSpeed * deltaTime);
    }

    private void Fire(Enemy target, Vector3 aimPoint)
    {
        if (SplashRadius > 0f)
        {
            float splashSqr = SplashRadius * SplashRadius;
            foreach (Enemy enemy in EntitySummoner.EnemiesInGame)
            {
                Vector3 offset = enemy.transform.position - target.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= splashSqr)
                {
                    Hit(enemy);
                }
            }
        }
        else
        {
            Hit(target);
        }

        if (ShotLine == null)
        {
            ShotLine = CreateShotLine();
        }

        Vector3 origin = HeadPivot != null ? HeadPivot.position : transform.position;
        ShotLine.SetPosition(0, origin);
        ShotLine.SetPosition(1, aimPoint);
        ShotLine.enabled = true;
        ShotTimeLeft = ShotDuration;
    }

    private void Hit(Enemy enemy)
    {
        if (Damage > 0f)
        {
            GameLoopManager.EnqueueDamageData(new EnemyDamageData(enemy, Damage, DamageType));
        }
        if (SlowAmount > 0f)
        {
            enemy.ApplySlow(SlowAmount, SlowDuration);
        }
    }

    private void UpdateShotLine(float deltaTime)
    {
        if (ShotLine == null || !ShotLine.enabled)
        {
            return;
        }

        ShotTimeLeft -= deltaTime;
        if (ShotTimeLeft <= 0f)
        {
            ShotLine.enabled = false;
        }
    }

    private LineRenderer CreateShotLine()
    {
        GameObject lineObject = new GameObject("Shot Line");
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = LineMaterial.Get();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = ShotWidth;
        line.startColor = ShotColor;
        line.endColor = ShotColor;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }
}
