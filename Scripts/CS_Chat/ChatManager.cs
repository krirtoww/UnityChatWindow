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

    [Header("动画设置")]
    public float animationDuration = 0.4f;

    [Header("警告闪烁设置")]
    public bool isWarning = false;
    public Color warningColor = Color.red;
    public Color defaultColor = Color.white;
    public float flashInterval = 0.5f;

    private Tween warningTween;

    private Vector2 savedOpenedPos;
    private Vector3 savedOpenedScale;
    private Vector2 closedSize;

    private RectTransform canvasRect;

    private void Start()
    {
        canvasRect = openedChat.GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        closedSize = closedChat.sizeDelta;
        savedOpenedPos = openedChat.anchoredPosition;
        savedOpenedScale = openedChat.localScale;
        openedChatGroup.alpha = 0f;
        openedChat.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isWarning)
            StartWarning();
        else
            StopWarning();
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
        // 获取 closedChat 的世界中心点
        Vector3 worldPos = closedChat.TransformPoint(closedChat.rect.center);

        // 将世界点转换为 openedChat 所在父级的本地坐标（通常是 Canvas）
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, worldPos, null, out localPoint);

        // 启用并设置 openedChat 起始状态
        openedChat.gameObject.SetActive(true);
        openedChat.anchoredPosition = localPoint;

        // 缩放比例
        Vector2 openedSize = openedChat.sizeDelta;
        float scaleX = closedSize.x / openedSize.x;
        float scaleY = closedSize.y / openedSize.y;
        openedChat.localScale = new Vector3(scaleX, scaleY, 1f);
        openedChatGroup.alpha = 0f;

        // 播放展开动画
        openedChat.DOAnchorPos(savedOpenedPos, animationDuration);
        openedChat.DOScale(Vector3.one, animationDuration);
        openedChatGroup.DOFade(1f, animationDuration);

        closedChat.DOScale(Vector3.zero, animationDuration).OnComplete(() =>
        {
            closedChat.gameObject.SetActive(false);
            closedChat.localScale = Vector3.one;
        });

    }

    public void Collapse()
    {
        // 记录 openedChat 当前锚点位置
        savedOpenedPos = openedChat.anchoredPosition;
        savedOpenedScale = openedChat.localScale;

        // 获取 closedChat 的世界中心点
        Vector3 worldPos = closedChat.TransformPoint(closedChat.rect.center);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, worldPos, null, out localPoint);

        // 缩放比例
        Vector2 openedSize = openedChat.sizeDelta;
        float scaleX = closedSize.x / openedSize.x;
        float scaleY = closedSize.y / openedSize.y;

        // 播放收起动画
        openedChat.DOAnchorPos(localPoint, animationDuration);
        openedChat.DOScale(new Vector3(scaleX, scaleY, 1f), animationDuration);
        openedChatGroup.DOFade(0f, animationDuration).OnComplete(() =>
        {
            openedChat.gameObject.SetActive(false);
        });

        // 显示并动画恢复 closedChat
        closedChat.gameObject.SetActive(true);
        closedChat.localScale = Vector3.zero;
        closedChat.DOScale(Vector3.one, animationDuration);
    }

    void StartWarning()
    {
        if (warningTween != null || !closedChat.gameObject.activeInHierarchy)
            return;

        warningTween = closedChatImage.DOColor(warningColor, flashInterval)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.Linear);
    }

    void StopWarning()
    {
        if (warningTween != null)
        {
            warningTween.Kill();
            warningTween = null;
            closedChatImage.color = defaultColor;
        }
    }
}
