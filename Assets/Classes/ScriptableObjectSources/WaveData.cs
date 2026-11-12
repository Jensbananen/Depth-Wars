using UnityEngine;

[CreateAssetMenu(fileName = "New WaveData", menuName = "Create WaveData")]
public class WaveData : ScriptableObject
{
    // Groups spawn one after another, e.g. first 10 normal enemies, then 5 fast ones
    public WaveGroup[] Groups;

    // Money the player gets when every enemy of this wave is gone
    public int CompletionBonus = 25;
}

[System.Serializable]
public class WaveGroup
{
    // Same ID as in the EnemySummonData
    public int EnemyID = 1;
    public int Count = 10;
    // Seconds between two enemies of this group
    public float SpawnInterval = 1f;
    // Seconds to wait before this group starts
    public float DelayBefore = 0f;
}
