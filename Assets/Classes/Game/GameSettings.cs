using UnityEngine;

public enum HealthBarMode
{
    Off,
    Damaged,    // only on enemies that lost HP
    Always
}

public enum GraphicsQuality
{
    Low,
    Medium,
    High
}

// Everything the player can change in the settings menu. Saved between sessions.
// Change a value and then call Apply.
public static class GameSettings
{
    private const string Prefix = "Setting.";

    // Called after Apply and after a reset
    public static event System.Action Changed;

    // Camera, 1 is the normal speed
    public static float CameraSpeed;
    public static float MouseSensitivity;

    // Video, a screen width of 0 means the player never chose a resolution
    public static bool Fullscreen;
    public static int ScreenWidth;
    public static int ScreenHeight;
    public static GraphicsQuality Quality;
    public static bool VSync;
    // 0 is unlimited
    public static int FpsLimit;

    // Gameplay
    public static int GameSpeed;
    public static bool AutoSkipWaves;
    public static bool AutoPause;

    // Interface
    public static HealthBarMode HealthBars;
    public static bool DamageNumbers;
    public static float UiScale;
    public static bool ShowFps;

    private static readonly string[] Keys =
    {
        "CameraSpeed", "MouseSensitivity", "Fullscreen", "ScreenWidth", "ScreenHeight", "Quality", "VSync", "FpsLimit",
        "GameSpeed", "AutoSkipWaves", "AutoPause", "HealthBars", "DamageNumbers", "UiScale", "ShowFps"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        CameraSpeed = PlayerPrefs.GetFloat(Prefix + "CameraSpeed", 1f);
        MouseSensitivity = PlayerPrefs.GetFloat(Prefix + "MouseSensitivity", 1f);

        Fullscreen = GetBool("Fullscreen", true);
        ScreenWidth = PlayerPrefs.GetInt(Prefix + "ScreenWidth", 0);
        ScreenHeight = PlayerPrefs.GetInt(Prefix + "ScreenHeight", 0);
        Quality = (GraphicsQuality)PlayerPrefs.GetInt(Prefix + "Quality", (int)GraphicsQuality.High);
        VSync = GetBool("VSync", false);
        FpsLimit = PlayerPrefs.GetInt(Prefix + "FpsLimit", 0);

        GameSpeed = PlayerPrefs.GetInt(Prefix + "GameSpeed", 1);
        AutoSkipWaves = GetBool("AutoSkipWaves", false);
        AutoPause = GetBool("AutoPause", false);

        HealthBars = (HealthBarMode)PlayerPrefs.GetInt(Prefix + "HealthBars", (int)HealthBarMode.Off);
        DamageNumbers = GetBool("DamageNumbers", false);
        UiScale = PlayerPrefs.GetFloat(Prefix + "UiScale", 1f);
        ShowFps = GetBool("ShowFps", false);

        VideoSettings.Apply();
    }

    // Uses the new values and remembers them
    public static void Apply()
    {
        PlayerPrefs.SetFloat(Prefix + "CameraSpeed", CameraSpeed);
        PlayerPrefs.SetFloat(Prefix + "MouseSensitivity", MouseSensitivity);

        SetBool("Fullscreen", Fullscreen);
        PlayerPrefs.SetInt(Prefix + "ScreenWidth", ScreenWidth);
        PlayerPrefs.SetInt(Prefix + "ScreenHeight", ScreenHeight);
        PlayerPrefs.SetInt(Prefix + "Quality", (int)Quality);
        SetBool("VSync", VSync);
        PlayerPrefs.SetInt(Prefix + "FpsLimit", FpsLimit);

        PlayerPrefs.SetInt(Prefix + "GameSpeed", GameSpeed);
        SetBool("AutoSkipWaves", AutoSkipWaves);
        SetBool("AutoPause", AutoPause);

        PlayerPrefs.SetInt(Prefix + "HealthBars", (int)HealthBars);
        SetBool("DamageNumbers", DamageNumbers);
        PlayerPrefs.SetFloat(Prefix + "UiScale", UiScale);
        SetBool("ShowFps", ShowFps);

        VideoSettings.Apply();
        Changed?.Invoke();
    }

    public static void ResetToDefaults()
    {
        foreach (string key in Keys)
        {
            PlayerPrefs.DeleteKey(Prefix + key);
        }

        Load();
        Changed?.Invoke();
    }

    private static bool GetBool(string key, bool defaultValue)
    {
        return PlayerPrefs.GetInt(Prefix + key, defaultValue ? 1 : 0) == 1;
    }

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);
    }
}
