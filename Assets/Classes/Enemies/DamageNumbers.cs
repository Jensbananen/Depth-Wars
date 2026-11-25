using UnityEngine;
using TMPro;

// Numbers that float up from a hit enemy, see DamageNumbers in the settings. Added by the GameLoopManager.
public class DamageNumbers : MonoBehaviour
{
    // With more hits at once the oldest number is reused
    private const int MaxNumbers = 60;
    private const float Duration = 0.7f;
    private const float RiseSpeed = 1.5f;

    private static readonly Color NormalColor = new Color(1f, 0.85f, 0.2f);
    private static readonly Color ResistedColor = new Color(0.7f, 0.7f, 0.7f);

    private static DamageNumbers Instance;

    private readonly TextMeshPro[] Texts = new TextMeshPro[MaxNumbers];
    private readonly float[] TimeLeft = new float[MaxNumbers];
    private int Next;
    private Camera PlayerCamera;

    void Awake()
    {
        Instance = this;
    }

    // Grey when the enemy resisted a part of it
    public static void Show(Vector3 position, float damage, bool resisted)
    {
        if (Instance != null)
        {
            Instance.Spawn(position, damage, resisted);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerCamera == null)
        {
            PlayerCamera = FindAnyObjectByType<Camera>();
        }

        for (int i = 0; i < MaxNumbers; i++)
        {
            if (TimeLeft[i] <= 0f)
            {
                continue;
            }

            TimeLeft[i] -= Time.deltaTime;
            if (TimeLeft[i] <= 0f)
            {
                Texts[i].gameObject.SetActive(false);
                continue;
            }

            Texts[i].transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);
            if (PlayerCamera != null)
            {
                Texts[i].transform.rotation = PlayerCamera.transform.rotation;
            }
            Texts[i].alpha = Mathf.Clamp01(TimeLeft[i] / Duration * 2f);
        }
    }

    private void Spawn(Vector3 position, float damage, bool resisted)
    {
        int index = Next;
        Next = (Next + 1) % MaxNumbers;

        if (Texts[index] == null)
        {
            Texts[index] = CreateText();
        }

        // A little to the side, so numbers of quick hits don't cover each other
        Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), 0.4f, Random.Range(-0.3f, 0.3f));
        Texts[index].transform.position = position + offset;
        Texts[index].text = damage.ToString("0.#");
        Texts[index].color = resisted ? ResistedColor : NormalColor;
        Texts[index].gameObject.SetActive(true);
        TimeLeft[index] = Duration;
    }

    private TextMeshPro CreateText()
    {
        GameObject textObject = new GameObject("Damage Number");
        textObject.transform.SetParent(transform, false);

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = new Vector2(4f, 1f);
        text.fontSize = 5f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }
}
