using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class EntitySummoner : MonoBehaviour
{
    public static List<Enemy> EnemiesInGame;
    public static Dictionary<int, GameObject> EnemyPrefabs;
    public static Dictionary<int, Queue<Enemy>> EnemyObjectPools;

    private static bool IsInitialized;
    public static void Init()
    {
        if (!IsInitialized)
        {
            EnemyPrefabs = new Dictionary<int, GameObject>();
            EnemyObjectPools = new Dictionary<int, Queue<Enemy>>();
            EnemiesInGame = new List<Enemy>();

            EnemySummonData[] Enemies = Resources.LoadAll<EnemySummonData>("Enemies");

            foreach (EnemySummonData enemy in Enemies)
            {
                EnemyPrefabs.Add(enemy.EnemyID, enemy.EnemyPrefab);
                EnemyObjectPools.Add(enemy.EnemyID, new Queue<Enemy>());
            }
            IsInitialized = true;
        }
        else
        {
            Debug.Log("EntitySummoner is already initialized.");
        }

    }

    public static Enemy SummonEnemy(int EnemyID)
    {
        Enemy SummonedEnemy = null;

        if (EnemyPrefabs.ContainsKey(EnemyID)) 
        { 
            Queue<Enemy> ReferencedQueue = EnemyObjectPools[EnemyID];

            if (ReferencedQueue.Count > 0)
            {
             //Dequeue Enemy and initialize 

                SummonedEnemy = ReferencedQueue.Dequeue();
                SummonedEnemy.Init();

                SummonedEnemy.gameObject.SetActive(true);
            }
            else
            {
                //Instantiate new instance of enemy and initialize
                GameObject NewEnemy = Instantiate ( EnemyPrefabs[EnemyID], Vector3.zero, Quaternion.identity);
                SummonedEnemy = NewEnemy.GetComponent<Enemy>();
                SummonedEnemy.Init();
            }
        }
        else
        {
            Debug.Log($"ENTITYSUMMONER: ENEMY PREFAB NOT FOUND FOR ID {EnemyID}");
            return null;
        }
        EnemiesInGame.Add(SummonedEnemy);
        SummonedEnemy.ID = EnemyID;
        return SummonedEnemy;
    }
    public static void RemoveEnemy(Enemy EnemyToRemove)
    {
        EnemyObjectPools[EnemyToRemove.ID].Enqueue(EnemyToRemove);
        EnemyToRemove.gameObject.SetActive(false);
        EnemiesInGame.Remove(EnemyToRemove);
    }
}
