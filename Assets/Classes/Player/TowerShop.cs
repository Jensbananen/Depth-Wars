using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Makes one button per equipped tower (see Loadout), in a row from the bottom left.
// Slot 1 to 5 can also be picked with the number keys. Buttons are greyed out when you can't place the tower.
// Hovering a button shows the tower's name, price and stats above it.
public class TowerShop : MonoBehaviour
{
    [SerializeField] private TowerPlacement TowerPlacement;
    [SerializeField] private float ButtonSize = 150f;
    [SerializeField] private float Spacing = 15f;
    [SerializeField] private float Margin = 20f;
    [SerializeField] private float IconPadding = 12f;

    private const int MaxHotkeySlots = 9;

    private TowerData[] Towers;
    private Button[] Buttons;
    private Image[] Icons;
    private TMP_Text[] CostTexts;
    private TMP_Text[] LimitTexts;
    private TMP_Text TotalText;
    private int ShownMoney = -1;
    private int ShownTowers = -1;
    private RectTransform Tooltip;
    private TMP_Text TooltipText;

    void Start()
    {
        BuildTooltip();
        BuildTotalText();
        BuildButtons();
        Loadout.Changed += BuildButtons;
    }

    void OnDestroy()
    {
        Loadout.Changed -= BuildButtons;
    }

    // Also called again when the loadout changes in the main menu
    private void BuildButtons()
    {
        if (Buttons != null)
        {
            foreach (Button oldButton in Buttons)
            {
                Destroy(oldButton.gameObject);
            }
        }

        Towers = Loadout.GetEquipped().ToArray();
        Buttons = new Button[Towers.Length];
        Icons = new Image[Towers.Length];
        CostTexts = new TMP_Text[Towers.Length];
        LimitTexts = new TMP_Text[Towers.Length];
        for (int i = 0; i < Towers.Length; i++)
        {
            CreateButton(Towers[i], i);
        }

        ShownMoney = -1;
        HideTooltip();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameLoopManager.Money != ShownMoney || GameLoopManager.TowersInGame.Count != ShownTowers)
        {
            ShownMoney = GameLoopManager.Money;
            ShownTowers = GameLoopManager.TowersInGame.Count;
            UpdateButtons();
        }

        if (SettingsMenu.IsOpen || MainMenu.IsOpen)
        {
            return;
        }

        int slots = Mathf.Min(Towers.Length, MaxHotkeySlots);
        for (int i = 0; i < slots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                TowerPlacement.SetTowerToPlace(Towers[i]);
            }
        }
    }

    private void UpdateButtons()
    {
        Color red = new Color(1f, 0.35f, 0.35f);
        TotalText.text = $"Towers: {ShownTowers} / {GameLoopManager.MaxTowers}";
        TotalText.color = ShownTowers >= GameLoopManager.MaxTowers ? red : Color.white;

        for (int i = 0; i < Towers.Length; i++)
        {
            bool canPlace = GameLoopManager.CanPlaceTower(Towers[i]);
            Buttons[i].interactable = canPlace;
            Icons[i].color = canPlace ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            CostTexts[i].color = ShownMoney >= Towers[i].Cost ? new Color(1f, 0.85f, 0.2f) : red;

            if (LimitTexts[i] != null)
            {
                int count = GameLoopManager.CountTowers(Towers[i]);
                LimitTexts[i].text = $"{count}/{Towers[i].MaxPlaced}";
                LimitTexts[i].color = count >= Towers[i].MaxPlaced ? red : Color.white;
            }
        }
    }

    // Number of towers on the map, above the buttons
    private void BuildTotalText()
    {
        GameObject textObject = new GameObject("Tower Total", typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)textObject.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(260f, 46f);
        rect.anchoredPosition = new Vector2(Margin, Margin + ButtonSize + Spacing);
        Image background = textObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);
        background.raycastTarget = false;

        TotalText = AddCornerText(rect, "", TextAlignmentOptions.Midline);
    }

    private void CreateButton(TowerData tower, int slot)
    {
        GameObject buttonObject = new GameObject(tower.TowerName + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.SetParent(transform, false);
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.zero;
        buttonRect.pivot = Vector2.zero;
        buttonRect.sizeDelta = Vector2.one * ButtonSize;
        buttonRect.anchoredPosition = new Vector2(Margin + (ButtonSize + Spacing) * slot, Margin);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 0.75f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);
        colors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        button.onClick.AddListener(() => TowerPlacement.SetTowerToPlace(tower));

        Buttons[slot] = button;
        Icons[slot] = AddIcon(buttonRect, TowerIcons.Get(tower));

        if (slot < MaxHotkeySlots)
        {
            AddCornerText(buttonRect, (slot + 1).ToString(), TextAlignmentOptions.TopLeft);
        }
        CostTexts[slot] = AddCornerText(buttonRect, $"${tower.Cost}", TextAlignmentOptions.BottomRight);
        if (tower.MaxPlaced > 0)
        {
            LimitTexts[slot] = AddCornerText(buttonRect, "", TextAlignmentOptions.TopRight);
        }

        EventTrigger trigger = buttonObject.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerEnter, () => ShowTooltip(tower, buttonRect));
        AddTrigger(trigger, EventTriggerType.PointerExit, HideTooltip);
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private void ShowTooltip(TowerData tower, RectTransform button)
    {
        TooltipText.text = $"<b>{tower.TowerName}</b>  <color=#FFD833>${tower.Cost}</color>\n" + tower.GetStatsText();

        // Box grows with the text and sits right above the hovered button
        Tooltip.sizeDelta = TooltipText.GetPreferredValues(TooltipText.text) + Vector2.one * 32f;
        Tooltip.anchoredPosition = button.anchoredPosition + new Vector2(0f, ButtonSize + Spacing);
        Tooltip.SetAsLastSibling();
        Tooltip.gameObject.SetActive(true);
    }

    private void HideTooltip()
    {
        Tooltip.gameObject.SetActive(false);
    }

    private void BuildTooltip()
    {
        GameObject tooltipObject = new GameObject("Tower Tooltip", typeof(RectTransform), typeof(Image));
        Tooltip = (RectTransform)tooltipObject.transform;
        Tooltip.SetParent(transform, false);
        Tooltip.anchorMin = Vector2.zero;
        Tooltip.anchorMax = Vector2.zero;
        Tooltip.pivot = Vector2.zero;

        Image background = tooltipObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.85f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(Tooltip, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.one * 16f;
        textRect.offsetMax = -Vector2.one * 16f;

        TooltipText = textObject.GetComponent<TMP_Text>();
        TooltipText.fontSize = 28f;
        TooltipText.color = Color.white;
        TooltipText.raycastTarget = false;
        TooltipText.textWrappingMode = TextWrappingModes.NoWrap;

        tooltipObject.SetActive(false);
    }

    private Image AddIcon(RectTransform button, Sprite icon)
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(button, false);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.one * IconPadding;
        iconRect.offsetMax = -Vector2.one * IconPadding;

        Image image = iconObject.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text AddCornerText(RectTransform button, string content, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("Corner Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(button, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = 30f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
