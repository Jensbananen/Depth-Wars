using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLoopManager : MonoBehaviour
{
    private static Queue<int> EnemyIDsToSummon;
    public bool LoopShouldEnd;

    //Start is called before the first frame update
   private void Start()
    {
        EnemyIDsToSummon = new Queue<int>();
        EntitySummoner.Init();

        StartCoroutine(GameLoop());
        InvokeRepeating("SummonTest", 0f, 0.5f);
        InvokeRepeating("RemoveTest", 0f, 1f);

    }
    void RemoveTest()
    {
        if(EntitySummoner.EnemiesInGame.Count > 0)
        {
            EntitySummoner.RemoveEnemy(EntitySummoner.EnemiesInGame[Random.Range(0, EntitySummoner.EnemiesInGame.Count)]);
        }
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

            if(EnemyIDsToSummon.Count > 0)
            {
                for (int i = 0; i < EnemyIDsToSummon.Count; i++)
                {
                    EntitySummoner.SummonEnemy(EnemyIDsToSummon.Dequeue());
                }
            }

            //spawn towers

            //Move enemies

            //tick towers

            //Apply effects

            //Damage enemies

            //Remove enemies

            //Remove towers

            yield return null;
        }
    }

    public static void EnqueueEnemyIDToSummon(int ID)
    {
        EnemyIDsToSummon.Enqueue(ID);
    }
}
