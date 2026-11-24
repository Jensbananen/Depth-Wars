using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

// Puts the video part of the GameSettings into effect
public static class VideoSettings
{
    private static readonly Vector2Int[] CommonResolutions =
    {
        new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440), new Vector2Int(3840, 2160)
    };

    // The pipeline asset of the project is never changed, lower qualities use a copy of it
    private static RenderPipelineAsset OriginalPipeline;
    private static UniversalRenderPipelineAsset PipelineCopy;
    private static int OriginalVSync;
    private static GraphicsQuality AppliedQuality = GraphicsQuality.High;
    private static bool Started;

    private static Vector2Int AppliedResolution;
    private static bool AppliedFullscreen = true;

    public static void Apply()
    {
        if (!Started)
        {
            Started = true;
            OriginalPipeline = QualitySettings.renderPipeline;
            OriginalVSync = QualitySettings.vSyncCount;
            Application.quitting += Restore;
        }

        // With VSync on Unity ignores the limit
        QualitySettings.vSyncCount = GameSettings.VSync ? 1 : 0;
        Application.targetFrameRate = GameSettings.FpsLimit > 0 ? GameSettings.FpsLimit : -1;

        ApplyQuality();
        ApplyScreen();
    }

    // Resolutions that fit on this screen, smallest first
    public static List<Vector2Int> GetResolutions()
    {
        Vector2Int native = GetNativeResolution();
        List<Vector2Int> resolutions = new List<Vector2Int>();
        foreach (Vector2Int resolution in CommonResolutions)
        {
            if (resolution.x <= native.x && resolution.y <= native.y)
            {
                resolutions.Add(resolution);
            }
        }
        if (!resolutions.Contains(native))
        {
            resolutions.Add(native);
        }
        return resolutions;
    }

    // The chosen resolution, or that of the screen when the player never chose one
    public static Vector2Int GetResolution()
    {
        return GameSettings.ScreenWidth > 0
            ? new Vector2Int(GameSettings.ScreenWidth, GameSettings.ScreenHeight)
            : GetNativeResolution();
    }

    private static Vector2Int GetNativeResolution()
    {
        return new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);
    }

    private static void ApplyScreen()
    {
        Vector2Int resolution = GameSettings.ScreenWidth > 0 ? GetResolution() : Vector2Int.zero;
        if (resolution == AppliedResolution && GameSettings.Fullscreen == AppliedFullscreen)
        {
            return;
        }
        AppliedResolution = resolution;
        AppliedFullscreen = GameSettings.Fullscreen;

        Vector2Int size = GetResolution();
        // A window as big as the screen doesn't fit with its title bar
        if (!GameSettings.Fullscreen && GameSettings.ScreenWidth == 0)
        {
            size = CommonResolutions[0];
        }
        Screen.SetResolution(size.x, size.y, GameSettings.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    // Lower qualities render in a lower resolution and with less shadow
    private static void ApplyQuality()
    {
        if (GameSettings.Quality == AppliedQuality)
        {
            return;
        }

        UniversalRenderPipelineAsset original = OriginalPipeline as UniversalRenderPipelineAsset;
        if (original == null)
        {
            return;
        }
        AppliedQuality = GameSettings.Quality;

        if (AppliedQuality == GraphicsQuality.High)
        {
            QualitySettings.renderPipeline = OriginalPipeline;
            return;
        }

        if (PipelineCopy == null)
        {
            PipelineCopy = Object.Instantiate(original);
            PipelineCopy.hideFlags = HideFlags.DontSave;
        }
        bool low = AppliedQuality == GraphicsQuality.Low;
        PipelineCopy.renderScale = original.renderScale * (low ? 0.6f : 0.8f);
        // Not 0, a build doesn't have the shaders without shadow
        PipelineCopy.shadowDistance = original.shadowDistance * (low ? 0.25f : 0.5f);
        QualitySettings.renderPipeline = PipelineCopy;
    }

    // In the editor these would otherwise stay changed after Play mode
    private static void Restore()
    {
        Application.quitting -= Restore;
        QualitySettings.renderPipeline = OriginalPipeline;
        QualitySettings.vSyncCount = OriginalVSync;
        Application.targetFrameRate = -1;
        if (PipelineCopy != null)
        {
            Object.DestroyImmediate(PipelineCopy);
        }
        AppliedQuality = GraphicsQuality.High;
        Started = false;
    }
}
