using System;
using UnityEngine;
using UnityEngine.UI;

public enum UiLayoutMode { Desktop, Mobile }

public sealed class UiRoot : MonoBehaviour
{
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasScaler scaler;
    [SerializeField] private UiAssets assets;
    [SerializeField] private UiSafeArea safeArea;
    [SerializeField] private UiLayoutView desktop, mobile;
    [SerializeField] private Image background;

    private UiLayoutMode? lastMode;
    public Canvas Canvas => rootCanvas;
    public UiAssets Assets => assets;
    public UiLayoutView[] Views => new[] { desktop, mobile };
    public UiLayoutView View => LayoutMode == UiLayoutMode.Mobile ? mobile : desktop;
    public UiLayoutMode LayoutMode { get; private set; }
    public event Action<UiLayoutView, UiLayoutView> LayoutChanged;

    public static UiRoot Create()
    {
        var prefab = Resources.Load<UiRoot>("UI/UiRoot");
        if (!prefab) throw new InvalidOperationException("Resources/UI/UiRoot.prefab is missing.");
        return Instantiate(prefab);
    }

    private void Awake()
    {
        if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;
        RefreshLayout();
    }

    private void Update() => RefreshLayout();

    public void RefreshLayout()
    {
        var mode = Screen.height > Screen.width ? UiLayoutMode.Mobile : UiLayoutMode.Desktop;
        if (lastMode == mode) return;
        var old = View;
        LayoutMode = mode;
        scaler.referenceResolution = mode == UiLayoutMode.Mobile ? UiTheme.MobileReferenceResolution : UiTheme.DesktopReferenceResolution;
        // Expand preserves the entire authored design area at all aspect ratios.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        background.sprite = mode == UiLayoutMode.Mobile ? assets.MobileBackground : assets.DesktopBackground;
        desktop.gameObject.SetActive(mode == UiLayoutMode.Desktop);
        mobile.gameObject.SetActive(mode == UiLayoutMode.Mobile);
        (mode == UiLayoutMode.Mobile ? mobile : desktop).GetComponent<UiDesignFit>().Apply();
        safeArea.Apply();
        if (lastMode.HasValue) LayoutChanged?.Invoke(old, View);
        lastMode = mode;
    }

    public void Show(UiPage page, bool teacher, string username)
    {
        foreach (var view in Views)
            view.Show(page, teacher, username, teacher ? assets.TeacherAvatar : assets.StudentAvatar);
    }

    public void SetStatus(string value)
    {
        desktop.status.text = mobile.status.text = value;
    }

}
