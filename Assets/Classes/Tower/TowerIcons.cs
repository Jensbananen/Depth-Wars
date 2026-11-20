using UnityEngine;
using System.Collections.Generic;

// Pictures of the towers for the UI. Uses TowerData.Icon when set, otherwise the prefab is
// rendered once with a temporary camera and the result is reused.
public static class TowerIcons
{
    private const int IconResolution = 128;
    // Unused layer so the icon camera only sees the tower it renders
    private const int IconLayer = 31;
    // Far away from the map so nothing else is in the picture
    private static readonly Vector3 IconStudioPosition = new Vector3(0f, -1000f, 0f);

    private static readonly Dictionary<TowerData, Sprite> Cache = new Dictionary<TowerData, Sprite>();

    public static Sprite Get(TowerData tower)
    {
        if (tower.Icon != null)
        {
            return tower.Icon;
        }

        // A cached sprite can be gone after a scene load, Unity then says it is null
        if (!Cache.TryGetValue(tower, out Sprite icon) || icon == null)
        {
            icon = Render(tower.TowerPrefab);
            Cache[tower] = icon;
        }
        return icon;
    }

    private static Sprite Render(GameObject prefab)
    {
        GameObject model = Object.Instantiate(prefab, IconStudioPosition, Quaternion.identity);
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
        Object.Destroy(cameraObject);
        // Destroy only happens at the end of the frame, hide it now so the next icon doesn't show this tower too
        model.SetActive(false);
        Object.Destroy(model);

        return Sprite.Create(texture, new Rect(0, 0, IconResolution, IconResolution), new Vector2(0.5f, 0.5f));
    }

    private static void SetLayerRecursive(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
