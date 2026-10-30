using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Shows the name and HP of the enemy under the mouse in a small box next to the cursor
public class EnemyHoverInfo : MonoBehaviour
{
    [SerializeField] private Camera PlayerCamera;
    [SerializeField] private Vector2 MouseOffset = new Vector2(20f, 20f);
    [SerializeField] private float FontSize = 26f;
    [SerializeField] private float Padding = 10f;

    private RectTransform Tooltip;
    private TMP_Text TooltipText;
    private Canvas Canvas;

    void Start()
    {
        Canvas = GetComponentInParent<Canvas>();
        BuildTooltip();
        Tooltip.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Enemy enemy = GetHoveredEnemy();
        if (enemy == null)
        {
            Tooltip.gameObject.SetActive(false);
            return;
        }

        string enemyName = enemy.name.Replace("(Clone)", "").Trim();
        TooltipText.text = $"{enemyName}\nHP: {Mathf.CeilToInt(enemy.Health)} / {Mathf.CeilToInt(enemy.MaxHealth)}";

        // Box grows with the text
        Vector2 textSize = TooltipText.GetPreferredValues(TooltipText.text);
        Tooltip.sizeDelta = textSize + Vector2.one * Padding * 2f;

        // Next to the mouse, but kept inside the screen
        float scale = Canvas.scaleFactor;
        Vector2 size = Tooltip.sizeDelta * scale;
        Vector2 position = (Vector2)Input.mousePosition + MouseOffset * scale;
        position.x = Mathf.Min(position.x, Screen.width - size.x);
        position.y = Mathf.Min(position.y, Screen.height - size.y);
        Tooltip.position = position;

        // Draw on top of the other UI
        Tooltip.SetAsLastSibling();
        Tooltip.gameObject.SetActive(true);
    }

    private Enemy GetHoveredEnemy()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return null;
        }

        Ray camray = PlayerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(camray, out RaycastHit hitInfo, 100f))
        {
            return hitInfo.collider.GetComponentInParent<Enemy>();
        }
        return null;
    }

    private void BuildTooltip()
    {
        GameObject tooltipObject = new GameObject("Enemy Tooltip", typeof(RectTransform), typeof(Image));
        Tooltip = (RectTransform)tooltipObject.transform;
        Tooltip.SetParent(transform, false);
        Tooltip.pivot = Vector2.zero;

        Image background = tooltipObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(Tooltip, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.one * Padding;
        textRect.offsetMax = -Vector2.one * Padding;

        TooltipText = textObject.GetComponent<TMP_Text>();
        TooltipText.fontSize = FontSize;
        TooltipText.color = Color.white;
        TooltipText.raycastTarget = false;
        TooltipText.textWrappingMode = TextWrappingModes.NoWrap;
    }
}
