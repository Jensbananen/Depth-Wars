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
   private static Queue<Enemy> EnemiesToRemove;    
    private static Queue<int> EnemyIDsToSummon;
    private static Queue<EnemyDamageData> DamageData;
    public Transform NodeParent;
    public bool LoopShouldEnd;
    public int StartingLives = 20;
    public int StartingMoney = 250;

    //Start is called before the first frame update
   private void Start()
    {
        EnemyIDsToSummon = new Queue<int>();
        EnemiesToRemove = new Queue<Enemy>();
        DamageData = new Queue<EnemyDamageData>();
        TowersInGame = new List<TowerBehaviour>();
        Lives = StartingLives;
        MaxLives = StartingLives;
        Money = StartingMoney;
        IsGameOver = false;
        EntitySummoner.Init();

        NodePositions = new Vector3[NodeParent.childCount];
        for(int i = 0; i < NodePositions.Length; i++)
        {
            NodePositions[i] = NodeParent.GetChild(i).position;
        }

        StartCoroutine(GameLoop());
        InvokeRepeating("SummonTest", 0f, 1f);

    }
 
    void SummonTest()
    {
        EnqueueEnemyIDToSummon(1);
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

            //spawn towers

            //Move enemies

            NativeArray<Vector3> NodesToUse = new NativeArray<Vector3>(NodePositions, Allocator.TempJob);
            NativeArray<float> EnemySpeeds = new NativeArray<float>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            NativeArray<int> NodeIndices = new NativeArray<int>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            TransformAccessArray EnemyAccess = new TransformAccessArray(EntitySummoner.EnemiesInGameTransform.ToArray(), 2);

            for(int i =0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EnemySpeeds[i] = EntitySummoner.EnemiesInGame[i].Speed;
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

            yield return null;
        }
    }

    private void GameOver()
    {
        IsGameOver = true;
        LoopShouldEnd = true;
        CancelInvoke();
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
