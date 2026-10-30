using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Click a placed tower to see its stats and sell it. The panel is built in code at the bottom right.
public class TowerInfoPanel : MonoBehaviour
{
    [SerializeField] private Camera PlayerCamera;
    [SerializeField] private TowerPlacement TowerPlacement;
    [SerializeField] private Vector2 PanelSize = new Vector2(360f, 290f);
    [SerializeField] private float Margin = 20f;
    [SerializeField] private Color RangeColor = new Color(1f, 1f, 1f, 0.8f);

    private GameObject Panel;
    private TMP_Text TitleText;
    private TMP_Text StatsText;
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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
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

        TitleText.text = tower.Data != null ? tower.Data.TowerName : tower.name;
        StatsText.text =
            $"Range: {tower.Range}\n" +
            $"Fire rate: {tower.FireRate} /s\n" +
            $"Damage: {tower.Damage}";

        Panel.SetActive(true);

        RangeCircle.transform.position = tower.transform.position;
        RangeCircle.SetRadius(tower.Range);
        RangeCircle.SetVisible(true);
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
            // TODO: give money back once there is a money system
            Destroy(SelectedTower.gameObject);
        }
        Deselect();
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
        Panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        TitleText = CreateText("Title", Panel.transform, 38f, FontStyles.Bold);
        SetRect(TitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -66f), new Vector2(-16f, -10f));

        StatsText = CreateText("Stats", Panel.transform, 30f, FontStyles.Normal);
        SetRect(StatsText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 86f), new Vector2(-16f, -70f));

        GameObject sellObject = new GameObject("Sell Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform sellRect = (RectTransform)sellObject.transform;
        sellRect.SetParent(Panel.transform, false);
        SetRect(sellRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f), new Vector2(-16f, 74f));
        sellObject.GetComponent<Image>().color = new Color(0.8f, 0.25f, 0.25f);
        sellObject.GetComponent<Button>().onClick.AddListener(SellSelectedTower);

        TMP_Text sellText = CreateText("Text", sellObject.transform, 32f, FontStyles.Bold);
        sellText.text = "Sell";
        sellText.alignment = TextAlignmentOptions.Center;
        SetRect(sellText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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
