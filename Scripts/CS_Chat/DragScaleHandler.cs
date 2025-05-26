using UnityEngine;
using UnityEngine.EventSystems;

public class DragScaleHandler : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public enum ScaleDirection
    {
        Left, Right, Top, Bottom,
        TopLeft, TopRight, BottomLeft, BottomRight
    }

    public ScaleDirection direction;
    public RectTransform targetRect;
    public RectTransform canvasRect;
    public CustomCursorManager cursorManager;

    public float minWidth = 240f;
    public float minHeight = 300f;
    public float maxWidth = 1200f;
    public float maxHeight = 700f;

    private Vector2 startMouseLocalToParent;
    private Vector2 startOffsetMin;
    private Vector2 startOffsetMax;

    private bool isDragging = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        cursorManager?.SetScaleCursor(direction);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging)
            cursorManager?.SetDefault();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetRect == null || canvasRect == null) return;

        isDragging = true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out startMouseLocalToParent);

        startOffsetMin = targetRect.offsetMin;
        startOffsetMax = targetRect.offsetMax;

        if (cursorManager == null) return;
        cursorManager._isHolding = true;
        cursorManager.SetScaleCursor(direction);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || targetRect == null || canvasRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 currentMouseLocalToParent);

        Vector2 delta = currentMouseLocalToParent - startMouseLocalToParent;

        Vector2 newOffsetMin = startOffsetMin;
        Vector2 newOffsetMax = startOffsetMax;

        switch (direction)
        {
            case ScaleDirection.Left:
                newOffsetMin.x = Mathf.Min(startOffsetMin.x + delta.x, startOffsetMax.x - minWidth);
                newOffsetMin.x = Mathf.Max(newOffsetMin.x, startOffsetMax.x - maxWidth);
                break;

            case ScaleDirection.Right:
                newOffsetMax.x = Mathf.Max(startOffsetMax.x + delta.x, startOffsetMin.x + minWidth);
                newOffsetMax.x = Mathf.Min(newOffsetMax.x, startOffsetMin.x + maxWidth);
                break;

            case ScaleDirection.Top:
                newOffsetMax.y = Mathf.Max(startOffsetMax.y + delta.y, startOffsetMin.y + minHeight);
                newOffsetMax.y = Mathf.Min(newOffsetMax.y, startOffsetMin.y + maxHeight);
                break;

            case ScaleDirection.Bottom:
                newOffsetMin.y = Mathf.Min(startOffsetMin.y + delta.y, startOffsetMax.y - minHeight);
                newOffsetMin.y = Mathf.Max(newOffsetMin.y, startOffsetMax.y - maxHeight);
                break;

            case ScaleDirection.TopLeft:
                newOffsetMin.x = Mathf.Min(startOffsetMin.x + delta.x, startOffsetMax.x - minWidth);
                newOffsetMin.x = Mathf.Max(newOffsetMin.x, startOffsetMax.x - maxWidth);

                newOffsetMax.y = Mathf.Max(startOffsetMax.y + delta.y, startOffsetMin.y + minHeight);
                newOffsetMax.y = Mathf.Min(newOffsetMax.y, startOffsetMin.y + maxHeight);
                break;

            case ScaleDirection.TopRight:
                newOffsetMax.x = Mathf.Max(startOffsetMax.x + delta.x, startOffsetMin.x + minWidth);
                newOffsetMax.x = Mathf.Min(newOffsetMax.x, startOffsetMin.x + maxWidth);

                newOffsetMax.y = Mathf.Max(startOffsetMax.y + delta.y, startOffsetMin.y + minHeight);
                newOffsetMax.y = Mathf.Min(newOffsetMax.y, startOffsetMin.y + maxHeight);
                break;

            case ScaleDirection.BottomLeft:
                newOffsetMin.x = Mathf.Min(startOffsetMin.x + delta.x, startOffsetMax.x - minWidth);
                newOffsetMin.x = Mathf.Max(newOffsetMin.x, startOffsetMax.x - maxWidth);

                newOffsetMin.y = Mathf.Min(startOffsetMin.y + delta.y, startOffsetMax.y - minHeight);
                newOffsetMin.y = Mathf.Max(newOffsetMin.y, startOffsetMax.y - maxHeight);
                break;

            case ScaleDirection.BottomRight:
                newOffsetMax.x = Mathf.Max(startOffsetMax.x + delta.x, startOffsetMin.x + minWidth);
                newOffsetMax.x = Mathf.Min(newOffsetMax.x, startOffsetMin.x + maxWidth);

                newOffsetMin.y = Mathf.Min(startOffsetMin.y + delta.y, startOffsetMax.y - minHeight);
                newOffsetMin.y = Mathf.Max(newOffsetMin.y, startOffsetMax.y - maxHeight);
                break;
        }

        targetRect.offsetMin = newOffsetMin;
        targetRect.offsetMax = newOffsetMax;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        if (cursorManager == null) return;
        cursorManager._isHolding = false;
        cursorManager.SetDefault();
    }

    private void OnDisable()
    {
        isDragging = false;
        cursorManager?.SetDefault();
    }
}
