using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Start screen over the game with Play, Inventory, Shop, Settings and Quit. The map stays visible behind it.
// Restart skips it, the Main Menu buttons load the scene with it again.
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string Title = "Depth Wars";
    [SerializeField] private string Subtitle = "Tower Defense";

    public static bool IsOpen;
    // Set before reloading the scene to go straight into the game
    private static bool SkipOnNextLoad;

    private static readonly Color PlayColor = new Color(0.25f, 0.6f, 0.3f);
    private static readonly Color ButtonColor = new Color(0.3f, 0.3f, 0.3f);
    private static readonly Color QuitColor = new Color(0.8f, 0.25f, 0.25f);

    private GameObject MenuScreen;
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

    private void Play()
    {
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
        MenuScreen.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.8f);

        TMP_Text title = CreateText("Title", screenRect, 140f, FontStyles.Bold);
        title.text = Title;
        SetCentered(title.rectTransform, new Vector2(1400f, 180f), 330f);

        TMP_Text subtitle = CreateText("Subtitle", screenRect, 48f, FontStyles.Normal);
        subtitle.text = Subtitle;
        subtitle.color = new Color(1f, 0.85f, 0.2f);
        SetCentered(subtitle.rectTransform, new Vector2(1400f, 70f), 220f);

        CreateButton("Play Button", screenRect, "Play", PlayColor, 90f, Play);
        CreateButton("Inventory Button", screenRect, "Inventory", ButtonColor, -10f, OpenInventory);
        CreateButton("Shop Button", screenRect, "Shop", ButtonColor, -110f, OpenShop);
        CreateButton("Settings Button", screenRect, "Settings", ButtonColor, -210f, OpenSettings);
        CreateButton("Quit Button", screenRect, "Quit", QuitColor, -310f, Quit);
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
