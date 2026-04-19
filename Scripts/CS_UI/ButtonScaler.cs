using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Scale Settings")]
    public float baseScale = 1.0f;     // 明确写死基础大小
    public float targetScale = 1.2f;
    public float duration = 0.3f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ScaleTo(targetScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ScaleTo(baseScale);
    }

    public void OnSelect(BaseEventData eventData)
    {
        ScaleTo(targetScale);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ScaleTo(baseScale);
    }

    private void ScaleTo(float scale)
    {
        // 直接杀死这个 transform 身上的所有缩放动画！
        // 这样如果鼠标在出生动画（CustomTextBox）期间移入，会立刻打断出生动画，防止两个 Tween 打架
        transform.DOKill(); 

        // 统一使用 transform.DOScale，不再依赖容易丢失引用的 currentTween
        transform.DOScale(Vector3.one * scale, duration).SetEase(Ease.OutBack);
    }

    private void OnDisable()
    {
        // 物体隐藏或销毁时，立刻停止动画并恢复原状
        transform.DOKill();
        transform.localScale = Vector3.one * baseScale;
    }
}