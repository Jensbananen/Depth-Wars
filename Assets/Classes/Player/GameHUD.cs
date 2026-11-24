using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Lives bar, money and wave at the top in the middle, a Start Wave button at the top right,
// and a game over or win screen with a restart button
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
    private TMP_Text MoneyText;
    private int ShownMoney = -1;
    private GameObject GameOverScreen;
    private GameObject WinScreen;
    private int ShownLives = -1;
    private TMP_Text WaveText;
    private int ShownWave = -1;
    private GameObject StartWaveButton;
    private TMP_Text StartWaveText;
    private TMP_Text WaveCompleteText;
    private int ShownCompletedWave;
    private float WaveCompleteTimeLeft;
    private const float WaveCompleteDuration = 3f;

    void Start()
    {
        BuildLivesBar();
        BuildInfoBox();
        BuildStartWaveButton();
        BuildWaveCompleteText();

        GameOverScreen = BuildEndScreen("Game Over Screen", "Game Over", Color.white);
        WinScreen = BuildEndScreen("Win Screen", "You Win!", new Color(1f, 0.85f, 0.2f));
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

        if (GameLoopManager.Money != ShownMoney)
        {
            ShownMoney = GameLoopManager.Money;
            MoneyText.text = $"${ShownMoney}";
        }

        if (GameLoopManager.CurrentWave != ShownWave)
        {
            ShownWave = GameLoopManager.CurrentWave;
            WaveText.text = $"Wave {ShownWave} / {GameLoopManager.TotalWaves}";
            StartWaveText.text = $"Start Wave {ShownWave + 1}";
        }

        UpdateWaveCompleteText();

        bool canStartWave = GameLoopManager.CanStartNextWave();
        if (StartWaveButton.activeSelf != canStartWave)
        {
            StartWaveButton.SetActive(canStartWave);
        }

        if (GameLoopManager.IsGameOver)
        {
            ShowEndScreen(GameOverScreen);
        }
        else if (GameLoopManager.HasWon)
        {
            ShowEndScreen(WinScreen);
        }
    }

    // "Wave 5 complete! +$40 +5 coins" for a few seconds after every wave, not after the last one (that shows the win screen)
    private void UpdateWaveCompleteText()
    {
        if (GameLoopManager.LastCompletedWave != ShownCompletedWave)
        {
            ShownCompletedWave = GameLoopManager.LastCompletedWave;
            if (ShownCompletedWave > 0 && !GameLoopManager.HasWon)
            {
                WaveCompleteText.text = $"Wave {ShownCompletedWave} complete!  <color=#FFD833>+${GameLoopManager.LastWaveBonus}</color>  <color=#FF9933>+{GameLoopManager.LastWaveCoins} coins</color>";
                WaveCompleteText.transform.parent.gameObject.SetActive(true);
                WaveCompleteTimeLeft = WaveCompleteDuration;
            }
        }

        if (WaveCompleteTimeLeft > 0f)
        {
            // Real time, so it also disappears while paused or at 2x speed after the same time
            WaveCompleteTimeLeft -= Time.unscaledDeltaTime;
            if (WaveCompleteTimeLeft <= 0f)
            {
                WaveCompleteText.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    private void ShowEndScreen(GameObject screen)
    {
        if (!screen.activeSelf)
        {
            screen.transform.Find("Coins").GetComponent<TMP_Text>().text = $"Coins earned: {GameLoopManager.CoinsEarned}";

            // On top of all other UI
            screen.transform.SetAsLastSibling();
            screen.SetActive(true);
        }
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

    // Dark box just below the lives bar, money on the left and the wave on the right,
    // so both stay readable against the light sky
    private void BuildInfoBox()
    {
        GameObject boxObject = new GameObject("Info Box", typeof(RectTransform), typeof(Image));
        RectTransform boxRect = (RectTransform)boxObject.transform;
        boxRect.SetParent(transform, false);
        boxRect.anchorMin = new Vector2(0.5f, 1f);
        boxRect.anchorMax = new Vector2(0.5f, 1f);
        boxRect.pivot = new Vector2(0.5f, 1f);
        boxRect.sizeDelta = new Vector2(LivesBarSize.x, 50f);
        boxRect.anchoredPosition = new Vector2(0f, -Margin - LivesBarSize.y - 6f);
        Image background = boxObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);
        background.raycastTarget = false;

        MoneyText = CreateText("Money", boxRect, FontSize + 4f, FontStyles.Bold);
        MoneyText.color = new Color(1f, 0.85f, 0.2f);
        MoneyText.alignment = TextAlignmentOptions.MidlineLeft;
        SetRect(MoneyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));

        WaveText = CreateText("Wave", boxRect, FontSize, FontStyles.Bold);
        WaveText.alignment = TextAlignmentOptions.MidlineRight;
        SetRect(WaveText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
    }

    private void BuildStartWaveButton()
    {
        StartWaveButton = new GameObject("Start Wave Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = (RectTransform)StartWaveButton.transform;
        buttonRect.SetParent(transform, false);
        buttonRect.anchorMin = Vector2.one;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.pivot = Vector2.one;
        buttonRect.sizeDelta = new Vector2(280f, 80f);
        buttonRect.anchoredPosition = new Vector2(-Margin, -Margin);
        StartWaveButton.GetComponent<Image>().color = new Color(0.25f, 0.6f, 0.3f);
        StartWaveButton.GetComponent<Button>().onClick.AddListener(GameLoopManager.StartNextWave);

        StartWaveText = CreateText("Text", buttonRect, 34f, FontStyles.Bold);
        StartWaveText.alignment = TextAlignmentOptions.Center;
        SetRect(StartWaveText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    // Dark box with big text just below the money and wave box, so it is readable against the sky
    private void BuildWaveCompleteText()
    {
        GameObject boxObject = new GameObject("Wave Complete", typeof(RectTransform), typeof(Image));
        RectTransform boxRect = (RectTransform)boxObject.transform;
        boxRect.SetParent(transform, false);
        boxRect.anchorMin = new Vector2(0.5f, 1f);
        boxRect.anchorMax = new Vector2(0.5f, 1f);
        boxRect.pivot = new Vector2(0.5f, 1f);
        boxRect.sizeDelta = new Vector2(900f, 70f);
        boxRect.anchoredPosition = new Vector2(0f, -Margin - LivesBarSize.y - 66f);
        Image background = boxObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);
        background.raycastTarget = false;

        WaveCompleteText = CreateText("Text", boxRect, 40f, FontStyles.Bold);
        WaveCompleteText.alignment = TextAlignmentOptions.Center;
        SetRect(WaveCompleteText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        boxObject.SetActive(false);
    }

    private GameObject BuildEndScreen(string name, string titleText, Color titleColor)
    {
        // Covers the whole screen so nothing behind it can be clicked
        GameObject screen = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform screenRect = (RectTransform)screen.transform;
        screenRect.SetParent(transform, false);
        SetRect(screenRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        screen.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        TMP_Text title = CreateText("Title", screenRect, 96f, FontStyles.Bold);
        title.text = titleText;
        title.color = titleColor;
        title.alignment = TextAlignmentOptions.Center;
        SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 50f), new Vector2(0f, 190f));

        // Filled in when the screen is shown
        TMP_Text coins = CreateText("Coins", screenRect, 44f, FontStyles.Bold);
        coins.color = new Color(1f, 0.6f, 0.2f);
        coins.alignment = TextAlignmentOptions.Center;
        SetRect(coins.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -15f), new Vector2(0f, 45f));

        GameObject buttonObject = new GameObject("Restart Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.SetParent(screenRect, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(300f, 80f);
        buttonRect.anchoredPosition = new Vector2(0f, -60f);
        buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.6f, 0.3f);
        buttonObject.GetComponent<Button>().onClick.AddListener(MainMenu.RestartGame);

        TMP_Text buttonText = CreateText("Text", buttonRect, 40f, FontStyles.Bold);
        buttonText.text = "Restart";
        buttonText.alignment = TextAlignmentOptions.Center;
        SetRect(buttonText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject menuObject = new GameObject("Main Menu Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform menuRect = (RectTransform)menuObject.transform;
        menuRect.SetParent(screenRect, false);
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(300f, 80f);
        menuRect.anchoredPosition = new Vector2(0f, -160f);
        menuObject.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
        menuObject.GetComponent<Button>().onClick.AddListener(MainMenu.BackToMainMenu);

        TMP_Text menuText = CreateText("Text", menuRect, 40f, FontStyles.Bold);
        menuText.text = "Main Menu";
        menuText.alignment = TextAlignmentOptions.Center;
        SetRect(menuText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        screen.SetActive(false);
        return screen;
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
