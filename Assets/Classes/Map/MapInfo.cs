using UnityEngine;

// On the root of every map prefab in Resources/Maps. The beach map has none, that one is part of the scene.
public class MapInfo : MonoBehaviour
{
    public string MapName;
    public string Description;
    // Parent of the path nodes, the enemies walk past its children in order
    public Transform Nodes;

    [Header("Look")]
    public Color BackgroundColor = Color.blue;
    public Color AmbientColor = Color.gray;
    public Color SunColor = Color.white;
    public float SunIntensity = 1f;
    // Fog in the background color, 0 for no fog
    public float FogDensity = 0f;
}
