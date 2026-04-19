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

    [Header("缩放配置")]
    public ScaleDirection direction;
    public RectTransform targetRect;
    public CustomCursorManager cursorManager;

    [Header("尺寸限制")]
    public float minWidth = 240f;
    public float minHeight = 300f;
    public float maxWidth = 1200f;
    public float maxHeight = 700f;

    // --- 内部状态 ---
    private RectTransform parentRect;
    private Vector2 startMouseLocalToParent;
    private Vector2 startOffsetMin;
    private Vector2 startOffsetMax;
    private bool isDragging = false;

    private void Start()
    {
        // 自动缓存父级容器，无需在面板上手动拖拽 Canvas
        if (targetRect != null)
        {
            parentRect = targetRect.parent as RectTransform;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        cursorManager?.SetScaleCursor(direction);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 只有在没按住拖拽时，离开边缘才恢复默认鼠标
        if (!isDragging)
        {
            cursorManager?.SetDefault();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetRect == null || parentRect == null) return;

        isDragging = true;
        
        // UX细节：点击边缘准备缩放时，窗口立刻置于最前层！
        targetRect.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out startMouseLocalToParent);

        startOffsetMin = targetRect.offsetMin;
        startOffsetMax = targetRect.offsetMax;

        if (cursorManager != null)
        {
            cursorManager._isHolding = true;
            cursorManager.SetScaleCursor(direction);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || targetRect == null || parentRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 currentMouseLocalToParent);

        Vector2 delta = currentMouseLocalToParent - startMouseLocalToParent;

        Vector2 newOffsetMin = startOffsetMin;
        Vector2 newOffsetMax = startOffsetMax;

        // 核心缩放算法：通过限制 Offset 的差值，完美锁定最大/最小尺寸，且不受 Pivot 影响
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
        if (cursorManager != null)
        {
            cursorManager._isHolding = false;
            // 确保松手时，如果在窗口范围内，恢复普通指针
            cursorManager.SetDefault();
        }
    }

    private void OnDisable()
    {
        isDragging = false;
        if (cursorManager != null)
        {
            cursorManager._isHolding = false;
            cursorManager.SetDefault();
        }
    }
}