using UnityEngine;

// Walks the path backwards, from the end to the start, and fights the enemies it runs into.
// Its health is also its damage: both lose the lowest health of the two.
public class FriendlyUnit : MonoBehaviour
{
    public string UnitName;
    public float Health = 10f;
    public float Speed = 3f;

    // Node it walks to
    private int NodeIndex;
    private float Radius;

    void Awake()
    {
        Renderer model = GetComponentInChildren<Renderer>();
        Radius = model != null ? model.bounds.extents.x : 0.5f;
    }

    public void Init()
    {
        int last = GameLoopManager.NodePositions.Length - 1;
        transform.position = GameLoopManager.NodePositions[last];
        NodeIndex = last - 1;
    }

    // Called by the game loop every frame, returns false when the unit is gone
    public bool Tick(float deltaTime)
    {
        if (!Move(Speed * deltaTime))
        {
            return false;
        }

        foreach (Enemy enemy in EntitySummoner.EnemiesInGame)
        {
            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;
            // Plus half of what they move in a frame, so they can't walk through each other at a high game speed
            float reach = Radius + enemy.Radius + (Speed + enemy.CurrentSpeed) * deltaTime * 0.5f;
            if (offset.sqrMagnitude > reach * reach)
            {
                continue;
            }

            float used = GameLoopManager.DamageEnemy(enemy, Health, DamageType.Collision);
            if (used > 0f)
            {
                SoundManager.Play(Sound.UnitHit);
            }
            Health -= used;
            if (Health <= 0f)
            {
                return false;
            }
        }
        return true;
    }

    // Returns false at the start of the path
    private bool Move(float distance)
    {
        while (NodeIndex >= 0)
        {
            Vector3 toNode = GameLoopManager.NodePositions[NodeIndex] - transform.position;
            float length = toNode.magnitude;
            if (length > distance)
            {
                transform.position += toNode / length * distance;
                transform.rotation = Quaternion.LookRotation(toNode);
                return true;
            }

            // Walks on to the next node with what is left
            transform.position = GameLoopManager.NodePositions[NodeIndex];
            distance -= length;
            NodeIndex--;
        }
        return false;
    }
}
