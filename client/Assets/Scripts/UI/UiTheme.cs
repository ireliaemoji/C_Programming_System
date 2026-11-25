using UnityEngine;

/// <summary>
/// Small, project-owned UI token set. It intentionally uses Unity UI primitives
/// so the client does not depend on a third-party UI package.
/// </summary>
public static class UiTheme
{
    public static readonly Vector2 DesktopReferenceResolution = new Vector2(2560f, 1440f);
    public static readonly Vector2 MobileReferenceResolution = new Vector2(1080f, 2160f);

    public static readonly Color Background = new Color(0.956f, 0.969f, 0.984f, 1f);
    public static readonly Color Surface = Color.white;
    public static readonly Color SurfaceRaised = new Color(0.918f, 0.953f, 0.980f, 1f);
    public static readonly Color Header = new Color(0.090f, 0.196f, 0.302f, 1f);
    public static readonly Color Sidebar = new Color(0.929f, 0.953f, 0.973f, 1f);
    public static readonly Color Primary = new Color(0.184f, 0.435f, 0.929f, 1f);
    public static readonly Color PrimaryText = new Color(0.090f, 0.196f, 0.302f, 1f);
    public static readonly Color TextPrimary = new Color(0.200f, 0.263f, 0.365f, 1f);
    public static readonly Color ButtonText = Color.white;
    public static readonly Color TextMuted = new Color(0.392f, 0.455f, 0.545f, 1f);
    public static readonly Color Divider = new Color(0.796f, 0.835f, 0.886f, 1f);
    public static readonly Color Warning = new Color(0.78f, 0.49f, 0.02f, 1f);

    public static Font RuntimeFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
