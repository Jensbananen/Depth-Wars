using UnityEngine;
using TMPro;

// Arrows that move along the path and a START and END sign, so the player sees where the enemies
// come from and go to. Visible as long as no wave is running. Added by the GameLoopManager.
public class PathIndicator : MonoBehaviour
{
    private const float ArrowSpacing = 2.5f;
    private const float ArrowSpeed = 3f;
    private const float ArrowSize = 0.35f;
    // Just above the path
    private const float ArrowHeight = 0.12f;
    private const float SignHeight = 1.6f;

    private static readonly Color ArrowColor = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color StartColor = new Color(0.2f, 0.9f, 0.3f);
    private static readonly Color EndColor = new Color(1f, 0.25f, 0.25f);

    private GameObject Root;
    private Transform[] Arrows;
    private Transform StartSign;
    private Transform EndSign;
    private Camera PlayerCamera;
    private float PathLength;
    private float Offset;

    void Start()
    {
        Root = new GameObject("Path Indicator");

        Vector3[] nodes = GameLoopManager.NodePositions;
        for (int i = 1; i < nodes.Length; i++)
        {
            PathLength += Vector3.Distance(nodes[i - 1], nodes[i]);
        }

        Arrows = new Transform[Mathf.FloorToInt(PathLength / ArrowSpacing)];
        for (int i = 0; i < Arrows.Length; i++)
        {
            Arrows[i] = CreateArrow();
        }

        StartSign = CreateSign("START", nodes[0], StartColor);
        EndSign = CreateSign("END", nodes[nodes.Length - 1], EndColor);
    }

    // Update is called once per frame
    void Update()
    {
        bool visible = !GameLoopManager.IsWaveRunning && !GameLoopManager.IsGameOver && !GameLoopManager.HasWon && !MainMenu.IsOpen;
        if (Root.activeSelf != visible)
        {
            Root.SetActive(visible);
        }
        if (!visible)
        {
            return;
        }

        // Real time, so the arrows keep the same speed while paused and at 2x
        Offset = (Offset + ArrowSpeed * Time.unscaledDeltaTime) % ArrowSpacing;
        for (int i = 0; i < Arrows.Length; i++)
        {
            PlaceOnPath(Arrows[i], Offset + i * ArrowSpacing);
        }

        if (PlayerCamera == null)
        {
            PlayerCamera = FindAnyObjectByType<Camera>();
        }
        if (PlayerCamera != null)
        {
            StartSign.rotation = PlayerCamera.transform.rotation;
            EndSign.rotation = PlayerCamera.transform.rotation;
        }
    }

    // Puts the arrow this far along the path, pointing in the walking direction
    private void PlaceOnPath(Transform arrow, float distance)
    {
        Vector3[] nodes = GameLoopManager.NodePositions;
        for (int i = 1; i < nodes.Length; i++)
        {
            Vector3 segment = nodes[i] - nodes[i - 1];
            float length = segment.magnitude;
            if (length > 0f && (distance <= length || i == nodes.Length - 1))
            {
                Vector3 direction = segment / length;
                arrow.position = nodes[i - 1] + direction * Mathf.Min(distance, length) + Vector3.up * ArrowHeight;
                arrow.rotation = Quaternion.LookRotation(direction);
                return;
            }
            distance -= length;
        }
    }

    // A flat > shape
    private Transform CreateArrow()
    {
        GameObject arrowObject = new GameObject("Arrow");
        arrowObject.transform.SetParent(Root.transform, false);

        LineRenderer line = arrowObject.AddComponent<LineRenderer>();
        line.sharedMaterial = LineMaterial.Get();
        line.useWorldSpace = false;
        line.positionCount = 3;
        line.SetPosition(0, new Vector3(-ArrowSize, 0f, -ArrowSize));
        line.SetPosition(1, Vector3.zero);
        line.SetPosition(2, new Vector3(ArrowSize, 0f, -ArrowSize));
        line.widthMultiplier = 0.12f;
        line.startColor = ArrowColor;
        line.endColor = ArrowColor;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return arrowObject.transform;
    }

    // A ring on the path with text above it
    private Transform CreateSign(string label, Vector3 position, Color color)
    {
        CircleIndicator ring = CircleIndicator.Create(label + " Ring", 0.12f);
        ring.transform.SetParent(Root.transform, false);
        ring.transform.position = position;
        ring.SetRadius(0.9f);
        ring.SetColor(color);

        GameObject textObject = new GameObject(label + " Text");
        textObject.transform.SetParent(Root.transform, false);
        textObject.transform.position = position + Vector3.up * SignHeight;

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = new Vector2(6f, 2f);
        text.text = label;
        text.fontSize = 9f;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return textObject.transform;
    }
}
