using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;

public class GameLoopManager : MonoBehaviour
{
    public static Vector3[] NodePositions;
   private static Queue<Enemy> EnemiesToRemove;    
    private static Queue<int> EnemyIDsToSummon;
    public Transform NodeParent;
    public bool LoopShouldEnd;

    //Start is called before the first frame update
   private void Start()
    {
        EnemyIDsToSummon = new Queue<int>();
        EnemiesToRemove = new Queue<Enemy>();
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
                    EnqueueEnemyToRemove(EntitySummoner.EnemiesInGame[i]);
                }
            }

            NodesToUse.Dispose();
            EnemySpeeds.Dispose();
            NodeIndices.Dispose();
            EnemyAccess.Dispose();

            //tick towers

            //Apply effects

            //Damage enemies

            //Remove enemies


            while (EnemiesToRemove.Count > 0)
            {
                EntitySummoner.RemoveEnemy(EnemiesToRemove.Dequeue());
            }

            //Remove towers

            yield return null;
        }
    }

    public static void EnqueueEnemyIDToSummon(int ID)
    {
        EnemyIDsToSummon.Enqueue(ID);
    }

    public static void EnqueueEnemyToRemove(Enemy EnemyToRemove)
    {
       EnemiesToRemove.Enqueue(EnemyToRemove);
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
