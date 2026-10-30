using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Makes one button per TowerData in Resources/Towers, in a row from the bottom left
public class TowerShop : MonoBehaviour
{
    [SerializeField] private Button ButtonTemplate;
    [SerializeField] private TowerPlacement TowerPlacement;
    [SerializeField] private float Spacing = 10f;
    [SerializeField] private float IconPadding = 8f;
    [SerializeField] private int IconResolution = 128;

    // Unused layer so the icon camera only sees the tower it renders
    private const int IconLayer = 31;
    // Far away from the map so nothing else is in the picture
    private static readonly Vector3 IconStudioPosition = new Vector3(0f, -1000f, 0f);

    void Start()
    {
        TowerData[] towers = Resources.LoadAll<TowerData>("Towers");
        System.Array.Sort(towers, (a, b) => a.Cost.CompareTo(b.Cost));

        RectTransform templateRect = (RectTransform)ButtonTemplate.transform;
        float step = templateRect.rect.width + Spacing;

        for (int i = 0; i < towers.Length; i++)
        {
            TowerData tower = towers[i];

            Button button = Instantiate(ButtonTemplate, templateRect.parent);
            button.name = tower.TowerName + " Button";
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => TowerPlacement.SetTowerToPlace(tower));

            ((RectTransform)button.transform).anchoredPosition = templateRect.anchoredPosition + Vector2.right * step * i;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }

            Sprite icon = tower.Icon != null ? tower.Icon : RenderIcon(tower.TowerPrefab);
            AddIcon(button, icon);
        }

        ButtonTemplate.gameObject.SetActive(false);
    }

    private void AddIcon(Button button, Sprite icon)
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(button.transform, false);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.one * IconPadding;
        iconRect.offsetMax = -Vector2.one * IconPadding;

        Image image = iconObject.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
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
