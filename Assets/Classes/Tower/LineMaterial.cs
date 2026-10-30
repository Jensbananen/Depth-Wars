using UnityEngine;

// Shared unlit material for LineRenderers, colored with the line's start/end color
public static class LineMaterial
{
    private static Material Material;

    public static Material Get()
    {
        if (Material == null)
        {
            Material = new Material(Shader.Find("Sprites/Default"));
        }
        return Material;
    }
}
