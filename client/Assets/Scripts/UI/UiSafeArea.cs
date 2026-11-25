using UnityEngine;

// Like Solitaire's fitted window: keep the authored aspect and scale uniformly
// within the safe rect. The page's anchored elements retain their prefab layout.
[RequireComponent(typeof(RectTransform))]
public sealed class UiSafeArea : MonoBehaviour
{
    public void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        var rect = (RectTransform)transform;
        var safe = Screen.safeArea;
        rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnEnable() => Apply();
    private void LateUpdate() => Apply();
}
