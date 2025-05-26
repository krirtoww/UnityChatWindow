using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class CustomCursorManager : MonoBehaviour
{
    [Header("图标资源")]
    public Sprite defaultCursor;
    public Sprite dragCursor;
    public Sprite scaleHorizontal;
    public Sprite scaleVertical;
    public Sprite scaleDiagonal1; // ↘️ ↖️
    public Sprite scaleDiagonal2; // ↗️ ↙️

    [Header("缩放设置")]
    public float baseScreenHeight = 1080f;
    public float baseCursorSize = 32f;

    public bool _isHolding = false;

    private RectTransform cursorUI;
    private Image cursorImage;
    private Canvas canvas;

    private void Awake()
    {
        cursorUI = GetComponent<RectTransform>();
        cursorImage = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if(Cursor.visible) Cursor.visible = false;
        UpdateCursorPosition();
        UpdateCursorScale();
    }

    private void UpdateCursorPosition()
    {
        if (canvas == null) return;

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out localPos
        );
        cursorUI.localPosition = localPos;
    }

    private void UpdateCursorScale()
    {
        float scaleFactor = Screen.height / baseScreenHeight;
        cursorUI.sizeDelta = Vector2.one * baseCursorSize * scaleFactor;
    }

    public void SetDefault() => SetCursor(defaultCursor);
    public void SetDrag() => SetCursor(dragCursor);

    public void SetScaleCursor(DragScaleHandler.ScaleDirection direction)
    {
        switch (direction)
        {
            case DragScaleHandler.ScaleDirection.Left:
            case DragScaleHandler.ScaleDirection.Right:
                SetCursor(scaleHorizontal);
                break;
            case DragScaleHandler.ScaleDirection.Top:
            case DragScaleHandler.ScaleDirection.Bottom:
                SetCursor(scaleVertical);
                break;
            case DragScaleHandler.ScaleDirection.TopLeft:
            case DragScaleHandler.ScaleDirection.BottomRight:
                SetCursor(scaleDiagonal1);
                break;
            case DragScaleHandler.ScaleDirection.TopRight:
            case DragScaleHandler.ScaleDirection.BottomLeft:
                SetCursor(scaleDiagonal2);
                break;
        }
    }

    private void SetCursor(Sprite sprite)
    {
        if (_isHolding) return;
        if (cursorImage != null)
        {
            cursorImage.sprite = sprite;
            cursorImage.enabled = sprite != null;

            if (sprite != null && cursorUI != null)
            {
                Vector2 spritePivot = sprite.pivot;
                Vector2 spriteSize = sprite.rect.size;

                cursorUI.pivot = new Vector2(
                    spritePivot.x / spriteSize.x,
                    spritePivot.y / spriteSize.y
                );
                cursorUI.localPosition = Vector3.zero;
            }
        }
    }
}
