using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// The Inventory and Shop screens from the main menu. Both show a card per tower.
// Inventory: equip up to Loadout.MaxEquipped towers to bring into the game.
// Shop: will sell towers for coins later, for now every tower is already owned.
public class TowerMenu : MonoBehaviour
{
    [SerializeField] private Vector2 PanelSize = new Vector2(1560f, 880f);
    [SerializeField] private Vector2 CardSize = new Vector2(230f, 480f);

    public static bool IsOpen;

    private static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.13f, 0.97f);
    private static readonly Color CardColor = new Color(0f, 0f, 0f, 0.5f);
    private static readonly Color EquipColor = new Color(0.25f, 0.6f, 0.3f);
    private static readonly Color UnequipColor = new Color(0.8f, 0.25f, 0.25f);
    private static readonly Color DisabledColor = new Color(0.3f, 0.3f, 0.3f);

    private GameObject InventoryScreen;
    private GameObject ShopScreen;
    private TMP_Text EquippedCountText;
    private readonly List<System.Action> InventoryRefreshActions = new List<System.Action>();

    void Start()
    {
        IsOpen = false;
        InventoryScreen = BuildScreen("Inventory", "Inventory", out RectTransform inventoryGrid, out EquippedCountText);
        ShopScreen = BuildScreen("Shop", "Shop", out RectTransform shopGrid, out TMP_Text shopInfo);
        shopInfo.text = "Coins: coming soon";

        foreach (TowerData tower in Loadout.GetAllTowers())
        {
            AddInventoryCard(inventoryGrid, tower);
            AddShopCard(shopGrid, tower);
        }
    }

    void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SettingsMenu.EscapeHandledFrame = Time.frameCount;
            Close();
        }
    }

    public void OpenInventory()
    {
        RefreshInventory();
        Show(InventoryScreen);
    }

    public void OpenShop()
    {
        Show(ShopScreen);
    }

    private void Show(GameObject screen)
    {
        IsOpen = true;
        // On top of the main menu
        screen.transform.SetAsLastSibling();
        screen.SetActive(true);
    }

    private void Close()
    {
        IsOpen = false;
        InventoryScreen.SetActive(false);
        ShopScreen.SetActive(false);
    }

    private void RefreshInventory()
    {
        EquippedCountText.text = $"Equipped: {Loadout.GetEquipped().Count} / {Loadout.MaxEquipped}";
        foreach (System.Action refresh in InventoryRefreshActions)
        {
            refresh();
        }
    }

    private void AddInventoryCard(RectTransform grid, TowerData tower)
    {
        Button button = AddCard(grid, tower, out TMP_Text buttonText);
        Image buttonImage = button.GetComponent<Image>();

        InventoryRefreshActions.Add(() =>
        {
            bool equipped = Loadout.IsEquipped(tower);
            bool canEquip = !equipped && !Loadout.IsFull();
            buttonText.text = equipped ? "Unequip" : (canEquip ? "Equip" : "Full");
            buttonImage.color = equipped ? UnequipColor : (canEquip ? EquipColor : DisabledColor);
            button.interactable = equipped || canEquip;
        });

        button.onClick.AddListener(() =>
        {
            if (Loadout.IsEquipped(tower))
            {
                Loadout.Unequip(tower);
            }
            else
            {
                Loadout.TryEquip(tower);
            }
            RefreshInventory();
        });
    }

    private void AddShopCard(RectTransform grid, TowerData tower)
    {
        Button button = AddCard(grid, tower, out TMP_Text buttonText);
        bool owned = Loadout.IsOwned(tower);
        buttonText.text = owned ? "Owned" : "Buy";
        button.GetComponent<Image>().color = DisabledColor;
        // Buying comes with the coins
        button.interactable = false;
    }

    // A card with the icon, name, in-game price and stats, and a button at the bottom
    private Button AddCard(RectTransform grid, TowerData tower, out TMP_Text buttonText)
    {
        GameObject card = new GameObject(tower.TowerName, typeof(RectTransform), typeof(Image));
        RectTransform cardRect = (RectTransform)card.transform;
        cardRect.SetParent(grid, false);
        card.GetComponent<Image>().color = CardColor;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(cardRect, false);
        SetRect(iconRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -160f), new Vector2(-40f, -10f));
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = TowerIcons.Get(tower);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text title = CreateText("Name", cardRect, 28f, FontStyles.Bold);
        title.text = $"{tower.TowerName}\n<color=#FFD833>${tower.Cost}</color>";
        title.alignment = TextAlignmentOptions.Top;
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -240f), new Vector2(-8f, -160f));

        TMP_Text stats = CreateText("Stats", cardRect, 20f, FontStyles.Normal);
        stats.text = tower.GetStatsText();
        stats.alignment = TextAlignmentOptions.TopLeft;
        SetRect(stats.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 70f), new Vector2(-8f, -244f));

        Button button = CreateButton("Button", cardRect, "", EquipColor, out buttonText);
        SetRect((RectTransform)button.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(12f, 12f), new Vector2(-12f, 62f));
        return button;
    }

    private GameObject BuildScreen(string name, string titleText, out RectTransform grid, out TMP_Text infoText)
    {
        // Dark layer over everything so the main menu behind it can't be clicked
        GameObject screen = new GameObject(name + " Screen", typeof(RectTransform), typeof(Image));
        RectTransform screenRect = (RectTransform)screen.transform;
        screenRect.SetParent(transform, false);
        SetRect(screenRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        screen.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform panel = (RectTransform)panelObject.transform;
        panel.SetParent(screenRect, false);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = PanelSize;
        panelObject.GetComponent<Image>().color = PanelColor;

        TMP_Text title = CreateText("Title", panel, 64f, FontStyles.Bold);
        title.text = titleText;
        title.alignment = TextAlignmentOptions.TopLeft;
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -110f), new Vector2(-40f, -24f));

        infoText = CreateText("Info", panel, 36f, FontStyles.Bold);
        infoText.color = new Color(1f, 0.85f, 0.2f);
        infoText.alignment = TextAlignmentOptions.TopRight;
        SetRect(infoText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -100f), new Vector2(-40f, -40f));

        // Cards fill the panel in rows, more towers simply wrap to the next row
        GameObject gridObject = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        grid = (RectTransform)gridObject.transform;
        grid.SetParent(panel, false);
        SetRect(grid, Vector2.zero, Vector2.one, new Vector2(40f, 120f), new Vector2(-40f, -130f));
        GridLayoutGroup layout = gridObject.GetComponent<GridLayoutGroup>();
        layout.cellSize = CardSize;
        layout.spacing = new Vector2(20f, 20f);
        layout.childAlignment = TextAnchor.UpperCenter;

        Button back = CreateButton("Back Button", panel, "Back", DisabledColor, out _);
        RectTransform backRect = (RectTransform)back.transform;
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.sizeDelta = new Vector2(320f, 80f);
        backRect.anchoredPosition = new Vector2(0f, 24f);
        back.onClick.AddListener(Close);

        screen.SetActive(false);
        return screen;
    }

    private Button CreateButton(string name, Transform parent, string label, Color color, out TMP_Text text)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = color;

        text = CreateText("Text", buttonObject.transform, 30f, FontStyles.Bold);
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
