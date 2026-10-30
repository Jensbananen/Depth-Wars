using UnityEngine;

// Flat circle on the ground, used for the placement area and the range of a tower
public class CircleIndicator : MonoBehaviour
{
    private const int Segments = 64;
    // Slightly above the ground so it doesn't flicker through it
    private const float HeightOffset = 0.03f;

    private static Material LineMaterial;
    private LineRenderer Line;

    public static CircleIndicator Create(string name, float lineWidth)
    {
        if (LineMaterial == null)
        {
            LineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        GameObject circleObject = new GameObject(name);
        CircleIndicator circle = circleObject.AddComponent<CircleIndicator>();

        circle.Line = circleObject.AddComponent<LineRenderer>();
        circle.Line.sharedMaterial = LineMaterial;
        circle.Line.useWorldSpace = false;
        circle.Line.loop = true;
        circle.Line.positionCount = Segments;
        circle.Line.widthMultiplier = lineWidth;
        circle.Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        circle.Line.receiveShadows = false;
        return circle;
    }

    public void SetRadius(float radius)
    {
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            Line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, HeightOffset, Mathf.Sin(angle) * radius));
        }
    }

    public void SetColor(Color color)
    {
        Line.startColor = color;
        Line.endColor = color;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
