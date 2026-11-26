using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Click a placed tower to see its stats, pick what it shoots at, upgrade it or sell it. The panel is built in code at the bottom right.
public class TowerInfoPanel : MonoBehaviour
{
    [SerializeField] private Camera PlayerCamera;
    [SerializeField] private TowerPlacement TowerPlacement;
    [SerializeField] private Vector2 PanelSize = new Vector2(360f, 560f);
    [SerializeField] private float Margin = 20f;
    [SerializeField] private Color RangeColor = new Color(1f, 1f, 1f, 0.8f);
    // Part of the tower cost you get back when selling
    [SerializeField] [Range(0f, 1f)] private float SellRefund = 0.7f;

    private GameObject Panel;
    private TMP_Text TitleText;
    private TMP_Text StatsText;
    private TMP_Text SellText;
    private Button UpgradeButton;
    private TMP_Text UpgradeText;
    private TMP_Text TargetText;
    private int ShownMoney = -1;
    private float ShownBuff;
    private TowerBehaviour SelectedTower;
    private CircleIndicator RangeCircle;

    void Start()
    {
        BuildPanel();
        RangeCircle = CircleIndicator.Create("Range Circle", 0.06f);
        RangeCircle.SetColor(RangeColor);
        Deselect();
    }

    // Update is called once per frame
    void Update()
    {
        if (TowerPlacement.IsPlacing)
        {
            if (SelectedTower != null)
            {
                Deselect();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && SelectedTower != null)
        {
            SettingsMenu.EscapeHandledFrame = Time.frameCount;
            Deselect();
        }

        // Upgrade button turns grey when the next upgrade is too expensive
        if (SelectedTower != null && GameLoopManager.Money != ShownMoney)
        {
            ShownMoney = GameLoopManager.Money;
            UpdateUpgradeButton();
        }

        // A booster was placed, upgraded or sold nearby
        if (SelectedTower != null && SelectedTower.ReceivedBuff != ShownBuff)
        {
            RefreshPanel();
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Clicks on the panel itself or the shop buttons
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray camray = PlayerCamera.ScreenPointToRay(Input.mousePosition);
            TowerBehaviour tower = null;
            if (Physics.Raycast(camray, out RaycastHit hitInfo, 100f))
            {
                tower = hitInfo.collider.GetComponentInParent<TowerBehaviour>();
            }

            if (tower != null)
            {
                Select(tower);
            }
            else
            {
                Deselect();
            }
        }
    }

    private void Select(TowerBehaviour tower)
    {
        SelectedTower = tower;
        RefreshPanel();
        Panel.SetActive(true);

        RangeCircle.transform.position = tower.transform.position;
        RangeCircle.SetRadius(tower.Range);
        RangeCircle.SetVisible(tower.Range > 0f);
    }

    private void RefreshPanel()
    {
        TowerBehaviour tower = SelectedTower;
        TitleText.text = tower.Data != null ? tower.Data.TowerName : tower.name;

        TowerUpgrade next = tower.NextUpgrade;
        StatsText.text =
            $"Level: {tower.Level} / {tower.MaxLevel}\n" +
            tower.GetStatsText() + "\n" +
            (next != null ? $"<color=#FFA506>Next: {DescribeUpgrade(tower, next)}</color>" : "<color=#FFA506>Max level</color>");

        ShownBuff = tower.ReceivedBuff;
        TargetText.text = $"Target: {tower.TargetMode}";
        TargetText.transform.parent.gameObject.SetActive(tower.Attacks);
        SellText.text = $"Sell (+${GetSellValue(tower)})";
        UpdateUpgradeButton();
    }

    private void UpdateUpgradeButton()
    {
        TowerUpgrade next = SelectedTower.NextUpgrade;
        if (next == null)
        {
            UpgradeText.text = "Max level";
            UpgradeButton.interactable = false;
            return;
        }

        UpgradeText.text = $"Upgrade (${next.Cost})";
        UpgradeButton.interactable = GameLoopManager.Money >= next.Cost;
    }

    private string DescribeUpgrade(TowerBehaviour tower, TowerUpgrade upgrade)
    {
        List<string> parts = new List<string>();
        if (upgrade.Damage != 0f) parts.Add($"+{upgrade.Damage:0.##} damage");
        if (upgrade.Range != 0f) parts.Add($"+{upgrade.Range:0.##} range");
        if (upgrade.FireRate != 0f) parts.Add($"+{upgrade.FireRate:0.##} fire rate");
        if (upgrade.Slow != 0f) parts.Add($"+{upgrade.Slow * 100f:0}% slow");
        if (upgrade.FireRateBuff != 0f) parts.Add($"+{upgrade.FireRateBuff * 100f:0}% boost");
        if (upgrade.MoneyPerWave != 0) parts.Add($"+${upgrade.MoneyPerWave} per wave");
        if (tower.HasUnits && tower.GetUnit(tower.Level + 1) != tower.GetUnit(tower.Level))
        {
            FriendlyUnit unit = tower.GetUnit(tower.Level + 1);
            parts.Add($"{unit.UnitName} ({unit.Health:0} HP) every {tower.SummonInterval + upgrade.SummonInterval:0.##}s");
        }
        return string.Join(", ", parts);
    }

    // Goes to the next mode, after the last one back to the first
    private void CycleTargetMode()
    {
        if (SelectedTower == null)
        {
            return;
        }

        int modeCount = System.Enum.GetValues(typeof(TargetMode)).Length;
        SelectedTower.TargetMode = (TargetMode)(((int)SelectedTower.TargetMode + 1) % modeCount);
        RefreshPanel();
    }

    private void UpgradeSelectedTower()
    {
        TowerUpgrade next = SelectedTower != null ? SelectedTower.NextUpgrade : null;
        if (next == null || !GameLoopManager.TrySpendMoney(next.Cost))
        {
            return;
        }

        SelectedTower.ApplyUpgrade(next);
        RefreshPanel();
        RangeCircle.SetRadius(SelectedTower.Range);
    }

    private void Deselect()
    {
        SelectedTower = null;
        Panel.SetActive(false);
        RangeCircle.SetVisible(false);
    }

    private void SellSelectedTower()
    {
        if (SelectedTower != null)
        {
            GameLoopManager.AddMoney(GetSellValue(SelectedTower));
            GameLoopManager.TowersInGame.Remove(SelectedTower);
            Destroy(SelectedTower.gameObject);
        }
        Deselect();
    }

    private int GetSellValue(TowerBehaviour tower)
    {
        int totalCost = (tower.Data != null ? tower.Data.Cost : 0) + tower.UpgradeCost;
        return Mathf.RoundToInt(totalCost * SellRefund);
    }

    private void BuildPanel()
    {
        Panel = new GameObject("Tower Info Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = (RectTransform)Panel.transform;
        panelRect.SetParent(transform, false);
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-Margin, Margin);
        panelRect.sizeDelta = PanelSize;
        Panel.GetComponent<Image>().color = UIStyle.Box;

        TitleText = CreateText("Title", Panel.transform, 38f, FontStyles.Bold);
        SetRect(TitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -66f), new Vector2(-16f, -10f));

        StatsText = CreateText("Stats", Panel.transform, 28f, FontStyles.Normal);
        SetRect(StatsText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 226f), new Vector2(-16f, -70f));

        GameObject targetObject = new GameObject("Target Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform targetRect = (RectTransform)targetObject.transform;
        targetRect.SetParent(Panel.transform, false);
        SetRect(targetRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 156f), new Vector2(-16f, 214f));
        targetObject.GetComponent<Image>().color = UIStyle.Accent;
        targetObject.GetComponent<Button>().onClick.AddListener(CycleTargetMode);

        TargetText = CreateText("Text", targetObject.transform, 30f, FontStyles.Bold);
        TargetText.alignment = TextAlignmentOptions.Center;
        SetRect(TargetText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject upgradeObject = new GameObject("Upgrade Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform upgradeRect = (RectTransform)upgradeObject.transform;
        upgradeRect.SetParent(Panel.transform, false);
        SetRect(upgradeRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 86f), new Vector2(-16f, 144f));
        upgradeObject.GetComponent<Image>().color = UIStyle.Primary;
        UpgradeButton = upgradeObject.GetComponent<Button>();
        UpgradeButton.onClick.AddListener(UpgradeSelectedTower);

        UpgradeText = CreateText("Text", upgradeObject.transform, 30f, FontStyles.Bold);
        UpgradeText.alignment = TextAlignmentOptions.Center;
        SetRect(UpgradeText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject sellObject = new GameObject("Sell Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform sellRect = (RectTransform)sellObject.transform;
        sellRect.SetParent(Panel.transform, false);
        SetRect(sellRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f), new Vector2(-16f, 74f));
        sellObject.GetComponent<Image>().color = UIStyle.Danger;
        sellObject.GetComponent<Button>().onClick.AddListener(SellSelectedTower);

        SellText = CreateText("Text", sellObject.transform, 32f, FontStyles.Bold);
        SellText.alignment = TextAlignmentOptions.Center;
        SetRect(SellText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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
