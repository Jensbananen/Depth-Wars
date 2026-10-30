using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Lives bar at the top in the middle, and a game over screen with a restart button
public class GameHUD : MonoBehaviour
{
    [SerializeField] private float Margin = 20f;
    [SerializeField] private float FontSize = 32f;
    [SerializeField] private Vector2 LivesBarSize = new Vector2(500f, 50f);
    [SerializeField] private Color FullLivesColor = new Color(0.3f, 0.85f, 0.35f);
    [SerializeField] private Color NoLivesColor = new Color(0.9f, 0.2f, 0.2f);

    private TMP_Text LivesText;
    private RectTransform LivesFill;
    private Image LivesFillImage;
    private GameObject GameOverScreen;
    private int ShownLives = -1;

    void Start()
    {
        BuildLivesBar();

        BuildGameOverScreen();
        GameOverScreen.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameLoopManager.Lives != ShownLives)
        {
            ShownLives = GameLoopManager.Lives;
            LivesText.text = $"{ShownLives} / {GameLoopManager.MaxLives}";

            // The fill shrinks from the right and goes from green to red
            float fraction = GameLoopManager.MaxLives > 0 ? (float)ShownLives / GameLoopManager.MaxLives : 0f;
            LivesFill.anchorMax = new Vector2(fraction, 1f);
            LivesFillImage.color = Color.Lerp(NoLivesColor, FullLivesColor, fraction);
        }

        if (GameLoopManager.IsGameOver && !GameOverScreen.activeSelf)
        {
            // On top of all other UI
            GameOverScreen.transform.SetAsLastSibling();
            GameOverScreen.SetActive(true);
        }
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BuildLivesBar()
    {
        GameObject barObject = new GameObject("Lives Bar", typeof(RectTransform), typeof(Image));
        RectTransform barRect = (RectTransform)barObject.transform;
        barRect.SetParent(transform, false);
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.sizeDelta = LivesBarSize;
        barRect.anchoredPosition = new Vector2(0f, -Margin);
        Image background = barObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);
        background.raycastTarget = false;

        // Small border of the background stays visible around the fill
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        RectTransform fillAreaRect = (RectTransform)fillArea.transform;
        fillAreaRect.SetParent(barRect, false);
        SetRect(fillAreaRect, Vector2.zero, Vector2.one, Vector2.one * 4f, -Vector2.one * 4f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        LivesFill = (RectTransform)fillObject.transform;
        LivesFill.SetParent(fillAreaRect, false);
        SetRect(LivesFill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        LivesFillImage = fillObject.GetComponent<Image>();
        LivesFillImage.color = FullLivesColor;
        LivesFillImage.raycastTarget = false;

        LivesText = CreateText("Lives Text", barRect, FontSize, FontStyles.Bold);
        LivesText.alignment = TextAlignmentOptions.Center;
        SetRect(LivesText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private void BuildGameOverScreen()
    {
        // Covers the whole screen so nothing behind it can be clicked
        GameOverScreen = new GameObject("Game Over Screen", typeof(RectTransform), typeof(Image));
        RectTransform screenRect = (RectTransform)GameOverScreen.transform;
        screenRect.SetParent(transform, false);
        SetRect(screenRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameOverScreen.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        TMP_Text title = CreateText("Title", screenRect, 96f, FontStyles.Bold);
        title.text = "Game Over";
        title.alignment = TextAlignmentOptions.Center;
        SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 20f), new Vector2(0f, 160f));

        GameObject buttonObject = new GameObject("Restart Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.SetParent(screenRect, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(300f, 80f);
        buttonRect.anchoredPosition = new Vector2(0f, -60f);
        buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.6f, 0.3f);
        buttonObject.GetComponent<Button>().onClick.AddListener(Restart);

        TMP_Text buttonText = CreateText("Text", buttonRect, 40f, FontStyles.Bold);
        buttonText.text = "Restart";
        buttonText.alignment = TextAlignmentOptions.Center;
        SetRect(buttonText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private TMP_Text CreateText(string name, Transform parent, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
