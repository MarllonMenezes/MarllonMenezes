using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AlbaWorld.UI;

public sealed class DraggableSceneElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string ItemId = string.Empty;
    public Action<Vector2>? Changed;
    private RectTransform? _parent;

    public void OnBeginDrag(PointerEventData eventData) => _parent = transform.parent as RectTransform;

    public void OnDrag(PointerEventData eventData)
    {
        if (_parent == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, eventData.position, eventData.pressEventCamera, out var local);
        var half = _parent.rect.size / 2f;
        var rect = (RectTransform)transform;
        var extent = rect.rect.size / 2f;
        rect.anchoredPosition = new Vector2(
            Mathf.Clamp(local.x, -half.x + extent.x, half.x - extent.x),
            Mathf.Clamp(local.y, -half.y + extent.y, half.y - extent.y));
        Changed?.Invoke(NormalizedPosition());
    }

    public void OnEndDrag(PointerEventData eventData) { }

    public Vector2 NormalizedPosition()
    {
        if (_parent == null) return new Vector2(0.5f, 0.5f);
        var half = _parent.rect.size / 2f;
        var pos = ((RectTransform)transform).anchoredPosition;
        return new Vector2(Mathf.InverseLerp(-half.x, half.x, pos.x), Mathf.InverseLerp(-half.y, half.y, pos.y));
    }
}
