using UnityEngine;

public sealed class UiDesignFit : MonoBehaviour
{
    public Vector2 designSize;

    private void LateUpdate() => Apply();
    private void OnEnable() => Apply();

    public void Apply()
    {
        var rect = (RectTransform)transform;
        var available = ((RectTransform)rect.parent).rect.size;
        if (available.x <= 0 || available.y <= 0) return;
        var scale = Mathf.Min(available.x / designSize.x, available.y / designSize.y);
        rect.localScale = Vector3.one * scale;
    }
}
