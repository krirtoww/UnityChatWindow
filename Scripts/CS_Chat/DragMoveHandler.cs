using UnityEngine.EventSystems;
using UnityEngine;

public class DragMoveHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("需要拖动的目标")]
    public RectTransform targetToMove;

    [Header("自定义鼠标控制器")]
    public CustomCursorManager cursorManager;

    [Header("安全设置")]
    [Tooltip("开启后，窗口无法被拖出父级容器(通常是屏幕)的范围")]
    public bool clampToScreen = true; 

    private Vector2 offset;
    private RectTransform parentRect;

    private void Start()
    {
        if (targetToMove != null)
        {
            parentRect = targetToMove.parent as RectTransform;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetToMove == null || parentRect == null) return;

        // 点击时自动置顶窗口
        targetToMove.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        // 【关键修复 1】：使用 localPosition 替代 anchoredPosition，计算真实的物理偏移量
        offset = (Vector2)targetToMove.localPosition - localPoint;

        if (cursorManager != null)
        {
            cursorManager._isHolding = true;
            cursorManager.SetDrag();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetToMove == null || parentRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            Vector2 targetLocalPosition = localPoint + offset;

            // 限制在屏幕范围内
            if (clampToScreen)
            {
                targetLocalPosition = ClampToParentBounds(targetLocalPosition);
            }

            // 【关键修复 2】：将计算好的安全坐标赋值给 localPosition，彻底避开锚点错位问题
            targetToMove.localPosition = targetLocalPosition;
        }
    }

    private Vector2 ClampToParentBounds(Vector2 targetLocalPos)
    {
        Vector3[] targetCorners = new Vector3[4];
        targetToMove.GetLocalCorners(targetCorners);
        
        float targetMinX = targetCorners[0].x;
        float targetMaxX = targetCorners[2].x;
        float targetMinY = targetCorners[0].y;
        float targetMaxY = targetCorners[2].y;

        Rect pRect = parentRect.rect;
        float safeMinX = pRect.xMin - targetMinX;
        float safeMaxX = pRect.xMax - targetMaxX;
        float safeMinY = pRect.yMin - targetMinY;
        float safeMaxY = pRect.yMax - targetMaxY;

        targetLocalPos.x = Mathf.Clamp(targetLocalPos.x, safeMinX, safeMaxX);
        targetLocalPos.y = Mathf.Clamp(targetLocalPos.y, safeMinY, safeMaxY);

        return targetLocalPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (cursorManager != null)
        {
            cursorManager._isHolding = false;
            cursorManager.SetDefault();
        }
    }

    private void OnDisable()
    {
        if (cursorManager != null)
        {
            cursorManager._isHolding = false;
            cursorManager.SetDefault();
        }
    }
}