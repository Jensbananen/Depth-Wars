using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

// Start screen over the game with Play, Inventory, Shop, Settings and Quit. The map stays visible behind it.
// Play first shows a screen to choose the map.
// Restart skips it, the Main Menu buttons load the scene with it again.
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string Title = "Depth Wars";
    [SerializeField] private string Subtitle = "Tower Defense";

    public static bool IsOpen;
    // Set before reloading the scene to go straight into the game
    private static bool SkipOnNextLoad;

    private static readonly Color PlayColor = UIStyle.Primary;
    private static readonly Color ButtonColor = UIStyle.Neutral;
    private static readonly Color QuitColor = UIStyle.Danger;

    private GameObject MenuScreen;
    private GameObject MapScreen;
    private readonly Dictionary<string, TMP_Text> WinTexts = new Dictionary<string, TMP_Text>();
    private SettingsMenu Settings;
    private TowerMenu TowerScreens;

    void Start()
    {
        Settings = GetComponent<SettingsMenu>();
        TowerScreens = GetComponent<TowerMenu>();
        BuildMenu();

        IsOpen = !SkipOnNextLoad;
        SkipOnNextLoad = false;
        MenuScreen.SetActive(IsOpen);
    }

    // Update is called once per frame
    void Update()
    {
        // Other UI is built in Start too, keep the menu on top of it (but below the menus opened from it)
        if (IsOpen && !SettingsMenu.IsOpen && !TowerMenu.IsOpen && MenuScreen.transform.GetSiblingIndex() != transform.childCount - 1)
        {
            MenuScreen.transform.SetAsLastSibling();
        }

        if (IsOpen && MapScreen.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            MapScreen.SetActive(false);
        }
    }

    public static void RestartGame()
    {
        SkipOnNextLoad = true;
        ReloadScene();
    }

    public static void BackToMainMenu()
    {
        SkipOnNextLoad = false;
        ReloadScene();
    }

    private static void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OpenMapScreen()
    {
        // Progress can be deleted in the settings, so check again every time
        foreach (KeyValuePair<string, TMP_Text> winText in WinTexts)
        {
            bool won = MapManager.IsWon(winText.Key);
            winText.Value.text = won ? "Completed" : $"First win: +{MapManager.FirstWinCoins} coins";
            winText.Value.color = won ? new Color(0.4f, 1f, 0.4f) : UIStyle.Coins;
        }
        MapScreen.SetActive(true);
    }

    // Straight into the game, the scene only has to load again for another map
    private void PlayMap(string mapName)
    {
        MapManager.Select(mapName);
        if (mapName != MapManager.LoadedMap)
        {
            RestartGame();
            return;
        }

        IsOpen = false;
        MenuScreen.SetActive(false);
    }

    private void OpenInventory()
    {
        TowerScreens.OpenInventory();
    }

    private void OpenShop()
    {
        TowerScreens.OpenShop();
    }

    private void OpenSettings()
    {
        Settings.Open();
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildMenu()
    {
        // Dark layer over the whole screen so the game behind it can't be clicked
        MenuScreen = new GameObject("Main Menu", typeof(RectTransform), typeof(Image));
        RectTransform screenRect = (RectTransform)MenuScreen.transform;
        screenRect.SetParent(transform, false);
        SetRect(screenRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        MenuScreen.GetComponent<Image>().color = UIStyle.MenuBackground;

        TMP_Text title = CreateText("Title", screenRect, 140f, FontStyles.Bold);
        title.text = Title;
        SetCentered(title.rectTransform, new Vector2(1400f, 180f), 330f);

        TMP_Text subtitle = CreateText("Subtitle", screenRect, 48f, FontStyles.Normal);
        subtitle.text = Subtitle;
        subtitle.color = UIStyle.Highlight;
        SetCentered(subtitle.rectTransform, new Vector2(1400f, 70f), 220f);

        CreateButton("Play Button", screenRect, "Play", PlayColor, 90f, OpenMapScreen);
        CreateButton("Inventory Button", screenRect, "Inventory", ButtonColor, -10f, OpenInventory);
        CreateButton("Shop Button", screenRect, "Shop", ButtonColor, -110f, OpenShop);
        CreateButton("Settings Button", screenRect, "Settings", ButtonColor, -210f, OpenSettings);
        CreateButton("Quit Button", screenRect, "Quit", QuitColor, -310f, Quit);
        AddLogo(screenRect);

        BuildMapScreen(screenRect);
    }

    // Small logo of Life Below Water in the bottom right corner, only when the logo is in the project.
    // A soft white glow behind it keeps the dark blue readable and fades into the background.
    private void AddLogo(RectTransform parent)
    {
        Sprite logo = UIStyle.GetLogo();
        if (logo == null)
        {
            return;
        }

        const float height = 130f;
        const float padding = 40f;
        float width = height * logo.rect.width / logo.rect.height;

        GameObject card = new GameObject("Logo", typeof(RectTransform), typeof(Image));
        RectTransform cardRect = (RectTransform)card.transform;
        cardRect.SetParent(parent, false);
        cardRect.anchorMin = new Vector2(1f, 0f);
        cardRect.anchorMax = new Vector2(1f, 0f);
        cardRect.pivot = new Vector2(1f, 0f);
        cardRect.sizeDelta = new Vector2(width + padding * 2f, height + padding * 2f);
        cardRect.anchoredPosition = new Vector2(-10f, 10f);
        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = UIStyle.GetGlow();
        cardImage.color = new Color(1f, 1f, 1f, 0.9f);
        cardImage.raycastTarget = false;

        GameObject logoObject = new GameObject("Image", typeof(RectTransform), typeof(Image));
        RectTransform logoRect = (RectTransform)logoObject.transform;
        logoRect.SetParent(cardRect, false);
        SetRect(logoRect, Vector2.zero, Vector2.one, Vector2.one * padding, -Vector2.one * padding);
        Image logoImage = logoObject.GetComponent<Image>();
        logoImage.sprite = logo;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
    }

    // Covers the rest of the main menu with a card per map
    private void BuildMapScreen(RectTransform parent)
    {
        MapScreen = new GameObject("Map Screen", typeof(RectTransform), typeof(Image));
        RectTransform screenRect = (RectTransform)MapScreen.transform;
        screenRect.SetParent(parent, false);
        SetRect(screenRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        MapScreen.GetComponent<Image>().color = UIStyle.Panel;

        TMP_Text title = CreateText("Title", screenRect, 90f, FontStyles.Bold);
        title.text = "Choose a map";
        SetCentered(title.rectTransform, new Vector2(1400f, 130f), 330f);

        // Cards next to each other, more maps make the row wider
        GameObject rowObject = new GameObject("Maps", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform row = (RectTransform)rowObject.transform;
        row.SetParent(screenRect, false);
        SetCentered(row, new Vector2(1700f, 380f), 30f);
        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 40f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        foreach (MapInfo map in MapManager.GetMapPrefabs())
        {
            AddMapCard(row, map.MapName, map.Description, map.BackgroundColor);
        }

        CreateButton("Back Button", screenRect, "Back", ButtonColor, -310f, () => MapScreen.SetActive(false));
        MapScreen.SetActive(false);
    }

    // The whole card is a button
    private void AddMapCard(RectTransform row, string mapName, string description, Color color)
    {
        GameObject card = new GameObject(mapName, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform cardRect = (RectTransform)card.transform;
        cardRect.SetParent(row, false);
        cardRect.sizeDelta = new Vector2(520f, 360f);
        card.GetComponent<Image>().color = color;
        card.GetComponent<Button>().onClick.AddListener(() => PlayMap(mapName));

        TMP_Text title = CreateText("Name", cardRect, 60f, FontStyles.Bold);
        title.text = mapName;
        SetCentered(title.rectTransform, new Vector2(500f, 90f), 40f);

        TMP_Text text = CreateText("Description", cardRect, 32f, FontStyles.Normal);
        text.text = description;
        SetCentered(text.rectTransform, new Vector2(480f, 90f), -40f);

        // Dark strip at the bottom, so the text is readable on every card color
        GameObject strip = new GameObject("Win Strip", typeof(RectTransform), typeof(Image));
        RectTransform stripRect = (RectTransform)strip.transform;
        stripRect.SetParent(cardRect, false);
        SetRect(stripRect, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 64f));
        Image stripImage = strip.GetComponent<Image>();
        stripImage.color = UIStyle.Box;
        stripImage.raycastTarget = false;

        // Filled in when the map screen opens
        TMP_Text winText = CreateText("Win Text", stripRect, 32f, FontStyles.Bold);
        SetRect(winText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        WinTexts[mapName] = winText;
    }

    private void CreateButton(string name, RectTransform parent, string label, Color color, float y, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.SetParent(parent, false);
        SetCentered(buttonRect, new Vector2(420f, 90f), y);
        buttonObject.GetComponent<Image>().color = color;
        buttonObject.GetComponent<Button>().onClick.AddListener(onClick);

        TMP_Text text = CreateText("Text", buttonRect, 44f, FontStyles.Bold);
        text.text = label;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    // Centered horizontally, y is the offset from the middle of the screen
    private void SetCentered(RectTransform rect, Vector2 size, float y)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(0f, y);
    }

    private TMP_Text CreateText(string name, Transform parent, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        if ((style & FontStyles.Bold) != 0)
        {
            UIStyle.ApplyHeadingFont(text);
        }
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
