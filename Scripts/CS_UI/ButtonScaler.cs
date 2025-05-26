using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Scale Settings")]
    public float targetScale = 1.2f;
    public float duration = 0.3f;

    private Vector3 originalScale;
    private Tween currentTween;
    private bool hasInitialized = false;

    private void EnsureOriginalScale()
    {
        if (!hasInitialized)
        {
            originalScale = transform.localScale;
            hasInitialized = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnsureOriginalScale();
        ScaleTo(targetScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EnsureOriginalScale();
        ScaleTo(originalScale.x);
    }

    public void OnSelect(BaseEventData eventData)
    {
        EnsureOriginalScale();
        ScaleTo(targetScale);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        EnsureOriginalScale();
        ScaleTo(originalScale.x);
    }

    private void ScaleTo(float scale)
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        currentTween = transform.DOScale(Vector3.one * scale, duration).SetEase(Ease.OutBack);
    }

    private void OnDestroy()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
    }
}
