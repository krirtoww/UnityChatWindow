using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ChatWindowManager : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform closedChat;         // 图标 RectTransform（右上角 anchor）
    public RectTransform openedChat;         // 展开窗口 RectTransform（中心 anchor）
    public CanvasGroup openedChatGroup;      // 用于渐变透明
    public Image closedChatImage;            // 图标 Image 组件
    public ScrollRect chatScroll;            // 滚动组件，展开时自动滚动到底部
    
    // 新增：需要引用布局管理器来修复Bug
    // 你可以在Inspector拖拽赋值，不拖拽代码也会自动获取
    public CustomVerticalLayout chatLayout;  

    [Header("动画设置")]
    public float animationDuration = 0.4f;

    [Header("警告闪烁设置")]
    public Color warningColor = Color.red;
    public Color defaultColor = Color.white;
    public float flashInterval = 0.5f;

    private Tween warningTween;
    private Vector2 savedOpenedPos;
    private Vector2 closedSize;
    private RectTransform canvasRect;

    // 性能优化：使用属性(Property)代替 Update() 里的每帧轮询
    private bool _isWarning = false;
    public bool IsWarning 
    {
        get => _isWarning;
        set 
        {
            if (_isWarning == value) return;
            _isWarning = value;
            if (_isWarning) StartWarning();
            else StopWarning();
        }
    }

    private void Start()
    {
        canvasRect = openedChat.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        
        // 自动获取布局组件
        if (chatLayout == null) chatLayout = openedChat.GetComponentInChildren<CustomVerticalLayout>();

        closedSize = closedChat.sizeDelta;
        savedOpenedPos = openedChat.anchoredPosition;
        openedChatGroup.alpha = 0f;
        openedChat.gameObject.SetActive(false);
    }

    public void OnClickClosedChat()
    {
        Expand();
    }

    public void OnClickClose()
    {
        Collapse();
    }

    public void Expand()
    {
        Vector3 worldPos = closedChat.TransformPoint(closedChat.rect.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, worldPos, null, out Vector2 localPoint);

        // 1. 立即激活窗口
        openedChat.gameObject.SetActive(true);
        
        if (chatLayout != null)
        {
            // 遍历并强制重算每个气泡
            foreach (Transform child in chatLayout.transform)
            {
                var box = child.GetComponent<CustomTextBox>();
                if (box != null) box.ForceRefreshSize();
            }
            
            // 强制布局管理器立刻排版，拒绝等待到帧末尾造成的闪烁
            chatLayout.RefreshChildren();
            chatLayout.UpdateLayout(); 
            
            // 排版重算完毕后，强制将滑动条瞬间拉到底部
            if (chatLayout != null)
            {
                foreach (Transform child in chatLayout.transform)
                {
                    var box = child.GetComponent<CustomTextBox>();
                    if (box != null) box.ForceRefreshSize();
                }
                chatLayout.RefreshChildren();
                chatLayout.UpdateLayout(); 
    
                // 排版重算完毕后，强制将滑动条瞬间拉到底部
                if (chatScroll != null)
                {
                    // 强制画布立刻更新，防止归零时高度还没应用
                    Canvas.ForceUpdateCanvases(); 
                    chatScroll.verticalNormalizedPosition = 0f;
                }
            }
        }

        // 2. 准备动画的起始状态
        openedChat.anchoredPosition = localPoint;
        Vector2 openedSize = openedChat.sizeDelta;
        float scaleX = closedSize.x / openedSize.x;
        float scaleY = closedSize.y / openedSize.y;
        openedChat.localScale = new Vector3(scaleX, scaleY, 1f);
        openedChatGroup.alpha = 0f;

        // 3. 播放展开动画 (加入了 SetEase 让弹出更有弹性)
        openedChat.DOAnchorPos(savedOpenedPos, animationDuration).SetEase(Ease.OutCubic);
        openedChat.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        openedChatGroup.DOFade(1f, animationDuration);

        closedChat.DOScale(Vector3.zero, animationDuration).OnComplete(() =>
        {
            closedChat.gameObject.SetActive(false);
            closedChat.localScale = Vector3.one;
        });
    }

    public void Collapse()
    {
        savedOpenedPos = openedChat.anchoredPosition;

        Vector3 worldPos = closedChat.TransformPoint(closedChat.rect.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, worldPos, null, out Vector2 localPoint);

        Vector2 openedSize = openedChat.sizeDelta;
        float scaleX = closedSize.x / openedSize.x;
        float scaleY = closedSize.y / openedSize.y;

        // 播放收起动画 (加入了 SetEase 优化手感)
        openedChat.DOAnchorPos(localPoint, animationDuration).SetEase(Ease.InCubic);
        openedChat.DOScale(new Vector3(scaleX, scaleY, 1f), animationDuration).SetEase(Ease.InBack);
        openedChatGroup.DOFade(0f, animationDuration).OnComplete(() =>
        {
            openedChat.gameObject.SetActive(false);
        });

        closedChat.gameObject.SetActive(true);
        closedChat.localScale = Vector3.zero;
        closedChat.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
    }

    public void StartWarning()
    {
        if (warningTween != null || !closedChat.gameObject.activeInHierarchy)
            return;

        warningTween = closedChatImage.DOColor(warningColor, flashInterval)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.Linear);
    }

    public void StopWarning()
    {
        if (warningTween != null)
        {
            warningTween.Kill();
            warningTween = null;
            closedChatImage.color = defaultColor;
        }
    }
    
    private void OnDestroy()
    {
        // 切换场景或销毁时，清理所有的 Tween
        warningTween?.Kill();
        openedChat?.DOKill();
        openedChatGroup?.DOKill();
        closedChat?.DOKill();
    }
}