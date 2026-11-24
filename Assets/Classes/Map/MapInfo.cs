using UnityEngine;

// On the root of every map prefab in Resources/Maps.
public class MapInfo : MonoBehaviour
{
    public string MapName;
    public string Description;
    // Parent of the path nodes, the enemies walk past its children in order.
    // Empty for the beach: its ground and path are in the scene, the prefab only decorates it.
    public Transform Nodes;

    [Header("Look")]
    // Uses the sky and light of the scene, then BackgroundColor is only for the card in the menu
    public bool KeepSceneLook;
    public Color BackgroundColor = Color.blue;
    public Color AmbientColor = Color.gray;
    public Color SunColor = Color.white;
    public float SunIntensity = 1f;
    // Fog in the background color, 0 for no fog
    public float FogDensity = 0f;
}
