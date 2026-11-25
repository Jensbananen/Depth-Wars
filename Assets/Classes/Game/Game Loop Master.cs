using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;

public class GameLoopManager : MonoBehaviour
{
    public static Vector3[] NodePositions;
    public static List<TowerBehaviour> TowersInGame;
    // All towers on the map together
    public const int MaxTowers = 40;
    public static List<FriendlyUnit> UnitsInGame;
    // Units come onto the path one after another, otherwise those of several towers would walk inside each other
    private static Queue<FriendlyUnit> UnitsToSummon;
    private static float UnitSpawnCooldown;
    private const float UnitSpawnSpacing = 0.6f;
    public static int Lives;
    public static int MaxLives;
    public static int Money;
    public static bool IsGameOver;
    public static bool HasWon;
    // 0 before the first wave, then the number of the current or last finished wave
    public static int CurrentWave;
    public static int TotalWaves;
    public static bool IsWaveRunning;
    // Last finished wave and the bonus money it gave, the HUD shows a message when this changes
    public static int LastCompletedWave;
    public static int LastWaveBonus;
    public static int LastWaveCoins;
    // Extra money for skipping, 0 when the last wave wasn't skipped
    public static int LastSkipBonus;
    public static WaveEnd LastWaveEnd;
    // Seconds into the current wave, skipping is possible after SkipDelay
    public static float WaveTime;
    public const float SkipDelay = 15f;
    // Part of the bonus of a wave that comes on top of it for skipping
    private const float SkipBonusPart = 0.25f;
    // Money from the towers with MoneyPerWave
    public static int LastFarmMoney;
    // All coins earned since the start of this game, shown on the end screens
    public static int CoinsEarned;
    // Part of CoinsEarned for winning this map for the first time
    public static int FirstWinBonus;
    public static bool IsPaused;
    private static GameLoopManager Instance;
   private static Queue<Enemy> EnemiesToRemove;    
    private static Queue<int> EnemyIDsToSummon;
    private static Queue<SummonRequest> SummonRequests;
    private static Queue<EnemyDamageData> DamageData;
    public Transform NodeParent;
    public bool LoopShouldEnd;
    public int StartingLives = 20;
    public int StartingMoney = 250;
    // Every WaveData in Resources/Waves, played in the order of the number in their name (Wave 1, Wave 2, ...)
    private WaveData[] Waves;
    // Seconds between two waves when they start by themselves
    public float AutoStartDelay = 2f;

    // Waves that are still spawning, more than one after skipping
    private int SpawningWaves;

    //Start is called before the first frame update
   private void Start()
    {
        EnemyIDsToSummon = new Queue<int>();
        SummonRequests = new Queue<SummonRequest>();
        EnemiesToRemove = new Queue<Enemy>();
        DamageData = new Queue<EnemyDamageData>();
        TowersInGame = new List<TowerBehaviour>();
        UnitsInGame = new List<FriendlyUnit>();
        UnitsToSummon = new Queue<FriendlyUnit>();
        UnitSpawnCooldown = 0f;
        Lives = StartingLives;
        MaxLives = StartingLives;
        Money = StartingMoney;
        IsGameOver = false;
        HasWon = false;
        CurrentWave = 0;
        LastCompletedWave = 0;
        LastWaveBonus = 0;
        LastWaveCoins = 0;
        LastSkipBonus = 0;
        LastWaveEnd = WaveEnd.Cleared;
        WaveTime = 0f;
        LastFarmMoney = 0;
        CoinsEarned = 0;
        FirstWinBonus = 0;
        Waves = LoadWaves();
        TotalWaves = Waves.Length;
        IsWaveRunning = false;
        IsPaused = false;
        // Loading the scene again doesn't reset the pause by itself
        ApplyTimeScale();
        Instance = this;
        EntitySummoner.Init();

        Transform MapNodes = MapManager.Load(NodeParent);
        NodePositions = new Vector3[MapNodes.childCount];
        for(int i = 0; i < NodePositions.Length; i++)
        {
            NodePositions[i] = MapNodes.GetChild(i).position;
        }
        gameObject.AddComponent<PathIndicator>();
        gameObject.AddComponent<DamageNumbers>();

        StartCoroutine(GameLoop());
    }

    public static void SetPaused(bool Paused)
    {
        IsPaused = Paused;
        ApplyTimeScale();
    }

    private static void ApplyTimeScale()
    {
        Time.timeScale = IsPaused ? 0f : GameSettings.GameSpeed;
    }

    public static void SetAutoSkipWaves(bool AutoSkip)
    {
        GameSettings.AutoSkipWaves = AutoSkip;

        // Turned on between two waves, so no need to wait for the button.
        // The player always starts the first wave.
        if (AutoSkip && CurrentWave > 0)
        {
            StartNextWave();
        }
    }

    private static WaveData[] LoadWaves()
    {
        WaveData[] waves = Resources.LoadAll<WaveData>("Waves");
        System.Array.Sort(waves, (a, b) => GetWaveNumber(a).CompareTo(GetWaveNumber(b)));
        return waves;
    }

    // "Wave 12" gives 12, so Wave 10 comes after Wave 9 and not after Wave 1
    private static int GetWaveNumber(WaveData wave)
    {
        string digits = System.Text.RegularExpressions.Regex.Match(wave.name, @"\d+").Value;
        return digits.Length > 0 ? int.Parse(digits) : int.MaxValue;
    }

    public static bool CanStartNextWave()
    {
        return !IsWaveRunning && !IsGameOver && !HasWon && CurrentWave < TotalWaves;
    }

    public static void StartNextWave()
    {
        if (!CanStartNextWave())
        {
            return;
        }

        Instance.BeginNextWave();
    }

    // The last wave has no next wave, so no time limit and nothing to skip to
    public static bool HasTimeLimit => IsWaveRunning && !IsGameOver && !HasWon && CurrentWave < TotalWaves;
    public static float WaveTimeLeft => HasTimeLimit ? Mathf.Max(0f, Instance.Waves[CurrentWave - 1].TimeLimit - WaveTime) : 0f;
    public static int SkipBonus => HasTimeLimit ? Mathf.RoundToInt(Instance.Waves[CurrentWave - 1].CompletionBonus * SkipBonusPart) : 0;

    public static bool CanSkipWave()
    {
        return HasTimeLimit && WaveTime >= SkipDelay;
    }

    // The next wave starts right away, the enemies that are still on the map stay
    public static void SkipWave()
    {
        if (CanSkipWave())
        {
            Instance.PayWaveReward(WaveEnd.Skipped);
            Instance.BeginNextWave();
        }
    }

    private void BeginNextWave()
    {
        CurrentWave++;
        IsWaveRunning = true;
        WaveTime = 0f;
        StartCoroutine(SpawnWave(Waves[CurrentWave - 1]));
    }

    private IEnumerator SpawnWave(WaveData Wave)
    {
        SpawningWaves++;

        foreach (WaveGroup Group in Wave.Groups)
        {
            if (Group.DelayBefore > 0f)
            {
                yield return new WaitForSeconds(Group.DelayBefore);
            }

            for (int i = 0; i < Group.Count; i++)
            {
                EnqueueEnemyIDToSummon(Group.EnemyID);
                yield return new WaitForSeconds(Group.SpawnInterval);
            }
        }

        SpawningWaves--;
    }

    // The wave is over when everything has spawned and no enemy is left on the map
    private void CheckWaveFinished()
    {
        if (!IsWaveRunning || SpawningWaves > 0 || EnemyIDsToSummon.Count > 0 || SummonRequests.Count > 0 || EntitySummoner.EnemiesInGame.Count > 0)
        {
            return;
        }

        IsWaveRunning = false;
        UnitsToSummon.Clear();
        PayWaveReward(WaveEnd.Cleared);

        if (CurrentWave >= TotalWaves)
        {
            HasWon = true;
            if (MapManager.MarkWon())
            {
                FirstWinBonus = MapManager.FirstWinCoins;
                CoinWallet.Add(FirstWinBonus);
                CoinsEarned += FirstWinBonus;
            }
            LoopShouldEnd = true;
        }
        else if (GameSettings.AutoSkipWaves)
        {
            StartCoroutine(AutoStartNextWave());
        }
    }

    // Starts the next wave over the current one when the time is up, or when it may be skipped with auto skip on
    private void UpdateWaveTime()
    {
        if (!HasTimeLimit)
        {
            return;
        }

        WaveTime += Time.deltaTime;
        if (GameSettings.AutoSkipWaves && CanSkipWave())
        {
            SkipWave();
        }
        else if (WaveTimeLeft <= 0f)
        {
            PayWaveReward(WaveEnd.TimeUp);
            BeginNextWave();
        }
    }

    // Every wave pays once: when all enemies are gone, or earlier when the next wave starts over it
    private void PayWaveReward(WaveEnd End)
    {
        LastWaveEnd = End;
        LastSkipBonus = End == WaveEnd.Skipped ? SkipBonus : 0;
        LastWaveBonus = Waves[CurrentWave - 1].CompletionBonus;
        LastWaveCoins = Waves[CurrentWave - 1].CoinReward;
        LastCompletedWave = CurrentWave;
        LastFarmMoney = 0;
        foreach (TowerBehaviour Tower in TowersInGame)
        {
            LastFarmMoney += Tower.MoneyPerWave;
        }
        AddMoney(LastWaveBonus + LastSkipBonus + LastFarmMoney);
        CoinWallet.Add(LastWaveCoins);
        CoinsEarned += LastWaveCoins;
    }

    private IEnumerator AutoStartNextWave()
    {
        yield return new WaitForSeconds(AutoStartDelay);

        // Could have been turned off or started by hand in the meantime
        if (GameSettings.AutoSkipWaves)
        {
            StartNextWave();
        }
    }

    IEnumerator GameLoop()
    {
        while(LoopShouldEnd == false)
        {
            //spawn enemies

            while (EnemyIDsToSummon.Count > 0)
            {
                EntitySummoner.SummonEnemy(EnemyIDsToSummon.Dequeue());
            }

            // Enemies summoned by other enemies start where their summoner is
            while (SummonRequests.Count > 0)
            {
                SummonRequest Request = SummonRequests.Dequeue();
                Enemy Summoned = EntitySummoner.SummonEnemy(Request.EnemyID);
                if (Summoned != null)
                {
                    Summoned.transform.position = Request.Position;
                    Summoned.NodeIndex = Request.NodeIndex;
                }
            }

            //spawn towers

            //Move enemies

            NativeArray<Vector3> NodesToUse = new NativeArray<Vector3>(NodePositions, Allocator.TempJob);
            NativeArray<float> EnemySpeeds = new NativeArray<float>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            NativeArray<int> NodeIndices = new NativeArray<int>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            TransformAccessArray EnemyAccess = new TransformAccessArray(EntitySummoner.EnemiesInGameTransform.ToArray(), 2);

            for(int i =0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EnemySpeeds[i] = EntitySummoner.EnemiesInGame[i].CurrentSpeed;
                NodeIndices[i] = EntitySummoner.EnemiesInGame[i].NodeIndex;
            }

            MoveEnemiesJob MoveJob = new MoveEnemiesJob
            {
                NodePositions = NodesToUse,
                EnemySpeed = EnemySpeeds,
                NodeIndex = NodeIndices,
                deltaTime = Time.deltaTime
            };

            JobHandle MoveJobHandle = MoveJob.Schedule(EnemyAccess);
            MoveJobHandle.Complete();

            for(int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EntitySummoner.EnemiesInGame[i].NodeIndex = NodeIndices[i];

                if(EntitySummoner.EnemiesInGame[i].NodeIndex == NodePositions.Length)
                {
                    Lives = Mathf.Max(0, Lives - EntitySummoner.EnemiesInGame[i].LivesDamage);
                    EnqueueEnemyToRemove(EntitySummoner.EnemiesInGame[i]);
                }
            }

            NodesToUse.Dispose();
            EnemySpeeds.Dispose();
            NodeIndices.Dispose();
            EnemyAccess.Dispose();

            //tick towers

            UpdateFireRateBuffs();
            for (int i = 0; i < TowersInGame.Count; i++)
            {
                TowersInGame[i].Tick();
            }

            //Apply effects

            for (int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EntitySummoner.EnemiesInGame[i].TickEffects(Time.deltaTime);
            }

            //Damage enemies

            while (DamageData.Count > 0)
            {
                EnemyDamageData CurrentDamageData = DamageData.Dequeue();
                DamageEnemy(CurrentDamageData.TargetedEnemy, CurrentDamageData.TotalDamage, CurrentDamageData.Type);
            }

            //Spawn and tick friendly units

            UnitSpawnCooldown -= Time.deltaTime;
            if (UnitsToSummon.Count > 0 && UnitSpawnCooldown <= 0f)
            {
                UnitSpawnCooldown = UnitSpawnSpacing;
                FriendlyUnit Unit = Instantiate(UnitsToSummon.Dequeue());
                Unit.Init();
                UnitsInGame.Add(Unit);
            }

            for (int i = UnitsInGame.Count - 1; i >= 0; i--)
            {
                if (!UnitsInGame[i].Tick(Time.deltaTime))
                {
                    Destroy(UnitsInGame[i].gameObject);
                    UnitsInGame.RemoveAt(i);
                }
            }

            //Remove enemies


            while (EnemiesToRemove.Count > 0)
            {
                EntitySummoner.RemoveEnemy(EnemiesToRemove.Dequeue());
            }

            //Remove towers

            if (Lives <= 0)
            {
                GameOver();
            }
            else
            {
                CheckWaveFinished();
                UpdateWaveTime();
            }

            yield return null;
        }
    }

    public static int CountTowers(TowerData Data)
    {
        int Count = 0;
        foreach (TowerBehaviour Tower in TowersInGame)
        {
            if (Tower.Data == Data)
            {
                Count++;
            }
        }
        return Count;
    }

    // Enough money, and the limits of this tower and of the whole map aren't reached
    public static bool CanPlaceTower(TowerData Data)
    {
        return Money >= Data.Cost && TowersInGame.Count < MaxTowers
            && (Data.MaxPlaced <= 0 || CountTowers(Data) < Data.MaxPlaced);
    }

    // Every tower gets the strongest buff that reaches it, they don't add up
    private static void UpdateFireRateBuffs()
    {
        foreach (TowerBehaviour Tower in TowersInGame)
        {
            Tower.ReceivedBuff = 0f;
        }

        foreach (TowerBehaviour Booster in TowersInGame)
        {
            if (Booster.FireRateBuff <= 0f || Booster.IsStunned)
            {
                continue;
            }

            foreach (TowerBehaviour Tower in TowersInGame)
            {
                Vector3 Offset = Tower.transform.position - Booster.transform.position;
                Offset.y = 0f;
                if (Offset.sqrMagnitude <= Booster.Range * Booster.Range)
                {
                    Tower.ReceivedBuff = Mathf.Max(Tower.ReceivedBuff, Booster.FireRateBuff);
                }
            }
        }
    }

    // Returns how much of the damage was used, less than Damage when the enemy had less health left than that.
    // What the enemy resists counts as used.
    public static float DamageEnemy(Enemy TargetedEnemy, float Damage, DamageType Type)
    {
        // Skip enemies that already died or reached the end this frame, so they aren't removed twice
        if (TargetedEnemy.Health <= 0f || TargetedEnemy.NodeIndex >= NodePositions.Length)
        {
            return 0f;
        }

        float Multiplier = TargetedEnemy.GetDamageMultiplier(Type);
        float Dealt = Damage * Multiplier;
        float Used = Damage;
        if (Dealt > TargetedEnemy.Health)
        {
            Dealt = TargetedEnemy.Health;
            Used = Dealt / Multiplier;
        }

        TargetedEnemy.Health -= Dealt;
        if (GameSettings.DamageNumbers)
        {
            DamageNumbers.Show(TargetedEnemy.AimPoint, Dealt, Multiplier < 1f);
        }

        if (TargetedEnemy.Health <= 0f)
        {
            AddMoney(TargetedEnemy.MoneyReward);
            EnqueueEnemyToRemove(TargetedEnemy);
        }
        return Used;
    }

    public static void SummonUnit(FriendlyUnit Prefab)
    {
        UnitsToSummon.Enqueue(Prefab);
    }

    private void GameOver()
    {
        IsGameOver = true;
        LoopShouldEnd = true;
        // Also stops the waves that are still spawning
        StopAllCoroutines();
    }

    public static void EnqueueSummonAt(int ID, Vector3 Position, int NodeIndex)
    {
        SummonRequests.Enqueue(new SummonRequest { EnemyID = ID, Position = Position, NodeIndex = NodeIndex });
    }

    public static void EnqueueEnemyIDToSummon(int ID)
    {
        EnemyIDsToSummon.Enqueue(ID);
    }

    public static void EnqueueEnemyToRemove(Enemy EnemyToRemove)
    {
       EnemiesToRemove.Enqueue(EnemyToRemove);
    }

    public static void AddMoney(int Amount)
    {
        Money += Amount;
    }

    // Returns false and spends nothing when there isn't enough money
    public static bool TrySpendMoney(int Amount)
    {
        if (Money < Amount)
        {
            return false;
        }
        Money -= Amount;
        return true;
    }

    public static void EnqueueDamageData(EnemyDamageData DamageToApply)
    {
        DamageData.Enqueue(DamageToApply);
    }
}

public struct SummonRequest
{
    public int EnemyID;
    public Vector3 Position;
    public int NodeIndex;
}

public struct EnemyDamageData
{
    public Enemy TargetedEnemy;
    public float TotalDamage;
    public DamageType Type;

    public EnemyDamageData(Enemy Target, float Damage, DamageType DamageType)
    {
        TargetedEnemy = Target;
        TotalDamage = Damage;
        Type = DamageType;
    }
}
public struct MoveEnemiesJob : IJobParallelForTransform
{
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> NodePositions;
    [NativeDisableParallelForRestriction]
    public NativeArray<float> EnemySpeed;
    [NativeDisableParallelForRestriction]
    public NativeArray<int> NodeIndex;
 
    public float deltaTime;

    public void Execute (int index, TransformAccess Transform)
    {
        if(NodeIndex[index] < NodePositions.Length)
        {
            Vector3 PositionToMoveTo = NodePositions[NodeIndex[index]];
            Transform.position = Vector3.MoveTowards(Transform.position, PositionToMoveTo, EnemySpeed[index] * deltaTime);

            if(Transform.position == PositionToMoveTo)
            {
                NodeIndex[index]++;
            }
        }
    }
}

// Why the last wave paid its reward
public enum WaveEnd
{
    Cleared,
    Skipped,
    TimeUp
}
