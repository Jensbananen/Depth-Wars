using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Esc opens a settings menu in the middle of the screen and pauses the game.
// New settings only need one AddToggle line in BuildSettings.
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private float PanelWidth = 640f;
    [SerializeField] private float RowHeight = 70f;
    [SerializeField] private float Spacing = 16f;

    public static bool IsOpen;
    // Set by scripts that already used Esc this frame (cancel placing, deselect a tower),
    // so the same press doesn't also open the menu
    public static int EscapeHandledFrame = -1;

    private static readonly Color OnColor = new Color(0.25f, 0.6f, 0.3f);
    private static readonly Color OffColor = new Color(0.3f, 0.3f, 0.3f);

    private GameObject MenuScreen;
    private RectTransform Panel;
    private float NextRowY;
    private readonly List<System.Action> RefreshActions = new List<System.Action>();

    void Start()
    {
        IsOpen = false;
        BuildMenu();
        MenuScreen.SetActive(false);
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
        else if (!GameLoopManager.IsGameOver && !GameLoopManager.HasWon)
        {
            Open();
        }
    }

    private void BuildSettings()
    {
        AddToggle("Auto skip waves", () => GameLoopManager.AutoStartWaves, value => GameLoopManager.SetAutoStartWaves(value));
        AddToggle("Game speed 2x", () => GameLoopManager.IsFastForward, value => GameLoopManager.SetFastForward(value));
    }

    private void Open()
    {
        IsOpen = true;
        GameLoopManager.SetPaused(true);
        foreach (System.Action refresh in RefreshActions)
        {
            refresh();
        }

        // On top of all other UI
        MenuScreen.transform.SetAsLastSibling();
        MenuScreen.SetActive(true);
    }

    private void Close()
    {
        IsOpen = false;
        GameLoopManager.SetPaused(false);
        MenuScreen.SetActive(false);
    }

    private void BuildMenu()
    {
        // Dark layer over the whole screen so nothing behind it can be clicked
        MenuScreen = new GameObject("Settings Menu", typeof(RectTransform), typeof(Image));
        RectTransform screenRect = (RectTransform)MenuScreen.transform;
        screenRect.SetParent(transform, false);
        SetRect(screenRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        MenuScreen.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        Panel = (RectTransform)panelObject.transform;
        Panel.SetParent(screenRect, false);
        Panel.anchorMin = new Vector2(0.5f, 0.5f);
        Panel.anchorMax = new Vector2(0.5f, 0.5f);
        panelObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.95f);

        TMP_Text title = CreateText("Title", Panel, 56f, FontStyles.Bold);
        title.text = "Settings";
        title.alignment = TextAlignmentOptions.Center;
        SetRow(title.rectTransform, 80f);

        BuildSettings();

        Button resume = CreateButton("Resume Button", Panel, "Resume", OnColor, out _);
        resume.onClick.AddListener(Close);
        SetRow((RectTransform)resume.transform, RowHeight);

        // Panel grows with the number of settings
        Panel.sizeDelta = new Vector2(PanelWidth, NextRowY + Spacing);
    }

    // A row with the name on the left and an On/Off button on the right
    private void AddToggle(string label, System.Func<bool> getValue, System.Action<bool> setValue)
    {
        GameObject row = new GameObject(label, typeof(RectTransform));
        RectTransform rowRect = (RectTransform)row.transform;
        rowRect.SetParent(Panel, false);
        SetRow(rowRect, RowHeight);

        TMP_Text text = CreateText("Label", rowRect, 34f, FontStyles.Normal);
        text.text = label;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        SetRect(text.rectTransform, Vector2.zero, new Vector2(0.65f, 1f), Vector2.zero, Vector2.zero);

        Button button = CreateButton("Toggle", rowRect, "", OffColor, out TMP_Text buttonText);
        SetRect((RectTransform)button.transform, new Vector2(0.7f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
        Image buttonImage = button.GetComponent<Image>();

        System.Action refresh = () =>
        {
            bool value = getValue();
            buttonText.text = value ? "On" : "Off";
            buttonImage.color = value ? OnColor : OffColor;
        };
        button.onClick.AddListener(() =>
        {
            setValue(!getValue());
            refresh();
        });
        RefreshActions.Add(refresh);
    }

    // Places the rect below the previous row, rows are stacked from the top of the panel
    private void SetRow(RectTransform rect, float height)
    {
        NextRowY += Spacing;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(Spacing * 2f, -NextRowY - height);
        rect.offsetMax = new Vector2(-Spacing * 2f, -NextRowY);
        NextRowY += height;
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
