using UnityEngine;
using TMPro;

// Colors, heading font and logo in the house style of Life Below Water (lifebelowwater.org)
public static class UIStyle
{
    // Dark navy of the website, behind all panels
    public static readonly Color Navy = Hex(0x081E35);

    // Buttons: blue of the logo for the main action, light blue for the others
    public static readonly Color Primary = Hex(0x0052A5);
    public static readonly Color Accent = Hex(0x35BCE7);
    public static readonly Color Neutral = Hex(0x1D3B5C);
    public static readonly Color Danger = Hex(0xCC3366);
    public static readonly Color Buy = Hex(0xFFA506);

    // Text: orange for money and other highlights, light blue of the logo for coins
    public static readonly Color Highlight = Hex(0xFFA506);
    public static readonly Color Coins = Hex(0x5BC8F5);

    // Backgrounds, from see-through to almost solid
    public static readonly Color Card = WithAlpha(Hex(0x12304F), 0.7f);
    public static readonly Color Overlay = WithAlpha(Navy, 0.65f);
    public static readonly Color Box = WithAlpha(Navy, 0.85f);
    public static readonly Color BoxHover = WithAlpha(Hex(0x1D3B5C), 0.9f);
    public static readonly Color BoxPressed = WithAlpha(Hex(0x2A5580), 0.95f);
    public static readonly Color MenuBackground = WithAlpha(Navy, 0.85f);
    public static readonly Color Panel = WithAlpha(Navy, 0.97f);

    private const string HeadingFontPath = "Fonts/BebasNeue-Regular";
    private const string LogoPath = "UI/LifeBelowWater-Logo";

    private static TMP_FontAsset HeadingFont;
    private static Sprite Logo;
    private static Sprite Glow;
    private static bool LogoLoaded;

    // Bebas Neue for titles, buttons and other bold text, like the headings on the website.
    // Characters it doesn't have come from the normal font.
    public static void ApplyHeadingFont(TMP_Text text)
    {
        if (HeadingFont == null)
        {
            Font font = Resources.Load<Font>(HeadingFontPath);
            if (font == null)
            {
                return;
            }

            HeadingFont = TMP_FontAsset.CreateFontAsset(font);
            if (TMP_Settings.defaultFontAsset != null)
            {
                HeadingFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            }
        }
        text.font = HeadingFont;
    }

    // Null when the logo isn't in Resources/UI
    public static Sprite GetLogo()
    {
        if (!LogoLoaded)
        {
            LogoLoaded = true;
            Texture2D texture = Resources.Load<Texture2D>(LogoPath);
            if (texture != null)
            {
                Logo = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }
        return Logo;
    }

    // White oval that fades out to the edges, stretched over the whole image it is used in
    public static Sprite GetGlow()
    {
        if (Glow == null)
        {
            const int width = 128;
            const int height = 64;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + 0.5f) / width * 2f - 1f;
                    float dy = (y + 0.5f) / height * 2f - 1f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    // Solid in the middle, then fades out until the edge of the oval
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, distance));
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            Glow = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }
        return Glow;
    }

    private static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
