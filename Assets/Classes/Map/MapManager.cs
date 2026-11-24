using UnityEngine;
using System.Collections.Generic;

// Which map is played. The beach is part of the scene, other maps are prefabs in Resources/Maps
// that replace it when the scene starts. The choice is saved between sessions.
public static class MapManager
{
    public const string BeachName = "Beach";
    private const string SaveKey = "Map";

    // Map that is in the scene right now
    public static string LoadedMap { get; private set; } = BeachName;

    // Every map prefab, the beach is not in here
    public static List<MapInfo> GetMapPrefabs()
    {
        List<MapInfo> maps = new List<MapInfo>();
        foreach (GameObject prefab in Resources.LoadAll<GameObject>("Maps"))
        {
            MapInfo map = prefab.GetComponent<MapInfo>();
            if (map != null)
            {
                maps.Add(map);
            }
        }
        return maps;
    }

    // Takes effect the next time the scene is loaded
    public static void Select(string mapName)
    {
        PlayerPrefs.SetString(SaveKey, mapName);
        PlayerPrefs.Save();
    }

    // Puts the selected map in the scene and returns the parent of its path nodes
    public static Transform Load(Transform beachNodes)
    {
        string selected = PlayerPrefs.GetString(SaveKey, BeachName);
        MapInfo prefab = GetMapPrefabs().Find(map => map.MapName == selected);
        if (prefab == null)
        {
            LoadedMap = BeachName;
            return beachNodes;
        }

        // Make the tower icons first, so they don't get the light of this map
        foreach (TowerData tower in Loadout.GetAllTowers())
        {
            TowerIcons.Get(tower);
        }

        // The nodes are inside the beach map, so this hides the whole beach
        beachNodes.parent.gameObject.SetActive(false);

        MapInfo loaded = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        ApplyLook(loaded);
        LoadedMap = loaded.MapName;
        return loaded.Nodes;
    }

    // Loading the scene again sets all of this back to the look of the beach
    private static void ApplyLook(MapInfo map)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = map.AmbientColor;
        RenderSettings.reflectionIntensity = 0.2f;
        RenderSettings.fog = map.FogDensity > 0f;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = map.BackgroundColor;
        RenderSettings.fogDensity = map.FogDensity;

        foreach (Light light in Object.FindObjectsByType<Light>())
        {
            if (light.type == LightType.Directional)
            {
                light.color = map.SunColor;
                light.intensity = map.SunIntensity;
            }
        }

        foreach (Camera camera in Camera.allCameras)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = map.BackgroundColor;
        }
    }
}
