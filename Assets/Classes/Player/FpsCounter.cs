using UnityEngine;
using TMPro;

// Frames per second in the top left, see ShowFps in the settings. Added by the SettingsMenu.
public class FpsCounter : MonoBehaviour
{
    private const float UpdateInterval = 0.5f;

    private TMP_Text Text;
    private int Frames;
    private float Elapsed;

    void Start()
    {
        GameObject textObject = new GameObject("FPS", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = (RectTransform)textObject.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(200f, 40f);
        rect.anchoredPosition = new Vector2(20f, -12f);

        Text = textObject.GetComponent<TMP_Text>();
        Text.fontSize = 28f;
        Text.fontStyle = FontStyles.Bold;
        Text.color = new Color(0.3f, 1f, 0.4f);
        Text.raycastTarget = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Text.gameObject.activeSelf != GameSettings.ShowFps)
        {
            Text.gameObject.SetActive(GameSettings.ShowFps);
        }

        // Average over half a second, real time so pause and game speed don't matter
        Frames++;
        Elapsed += Time.unscaledDeltaTime;
        if (Elapsed >= UpdateInterval)
        {
            Text.text = $"{Mathf.RoundToInt(Frames / Elapsed)} FPS";
            Frames = 0;
            Elapsed = 0f;
        }
    }
}
