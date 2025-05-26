using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
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


    private RectTransform rectTransform;
    private TextMeshProUGUI tmpText;
    private RectTransform parentRect;

    private float lastParentWidth = -1f;
    private float lastWidth = -1f;

    public System.Action OnSizeChanged;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if(parentRect == null && transform.parent != null) parentRect = transform.parent.GetComponent<RectTransform>();
        RefreshSizeIfNeeded();
    }
    public void RefreshSizeIfNeeded()
    {
        if(parentRect == null) return;
        float preferredHeight = GetAccurateTextHeight(tmpText);
        if (!Mathf.Approximately(preferredHeight, rectTransform.sizeDelta.y))
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, preferredHeight);
            OnSizeChanged?.Invoke();
            return;
        }
        float parentWidth = parentRect.rect.width;
        if (Mathf.Approximately(parentWidth, lastParentWidth)) return; 
        lastParentWidth = parentWidth;

        bool isLeftAligned = rectTransform.pivot.x == 0f;
        float totalAvailableWidth = parentWidth - frontSpace - backSpace;
        float clampedWidth = Mathf.Clamp(totalAvailableWidth, minWidth, maxWidth);

        if (!Mathf.Approximately(clampedWidth, lastWidth))
        {
            rectTransform.sizeDelta = new Vector2(clampedWidth, preferredHeight);
            Vector2 anchoredPos = rectTransform.anchoredPosition;
            anchoredPos.x = isLeftAligned ? frontSpace : -backSpace;
            rectTransform.anchoredPosition = anchoredPos;
            OnSizeChanged?.Invoke();
            lastWidth = clampedWidth;
        }
    }

    public void ForceRefreshSize()
    {
        lastParentWidth = -1f;
        lastWidth = -1f;
        return;
    }

    public void PlayShowAnimation()
    {
        CanvasGroup canvasGroup = GetOrAddCanvasGroup();
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.zero;

        DOTween.Kill(gameObject); // 防止重复动画
        Sequence anim = DOTween.Sequence();
        anim.Append(canvasGroup.DOFade(1f, 0.3f));
        anim.Join(rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
    }

    private CanvasGroup GetOrAddCanvasGroup()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    private float GetAccurateTextHeight(TextMeshProUGUI tmp)
    {
        tmp.ForceMeshUpdate(); // 确保 textInfo 更新

        int lineCount = tmp.textInfo.lineCount;

        if (lineCount == 0)
            return 0;

        float lineHeight = tmp.font.faceInfo.lineHeight * tmp.fontSize / tmp.font.faceInfo.pointSize;
        float totalPadding = tmp.margin.y + tmp.margin.x;
        float totalHeight = lineHeight * lineCount + totalPadding;


        return totalHeight;
    }

}
