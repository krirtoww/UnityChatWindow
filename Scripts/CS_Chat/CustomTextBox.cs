using UnityEngine;
using TMPro;
using DG.Tweening;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(TextMeshProUGUI))]
public class CustomTextBox : MonoBehaviour
{
    [Header("Spacing")]
    public float frontSpace = 40f;
    public float backSpace = 20f;

    [Header("Width Constraints")]
    public float minWidth = 160f;
    public float maxWidth = 400f;

    // ========== 缓存组件 ==========
    private RectTransform rectTransform;
    private TextMeshProUGUI tmpText;
    private RectTransform parentRect;
    private CanvasGroup canvasGroup;

    // ========== 状态记录 ==========
    private float lastParentWidth = -1f;
    private string lastText = null;

    public System.Action OnSizeChanged;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        tmpText = GetComponent<TextMeshProUGUI>();
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (gameObject.activeInHierarchy)
        {
            RefreshSizeIfNeeded();
        }
    }

    public void RefreshSizeIfNeeded()
    {
        if (parentRect == null && transform.parent != null)
            parentRect = transform.parent.GetComponent<RectTransform>();

        if (parentRect == null || tmpText == null) return;

        float parentWidth = parentRect.rect.width;
        string currentText = tmpText.text;

        // 性能拦截：宽度没变且文字没变，直接跳过
        if (Mathf.Approximately(parentWidth, lastParentWidth) && currentText == lastText && lastParentWidth != -1f)
        {
            return;
        }

        // 1. 计算文本框的目标宽度
        bool isLeftAligned = rectTransform.pivot.x == 0f;
        float totalAvailableWidth = parentWidth - frontSpace - backSpace;
        float targetWidth = Mathf.Clamp(totalAvailableWidth, minWidth, maxWidth);
        
        // Step A: 先赋予真实的物理宽度（为了抹平浮点数误差，稍微缩小一点点可渲染区域的判定边界）
        rectTransform.sizeDelta = new Vector2(targetWidth, rectTransform.sizeDelta.y);
        
        // Step B: 强制 TMP 根据刚才设置的真实物理宽度，立刻在后台重构文字网格
        // (不用担心性能，因为这段代码被上面的拦截器保护着，只在尺寸变化的一瞬间触发)
        tmpText.ForceMeshUpdate();

        // Step C: 直接读取 TMP 排版后的绝对精准高度（这个高度内置了你在 TMP 里设置的 Margin 上下边距）
        float targetHeight = tmpText.preferredHeight;
        
        // ==========================================

        // 3. 应用最终正确的尺寸与位置
        rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
        
        Vector2 anchoredPos = rectTransform.anchoredPosition;
        anchoredPos.x = isLeftAligned ? frontSpace : -backSpace;
        rectTransform.anchoredPosition = anchoredPos;

        // 4. 更新状态与回调
        lastParentWidth = parentWidth;
        lastText = currentText;
        OnSizeChanged?.Invoke();
    }

    public void ForceRefreshSize()
    {
        lastParentWidth = -1f;
        lastText = null;
        RefreshSizeIfNeeded(); 
    }

    public void PlayShowAnimation()
    {
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.zero;

        DOTween.Kill(gameObject); 
        Sequence anim = DOTween.Sequence();
        anim.Append(canvasGroup.DOFade(1f, 0.3f));
        anim.Join(rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
    }
    
    private void OnDestroy()
    {
        // 当气泡被 ResetAll 销毁时，确保掐断它身上的所有动画
        DOTween.Kill(gameObject);
    }
}