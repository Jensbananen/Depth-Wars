using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New EnemySummonData", menuName = "Create EnemySummonData")]
public class EnemySummonData : ScriptableObject
{
   public GameObject EnemyPrefab;
    public int EnemyID;
}
