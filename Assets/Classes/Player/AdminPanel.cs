using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Temporary cheat panel for testing, opened with the ` key (left of 1).
// Only works in the Unity editor and development builds, never in a normal build.
public class AdminPanel : MonoBehaviour
{
    private GameObject Panel;
    private RectTransform Content;

    void Start()
    {
        if (!Debug.isDebugBuild)
        {
            enabled = false;
            return;
        }

        BuildPanel();
        AddButton("+ $1000", () => GameLoopManager.AddMoney(1000));
        AddButton("+ $10000", () => GameLoopManager.AddMoney(10000));
        AddButton("Full lives", () => GameLoopManager.Lives = GameLoopManager.MaxLives);
        AddButton("Kill all enemies", KillAllEnemies);
        AddButton("Skip a wave", SkipWave);
        AddButton("Start next wave", GameLoopManager.StartNextWave);
        Panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Panel.SetActive(!Panel.activeSelf);
            Panel.transform.SetAsLastSibling();
        }
    }

    // Goes through the normal damage step, so the kill money is given too
    private void KillAllEnemies()
    {
        foreach (Enemy enemy in EntitySummoner.EnemiesInGame)
        {
            GameLoopManager.EnqueueDamageData(new EnemyDamageData(enemy, float.MaxValue));
        }
    }

    // Jumps one wave ahead without playing it, only between waves
    private void SkipWave()
    {
        if (!GameLoopManager.IsWaveRunning && GameLoopManager.CurrentWave < GameLoopManager.TotalWaves - 1)
        {
            GameLoopManager.CurrentWave++;
        }
    }

    private void BuildPanel()
    {
        Panel = new GameObject("Admin Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Content = (RectTransform)Panel.transform;
        Content.SetParent(transform, false);
        Content.anchorMin = new Vector2(0f, 1f);
        Content.anchorMax = new Vector2(0f, 1f);
        Content.pivot = new Vector2(0f, 1f);
        Content.anchoredPosition = new Vector2(20f, -20f);
        Content.sizeDelta = new Vector2(300f, 0f);
        Panel.GetComponent<Image>().color = new Color(0.4f, 0f, 0f, 0.85f);

        VerticalLayoutGroup layout = Panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Grows with the number of buttons
        Panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = CreateText("Title", Content, 30f);
        title.text = "Admin  ( ` )";
        title.rectTransform.sizeDelta = new Vector2(0f, 40f);
    }

    private void AddButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.SetParent(Content, false);
        buttonRect.sizeDelta = new Vector2(0f, 50f);
        buttonObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        buttonObject.GetComponent<Button>().onClick.AddListener(onClick);

        TMP_Text text = CreateText("Text", buttonRect, 26f);
        text.text = label;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private TMP_Text CreateText(string name, Transform parent, float fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }
}
