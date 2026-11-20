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
    public static bool IsPaused;
    public static bool IsFastForward;
    // Starts the next wave by itself when the current one is over
    public static bool AutoStartWaves;
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
    // Game speed with fast forward on
    public float FastForwardSpeed = 2f;
    // Seconds between two waves when they start by themselves
    public float AutoStartDelay = 2f;

    private Coroutine SpawnRoutine;
    private bool IsSpawning;

    //Start is called before the first frame update
   private void Start()
    {
        EnemyIDsToSummon = new Queue<int>();
        SummonRequests = new Queue<SummonRequest>();
        EnemiesToRemove = new Queue<Enemy>();
        DamageData = new Queue<EnemyDamageData>();
        TowersInGame = new List<TowerBehaviour>();
        Lives = StartingLives;
        MaxLives = StartingLives;
        Money = StartingMoney;
        IsGameOver = false;
        HasWon = false;
        CurrentWave = 0;
        LastCompletedWave = 0;
        LastWaveBonus = 0;
        Waves = LoadWaves();
        TotalWaves = Waves.Length;
        IsWaveRunning = false;
        IsPaused = false;
        IsFastForward = false;
        AutoStartWaves = false;
        // Loading the scene again doesn't reset the game speed by itself
        Time.timeScale = 1f;
        Instance = this;
        EntitySummoner.Init();

        NodePositions = new Vector3[NodeParent.childCount];
        for(int i = 0; i < NodePositions.Length; i++)
        {
            NodePositions[i] = NodeParent.GetChild(i).position;
        }

        StartCoroutine(GameLoop());
    }

    public static void SetPaused(bool Paused)
    {
        IsPaused = Paused;
        ApplyTimeScale();
    }

    public static void SetFastForward(bool FastForward)
    {
        IsFastForward = FastForward;
        ApplyTimeScale();
    }

    private static void ApplyTimeScale()
    {
        Time.timeScale = IsPaused ? 0f : (IsFastForward ? Instance.FastForwardSpeed : 1f);
    }

    public static void SetAutoStartWaves(bool AutoStart)
    {
        AutoStartWaves = AutoStart;

        // Turned on between two waves, so no need to wait for the button
        if (AutoStart)
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

        CurrentWave++;
        IsWaveRunning = true;
        Instance.SpawnRoutine = Instance.StartCoroutine(Instance.SpawnWave(Instance.Waves[CurrentWave - 1]));
    }

    private IEnumerator SpawnWave(WaveData Wave)
    {
        IsSpawning = true;

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

        IsSpawning = false;
    }

    // The wave is over when everything has spawned and no enemy is left on the map
    private void CheckWaveFinished()
    {
        if (!IsWaveRunning || IsSpawning || EnemyIDsToSummon.Count > 0 || SummonRequests.Count > 0 || EntitySummoner.EnemiesInGame.Count > 0)
        {
            return;
        }

        IsWaveRunning = false;
        LastWaveBonus = Waves[CurrentWave - 1].CompletionBonus;
        LastCompletedWave = CurrentWave;
        AddMoney(LastWaveBonus);

        if (CurrentWave >= TotalWaves)
        {
            HasWon = true;
            LoopShouldEnd = true;
        }
        else if (AutoStartWaves)
        {
            StartCoroutine(AutoStartNextWave());
        }
    }

    private IEnumerator AutoStartNextWave()
    {
        yield return new WaitForSeconds(AutoStartDelay);

        // Could have been turned off or started by hand in the meantime
        if (AutoStartWaves)
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
                Enemy TargetedEnemy = CurrentDamageData.TargetedEnemy;

                // Skip enemies that already died or reached the end this frame, so they aren't removed twice
                if (TargetedEnemy.Health <= 0f || TargetedEnemy.NodeIndex >= NodePositions.Length)
                {
                    continue;
                }

                TargetedEnemy.Health -= CurrentDamageData.TotalDamage;

                if (TargetedEnemy.Health <= 0f)
                {
                    AddMoney(TargetedEnemy.MoneyReward);
                    EnqueueEnemyToRemove(TargetedEnemy);
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
            }

            yield return null;
        }
    }

    private void GameOver()
    {
        IsGameOver = true;
        LoopShouldEnd = true;
        if (SpawnRoutine != null)
        {
            StopCoroutine(SpawnRoutine);
        }
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

    public EnemyDamageData(Enemy Target, float Damage)
    {
        TargetedEnemy = Target;
        TotalDamage = Damage;
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
