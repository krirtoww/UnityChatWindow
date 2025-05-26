using UnityEngine.EventSystems;
using UnityEngine;

public class DragMoveHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("需要拖动的目标")]
    public RectTransform targetToMove;

    [Header("自定义鼠标控制器")]
    public CustomCursorManager cursorManager;

    private Vector2 offset;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetToMove == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetToMove.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        offset = targetToMove.anchoredPosition - localPoint;

        if (cursorManager == null) return;
        cursorManager._isHolding = true;
        cursorManager.SetDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetToMove == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetToMove.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        targetToMove.anchoredPosition = localPoint + offset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (cursorManager == null) return;
        cursorManager._isHolding = false;
        cursorManager.SetDefault();
    }

    private void OnDisable()
    {
        cursorManager?.SetDefault();
    }
}
