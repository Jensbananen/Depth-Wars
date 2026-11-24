using UnityEngine;

// Bar above an enemy that shows its HP, see HealthBars in the settings. Added by the Enemy itself.
public class EnemyHealthBar : MonoBehaviour
{
    private const float Gap = 0.35f;
    private const float BackThickness = 0.18f;
    private const float FillThickness = 0.12f;

    private static Camera PlayerCamera;

    private Enemy Enemy;
    private float Height;
    private float Width;
    private LineRenderer Back;
    private LineRenderer Fill;

    void Awake()
    {
        Enemy = GetComponent<Enemy>();

        // Bigger enemies get a wider bar
        Renderer model = GetComponentInChildren<Renderer>();
        Height = (model != null ? model.bounds.max.y - transform.position.y : 1f) + Gap;
        Width = Mathf.Clamp(model != null ? model.bounds.size.x * 1.1f : 1f, 0.8f, 3f);
    }

    // LateUpdate so the bar follows the position of this frame
    void LateUpdate()
    {
        bool visible = GameSettings.HealthBars == HealthBarMode.Always
            || (GameSettings.HealthBars == HealthBarMode.Damaged && Enemy.Health < Enemy.MaxHealth);
        if (!visible)
        {
            if (Back != null && Back.enabled)
            {
                Back.enabled = false;
                Fill.enabled = false;
            }
            return;
        }

        if (PlayerCamera == null)
        {
            PlayerCamera = FindAnyObjectByType<Camera>();
        }
        if (Back == null)
        {
            Back = CreateLine("Health Bar Back", BackThickness, 0);
            Fill = CreateLine("Health Bar Fill", FillThickness, 1);
            Back.startColor = Back.endColor = new Color(0f, 0f, 0f, 0.8f);
        }
        Back.enabled = true;
        Fill.enabled = true;

        Vector3 right = PlayerCamera.transform.right;
        Vector3 left = transform.position + Vector3.up * Height - right * (Width * 0.5f);
        float fraction = Mathf.Clamp01(Enemy.Health / Enemy.MaxHealth);

        Back.SetPosition(0, left - right * 0.03f);
        Back.SetPosition(1, left + right * (Width + 0.03f));
        Fill.SetPosition(0, left);
        Fill.SetPosition(1, left + right * (Width * fraction));
        Fill.startColor = Fill.endColor = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.85f, 0.35f), fraction);
    }

    private LineRenderer CreateLine(string name, float thickness, int sortingOrder)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = LineMaterial.Get();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = thickness;
        line.sortingOrder = sortingOrder;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }
}
