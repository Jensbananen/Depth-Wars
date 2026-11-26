using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Esc opens a settings menu in the middle of the screen and pauses the game. The settings are in tabs,
// a new one only needs a field in GameSettings and one line in BuildSettings.
public class SettingsMenu : MonoBehaviour
{
    private const float PanelWidth = 1000f;
    private const float PanelHeight = 800f;
    private const float RowHeight = 68f;
    private const float Spacing = 14f;
    private const float Padding = 32f;

    public static bool IsOpen;
    // Set by scripts that already used Esc this frame (cancel placing, deselect a tower),
    // so the same press doesn't also open the menu
    public static int EscapeHandledFrame = -1;

    private static readonly Color OnColor = UIStyle.Primary;
    private static readonly Color OffColor = UIStyle.Neutral;
    private static readonly Color ChoiceColor = UIStyle.Accent;
    private static readonly Color DangerColor = UIStyle.Danger;

    private class Tab
    {
        public RectTransform Page;
        public Image Button;
        public float NextRowY;
    }

    private GameObject MenuScreen;
    private RectTransform Panel;
    private readonly List<Tab> Tabs = new List<Tab>();
    // Tab that gets the rows while the menu is built
    private Tab BuildingTab;
    private readonly List<System.Action> RefreshActions = new List<System.Action>();
    private CanvasScaler Scaler;
    private Vector2 NormalResolution;

    void Start()
    {
        IsOpen = false;
        Scaler = GetComponent<CanvasScaler>();
        NormalResolution = Scaler.referenceResolution;

        BuildMenu();
        MenuScreen.SetActive(false);
        gameObject.AddComponent<FpsCounter>();

        GameSettings.Changed += OnSettingsChanged;
        ApplyUiScale();
    }

    void OnDestroy()
    {
        GameSettings.Changed -= OnSettingsChanged;
    }

    // LateUpdate runs after every Update, so EscapeHandledFrame is already set by then
    void LateUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || EscapeHandledFrame == Time.frameCount)
        {
            return;
        }

        if (IsOpen)
        {
            Close();
        }
        else if (CanOpenInGame())
        {
            Open();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // MenuScreen doesn't exist yet when the game starts in the background
        if (!hasFocus && GameSettings.AutoPause && MenuScreen != null && !IsOpen && CanOpenInGame())
        {
            Open();
        }
    }

    private bool CanOpenInGame()
    {
        return !GameLoopManager.IsGameOver && !GameLoopManager.HasWon && !MainMenu.IsOpen;
    }

    private void BuildSettings()
    {
        AddTab("Camera");
        AddSlider("Camera speed", 0.25f, 3f, () => GameSettings.CameraSpeed, value => GameSettings.CameraSpeed = value);
        AddSlider("Mouse sensitivity", 0.25f, 3f, () => GameSettings.MouseSensitivity, value => GameSettings.MouseSensitivity = value);

        AddTab("Video");
        AddToggle("Fullscreen", () => GameSettings.Fullscreen, value => GameSettings.Fullscreen = value);
        AddChoice("Resolution", VideoSettings.GetResolutions, VideoSettings.GetResolution, value =>
        {
            GameSettings.ScreenWidth = value.x;
            GameSettings.ScreenHeight = value.y;
        }, value => $"{value.x} x {value.y}");
        AddChoice("Quality", () => new List<GraphicsQuality> { GraphicsQuality.Low, GraphicsQuality.Medium, GraphicsQuality.High },
            () => GameSettings.Quality, value => GameSettings.Quality = value, value => value.ToString());
        AddToggle("VSync", () => GameSettings.VSync, value => GameSettings.VSync = value);
        AddChoice("FPS limit", () => new List<int> { 30, 60, 120, 0 },
            () => GameSettings.FpsLimit, value => GameSettings.FpsLimit = value, value => value > 0 ? value.ToString() : "Unlimited");

        AddTab("Gameplay");
        AddChoice("Game speed", () => new List<int> { 1, 2, 3 },
            () => GameSettings.GameSpeed, value => GameSettings.GameSpeed = value, value => $"{value}x");
        AddToggle("Auto skip waves", () => GameSettings.AutoSkipWaves, GameLoopManager.SetAutoSkipWaves);
        AddToggle("Pause in background", () => GameSettings.AutoPause, value => GameSettings.AutoPause = value);

        AddTab("Interface");
        AddChoice("Health bars", () => new List<HealthBarMode> { HealthBarMode.Off, HealthBarMode.Damaged, HealthBarMode.Always },
            () => GameSettings.HealthBars, value => GameSettings.HealthBars = value, value => value.ToString());
        AddToggle("Damage numbers", () => GameSettings.DamageNumbers, value => GameSettings.DamageNumbers = value);
        AddToggle("FPS counter", () => GameSettings.ShowFps, value => GameSettings.ShowFps = value);
        // Bigger than this doesn't fit on the screen
        AddChoice("UI size", () => new List<float> { 0.7f, 0.8f, 0.9f, 1f, 1.1f, 1.2f },
            () => GameSettings.UiScale, value => GameSettings.UiScale = value, value => $"{Mathf.RoundToInt(value * 100f)}%");

        AddTab("General");
        AddConfirmButton("Reset all settings", GameSettings.ResetToDefaults);
        AddConfirmButton("Delete coins and towers", DeleteProgress);
    }

    private void DeleteProgress()
    {
        CoinWallet.Reset();
        Loadout.Reset();
        MapManager.ResetWins();
    }

    public void Open()
    {
        IsOpen = true;
        GameLoopManager.SetPaused(true);
        Refresh();

        // On top of all other UI
        MenuScreen.transform.SetAsLastSibling();
        MenuScreen.SetActive(true);
    }

    private void Close()
    {
        IsOpen = false;
        GameLoopManager.SetPaused(false);
        MenuScreen.SetActive(false);
        PlayerPrefs.Save();

        // Otherwise the arrow keys keep moving the last used slider
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnSettingsChanged()
    {
        ApplyUiScale();
        Refresh();
    }

    private void Refresh()
    {
        foreach (System.Action refresh in RefreshActions)
        {
            refresh();
        }
    }

    private void ApplyUiScale()
    {
        Scaler.referenceResolution = NormalResolution / GameSettings.UiScale;
    }

    private void BuildMenu()
    {
        // Dark layer over the whole screen so nothing behind it can be clicked
        MenuScreen = new GameObject("Settings Menu", typeof(RectTransform), typeof(Image));
        RectTransform screenRect = (RectTransform)MenuScreen.transform;
        screenRect.SetParent(transform, false);
        SetRect(screenRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        MenuScreen.GetComponent<Image>().color = UIStyle.Overlay;

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        Panel = (RectTransform)panelObject.transform;
        Panel.SetParent(screenRect, false);
        Panel.anchorMin = new Vector2(0.5f, 0.5f);
        Panel.anchorMax = new Vector2(0.5f, 0.5f);
        Panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelObject.GetComponent<Image>().color = UIStyle.Panel;

        TMP_Text title = CreateText("Title", Panel, 56f, FontStyles.Bold);
        title.text = "Settings";
        title.alignment = TextAlignmentOptions.Center;
        SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -100f), new Vector2(0f, -16f));

        BuildSettings();
        LayOutTabButtons();
        ShowTab(Tabs[0]);

        // Next to each other at the bottom
        Button mainMenu = CreateButton("Main Menu Button", Panel, "Main Menu", OffColor, out _);
        mainMenu.onClick.AddListener(MainMenu.BackToMainMenu);
        SetRect((RectTransform)mainMenu.transform, Vector2.zero, new Vector2(0.5f, 0f), new Vector2(Padding, 24f), new Vector2(-Spacing * 0.5f, 24f + RowHeight));

        Button resume = CreateButton("Resume Button", Panel, "Resume", OnColor, out _);
        resume.onClick.AddListener(Close);
        SetRect((RectTransform)resume.transform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(Spacing * 0.5f, 24f), new Vector2(-Padding, 24f + RowHeight));
    }

    // The rows added after this go on this tab
    private void AddTab(string label)
    {
        GameObject pageObject = new GameObject(label + " Page", typeof(RectTransform));
        Tab tab = new Tab { Page = (RectTransform)pageObject.transform };
        tab.Page.SetParent(Panel, false);
        SetRect(tab.Page, Vector2.zero, Vector2.one, new Vector2(Padding, 24f + RowHeight + Spacing), new Vector2(-Padding, -190f));

        Button button = CreateButton(label + " Tab", Panel, label, OffColor, out TMP_Text text);
        text.fontSize = 28f;
        button.onClick.AddListener(() => ShowTab(tab));
        tab.Button = button.GetComponent<Image>();

        Tabs.Add(tab);
        BuildingTab = tab;
    }

    // Below the title, all tabs equally wide
    private void LayOutTabButtons()
    {
        float width = (PanelWidth - Padding * 2f - Spacing * (Tabs.Count - 1)) / Tabs.Count;
        for (int i = 0; i < Tabs.Count; i++)
        {
            float left = Padding + (width + Spacing) * i;
            SetRect(Tabs[i].Button.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -168f), new Vector2(left + width, -108f));
        }
    }

    private void ShowTab(Tab shown)
    {
        foreach (Tab tab in Tabs)
        {
            tab.Page.gameObject.SetActive(tab == shown);
            tab.Button.color = tab == shown ? ChoiceColor : OffColor;
        }
    }

    // A row with the name on the left, the control goes on the right
    private RectTransform AddRow(string label)
    {
        GameObject row = new GameObject(label, typeof(RectTransform));
        RectTransform rowRect = (RectTransform)row.transform;
        rowRect.SetParent(BuildingTab.Page, false);
        SetRect(rowRect, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -BuildingTab.NextRowY - RowHeight), new Vector2(0f, -BuildingTab.NextRowY));
        BuildingTab.NextRowY += RowHeight + Spacing;

        TMP_Text text = CreateText("Label", rowRect, 34f, FontStyles.Normal);
        text.text = label;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        SetRect(text.rectTransform, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        return rowRect;
    }

    private void AddToggle(string label, System.Func<bool> getValue, System.Action<bool> setValue)
    {
        RectTransform row = AddRow(label);
        Button button = CreateButton("Toggle", row, "", OffColor, out TMP_Text text);
        SetRect((RectTransform)button.transform, new Vector2(0.75f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
        Image image = button.GetComponent<Image>();

        RefreshActions.Add(() =>
        {
            text.text = getValue() ? "On" : "Off";
            image.color = getValue() ? OnColor : OffColor;
        });
        button.onClick.AddListener(() =>
        {
            setValue(!getValue());
            GameSettings.Apply();
        });
    }

    // The value with a < and > button next to it
    private void AddChoice<T>(string label, System.Func<List<T>> getOptions, System.Func<T> getValue, System.Action<T> setValue, System.Func<T, string> describe)
    {
        RectTransform row = AddRow(label);
        Button previous = CreateButton("Previous", row, "<", ChoiceColor, out _);
        SetRect((RectTransform)previous.transform, new Vector2(0.5f, 0f), new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero);
        Button next = CreateButton("Next", row, ">", ChoiceColor, out _);
        SetRect((RectTransform)next.transform, new Vector2(0.92f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text text = CreateText("Value", row, 34f, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        SetRect(text.rectTransform, new Vector2(0.58f, 0f), new Vector2(0.92f, 1f), Vector2.zero, Vector2.zero);

        void Step(int direction)
        {
            List<T> options = getOptions();
            int index = options.IndexOf(getValue());
            // A saved value that isn't an option anymore goes to the first one
            index = index < 0 ? 0 : (index + direction + options.Count) % options.Count;
            setValue(options[index]);
            GameSettings.Apply();
        }

        RefreshActions.Add(() => text.text = describe(getValue()));
        previous.onClick.AddListener(() => Step(-1));
        next.onClick.AddListener(() => Step(1));
    }

    // Shown as a percentage
    private void AddSlider(string label, float min, float max, System.Func<float> getValue, System.Action<float> setValue)
    {
        RectTransform row = AddRow(label);
        TMP_Text text = CreateText("Value", row, 34f, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.MidlineRight;
        SetRect(text.rectTransform, new Vector2(0.88f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
        RectTransform sliderRect = (RectTransform)sliderObject.transform;
        sliderRect.SetParent(row, false);
        SetRect(sliderRect, new Vector2(0.5f, 0.5f), new Vector2(0.86f, 0.5f), new Vector2(0f, -10f), new Vector2(0f, 10f));
        sliderObject.GetComponent<Image>().color = OffColor;

        RectTransform fill = CreateSliderPart("Fill", sliderRect, OnColor);
        SetRect(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform handle = CreateSliderPart("Handle", sliderRect, Color.white);
        handle.sizeDelta = new Vector2(24f, 24f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = min;
        slider.maxValue = max;

        RefreshActions.Add(() =>
        {
            slider.SetValueWithoutNotify(getValue());
            text.text = $"{Mathf.RoundToInt(getValue() * 100f)}%";
        });
        slider.onValueChanged.AddListener(value =>
        {
            // Steps of 5%
            setValue(Mathf.Round(value * 20f) / 20f);
            GameSettings.Apply();
        });
    }

    private RectTransform CreateSliderPart(string name, Transform parent, Color color)
    {
        GameObject partObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        partObject.transform.SetParent(parent, false);
        partObject.GetComponent<Image>().color = color;
        return (RectTransform)partObject.transform;
    }

    // Only does it after a second click, within a few seconds
    private void AddConfirmButton(string label, System.Action action)
    {
        RectTransform row = AddRow(label);
        Button button = CreateButton("Confirm", row, "", OffColor, out TMP_Text text);
        SetRect((RectTransform)button.transform, new Vector2(0.6f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
        Image image = button.GetComponent<Image>();
        bool asking = false;
        Coroutine cancel = null;

        void SetAsking(bool value)
        {
            asking = value;
            text.text = value ? "Are you sure?" : "Reset";
            image.color = value ? DangerColor : OffColor;
        }

        IEnumerator CancelLater()
        {
            yield return new WaitForSecondsRealtime(3f);
            SetAsking(false);
        }

        RefreshActions.Add(() => SetAsking(false));
        button.onClick.AddListener(() =>
        {
            if (cancel != null)
            {
                StopCoroutine(cancel);
                cancel = null;
            }

            if (!asking)
            {
                SetAsking(true);
                cancel = StartCoroutine(CancelLater());
                return;
            }

            SetAsking(false);
            action();
        });
    }

    private Button CreateButton(string name, Transform parent, string label, Color color, out TMP_Text text)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = color;

        text = CreateText("Text", buttonObject.transform, 34f, FontStyles.Bold);
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return buttonObject.GetComponent<Button>();
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
