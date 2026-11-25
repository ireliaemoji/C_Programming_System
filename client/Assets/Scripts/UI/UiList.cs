using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UiList : MonoBehaviour
{
    public UiListItem itemTemplate;
    public GameObject emptyState;

    public void Clear()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (child == itemTemplate.gameObject || child == emptyState) continue;
            child.SetActive(false);
            Destroy(child);
        }
        itemTemplate.gameObject.SetActive(false);
        emptyState.SetActive(true);
    }

    public void AddExpanded(string title, string detail, string actionLabel, Action action)
    {
        var item = Add(title, detail, actionLabel, action);
        item.title.supportRichText = false;
        item.detail.supportRichText = false;
        item.detail.alignment = TextAnchor.UpperLeft;
        var detailRect = item.detail.rectTransform;
        var detailTop = -detailRect.anchoredPosition.y;
        var detailHeight = Mathf.Ceil(item.detail.preferredHeight);
        detailRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, detailHeight);
        var layout = item.GetComponent<LayoutElement>();
        var actionRect = (RectTransform)item.action.transform;
        var actionTop = detailTop + detailHeight + 16f;
        if (action != null)
        {
            var position = actionRect.anchoredPosition;
            position.y = -actionTop;
            actionRect.anchoredPosition = position;
            actionTop += actionRect.rect.height;
        }
        layout.preferredHeight = Mathf.Max(layout.preferredHeight, actionTop + 16f);
    }

    public UiListItem Add(string title, string detail, string actionLabel = null, Action action = null)
    {
        emptyState.SetActive(false);
        var item = Instantiate(itemTemplate, transform);
        item.gameObject.SetActive(true);
        item.title.text = title;
        item.detail.text = detail;
        // The template is also used as the hidden source item. Reset the
        // runtime button state and listeners so every cloned row is clickable
        // after a list refresh.
        item.action.onClick.RemoveAllListeners();
        var hasLabel = actionLabel != null;
        item.action.gameObject.SetActive(hasLabel);
        item.action.interactable = action != null;
        item.actionLabel.text = actionLabel ?? string.Empty;
        if (action != null) item.action.onClick.AddListener(() => action.Invoke());
        return item;
    }
}
