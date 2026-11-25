using UnityEngine;
using System.Collections.Generic;

// Which map is played. Maps are prefabs in Resources/Maps, put in the scene when it starts.
// The ground and path of the beach are in the scene itself. The choice is saved between sessions.
public static class MapManager
{
    private const string BeachName = "Beach";
    private const string SaveKey = "Map";
    private const string WonSaveKey = "MapWon_";

    // Coins for winning a map for the first time
    public const int FirstWinCoins = 250;

    // Map that is in the scene right now
    public static string LoadedMap { get; private set; } = BeachName;

    // Every map prefab, in alphabetical order
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

    public static bool IsWon(string mapName)
    {
        return PlayerPrefs.GetInt(WonSaveKey + mapName, 0) == 1;
    }

    // Returns true only the first time the map in the scene is won
    public static bool MarkWon()
    {
        if (IsWon(LoadedMap))
        {
            return false;
        }

        PlayerPrefs.SetInt(WonSaveKey + LoadedMap, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void ResetWins()
    {
        foreach (MapInfo map in GetMapPrefabs())
        {
            PlayerPrefs.DeleteKey(WonSaveKey + map.MapName);
        }
        PlayerPrefs.Save();
    }

    // Puts the selected map in the scene and returns the parent of its path nodes
    public static Transform Load(Transform beachNodes)
    {
        List<MapInfo> maps = GetMapPrefabs();
        string selected = PlayerPrefs.GetString(SaveKey, BeachName);
        MapInfo prefab = maps.Find(map => map.MapName == selected);
        if (prefab == null)
        {
            prefab = maps.Find(map => map.MapName == BeachName);
        }
        LoadedMap = prefab.MapName;

        // Make the tower icons first, so they don't get the light of this map
        foreach (TowerData tower in Loadout.GetAllTowers())
        {
            TowerIcons.Get(tower);
        }

        MapInfo loaded = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        if (!loaded.KeepSceneLook)
        {
            ApplyLook(loaded);
        }
        if (loaded.Nodes == null)
        {
            return beachNodes;
        }

        // The nodes are inside the beach map, so this hides the whole beach
        beachNodes.parent.gameObject.SetActive(false);
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
