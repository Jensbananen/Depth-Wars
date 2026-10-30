using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Makes one button per TowerData in Resources/Towers, in a row from the bottom left.
// Slot 1 to 9 can also be picked with the number keys.
public class TowerShop : MonoBehaviour
{
    [SerializeField] private TowerPlacement TowerPlacement;
    [SerializeField] private float ButtonSize = 150f;
    [SerializeField] private float Spacing = 15f;
    [SerializeField] private float Margin = 20f;
    [SerializeField] private float IconPadding = 12f;
    [SerializeField] private int IconResolution = 128;

    private const int MaxHotkeySlots = 9;

    // Unused layer so the icon camera only sees the tower it renders
    private const int IconLayer = 31;
    // Far away from the map so nothing else is in the picture
    private static readonly Vector3 IconStudioPosition = new Vector3(0f, -1000f, 0f);

    private TowerData[] Towers;

    void Start()
    {
        Towers = Resources.LoadAll<TowerData>("Towers");
        System.Array.Sort(Towers, (a, b) => a.Cost.CompareTo(b.Cost));

        for (int i = 0; i < Towers.Length; i++)
        {
            CreateButton(Towers[i], i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        int slots = Mathf.Min(Towers.Length, MaxHotkeySlots);
        for (int i = 0; i < slots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                TowerPlacement.SetTowerToPlace(Towers[i]);
            }
        }
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

        Sprite icon = tower.Icon != null ? tower.Icon : RenderIcon(tower.TowerPrefab);
        AddIcon(buttonRect, icon);

        if (slot < MaxHotkeySlots)
        {
            AddSlotNumber(buttonRect, slot + 1);
        }
    }

    private void AddIcon(RectTransform button, Sprite icon)
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
    }

    private void AddSlotNumber(RectTransform button, int number)
    {
        GameObject textObject = new GameObject("Slot Number", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(button, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = number.ToString();
        text.fontSize = 30f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
    }

    // Renders the prefab once with a temporary camera and turns it into a sprite
    private Sprite RenderIcon(GameObject prefab)
    {
        GameObject model = Instantiate(prefab, IconStudioPosition, Quaternion.identity);
        SetLayerRecursive(model, IconLayer);

        Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(renderer.bounds);
        }

        GameObject cameraObject = new GameObject("Icon Camera");
        Camera iconCamera = cameraObject.AddComponent<Camera>();
        iconCamera.enabled = false;
        iconCamera.cullingMask = 1 << IconLayer;
        iconCamera.clearFlags = CameraClearFlags.SolidColor;
        iconCamera.backgroundColor = Color.clear;
        iconCamera.fieldOfView = 30f;

        // Look at the tower slightly from above, far enough back that it fits
        float distance = bounds.extents.magnitude / Mathf.Sin(iconCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 viewDirection = new Vector3(1f, 0.8f, -1f).normalized;
        cameraObject.transform.position = bounds.center + viewDirection * distance;
        cameraObject.transform.LookAt(bounds.center);
        iconCamera.nearClipPlane = 0.01f;
        iconCamera.farClipPlane = distance * 2f;

        RenderTexture renderTexture = RenderTexture.GetTemporary(IconResolution, IconResolution, 24, RenderTextureFormat.ARGB32);
        iconCamera.targetTexture = renderTexture;
        iconCamera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(IconResolution, IconResolution, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, IconResolution, IconResolution), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;

        iconCamera.targetTexture = null;
        RenderTexture.ReleaseTemporary(renderTexture);
        Destroy(cameraObject);
        Destroy(model);

        return Sprite.Create(texture, new Rect(0, 0, IconResolution, IconResolution), new Vector2(0.5f, 0.5f));
    }

    private void SetLayerRecursive(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
