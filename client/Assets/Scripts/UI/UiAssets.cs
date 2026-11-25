using UnityEngine;

/// <summary>
/// Art references for the UI root prefab. Keeping these references serialized in
/// the prefab avoids runtime path lookups and keeps generated assets in use.
/// </summary>
public sealed class UiAssets : MonoBehaviour
{
    [SerializeField] private Sprite desktopBackground;
    [SerializeField] private Sprite mobileBackground;
    [SerializeField] private Sprite panelWhite;
    [SerializeField] private Sprite panelBlue;
    [SerializeField] private Sprite panelSuccess;
    [SerializeField] private Sprite panelWarning;
    [SerializeField] private Sprite buttonPrimary;
    [SerializeField] private Sprite buttonSecondary;
    [SerializeField] private Sprite buttonDanger;
    [SerializeField] private Sprite logo;
    [SerializeField] private Sprite studentAvatar;
    [SerializeField] private Sprite teacherAvatar;

    public Sprite DesktopBackground => desktopBackground;
    public Sprite MobileBackground => mobileBackground;
    public Sprite PanelWhite => panelWhite;
    public Sprite PanelBlue => panelBlue;
    public Sprite PanelSuccess => panelSuccess;
    public Sprite PanelWarning => panelWarning;
    public Sprite ButtonPrimary => buttonPrimary;
    public Sprite ButtonSecondary => buttonSecondary;
    public Sprite ButtonDanger => buttonDanger;
    public Sprite Logo => logo;
    public Sprite StudentAvatar => studentAvatar;
    public Sprite TeacherAvatar => teacherAvatar;
}
