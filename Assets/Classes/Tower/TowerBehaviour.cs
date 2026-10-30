using UnityEngine;

public class TowerBehaviour : MonoBehaviour
{
    // Set by TowerPlacement when the tower is placed
    [HideInInspector] public TowerData Data;

    public float Range = 5f;
    // Shots per second
    public float FireRate = 1f;
    public float Damage = 10f;

    // Part of the tower that turns towards the target, the shot also starts here
    [SerializeField] private Transform HeadPivot;
    [SerializeField] private float TurnSpeed = 720f;
    [SerializeField] private Color ShotColor = new Color(1f, 0.9f, 0.4f);
    [SerializeField] private float ShotDuration = 0.05f;

    // Aim at the middle of the enemy instead of its feet
    private const float AimHeight = 0.5f;

    private Enemy Target;
    private float FireCooldown;
    private LineRenderer ShotLine;
    private float ShotTimeLeft;

    // Called by the game loop every frame for placed towers
    public void Tick()
    {
        float deltaTime = Time.deltaTime;
        FireCooldown -= deltaTime;

        UpdateShotLine(deltaTime);

        Target = FindTarget();
        if (Target == null)
        {
            return;
        }

        Vector3 aimPoint = Target.transform.position + Vector3.up * AimHeight;
        TurnTowards(aimPoint, deltaTime);

        if (FireCooldown <= 0f)
        {
            Fire(Target, aimPoint);
            FireCooldown = 1f / FireRate;
        }
    }

    // The enemy in range that is furthest along the path
    private Enemy FindTarget()
    {
        Enemy best = null;
        int bestNodeIndex = -1;
        float bestDistanceToNode = float.MaxValue;
        float rangeSqr = Range * Range;
        Vector3 position = transform.position;

        foreach (Enemy enemy in EntitySummoner.EnemiesInGame)
        {
            Vector3 offset = enemy.transform.position - position;
            offset.y = 0f;
            if (offset.sqrMagnitude > rangeSqr)
            {
                continue;
            }

            int nodeIndex = enemy.NodeIndex;
            float distanceToNode = nodeIndex < GameLoopManager.NodePositions.Length
                ? (GameLoopManager.NodePositions[nodeIndex] - enemy.transform.position).sqrMagnitude
                : 0f;

            if (nodeIndex > bestNodeIndex || (nodeIndex == bestNodeIndex && distanceToNode < bestDistanceToNode))
            {
                best = enemy;
                bestNodeIndex = nodeIndex;
                bestDistanceToNode = distanceToNode;
            }
        }

        return best;
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
        GameLoopManager.EnqueueDamageData(new EnemyDamageData(target, Damage));

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
        line.widthMultiplier = 0.05f;
        line.startColor = ShotColor;
        line.endColor = ShotColor;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }
}
