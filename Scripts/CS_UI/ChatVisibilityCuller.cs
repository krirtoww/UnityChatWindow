using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 聊天列表高性能剔除器
/// 作用：将滚动出屏幕外看不见的历史消息从 GPU 渲染管线中剔除，但保留其物理排版位置
/// </summary>
public class ChatVisibilityCuller : MonoBehaviour
{
    [Header("UI references")]
    public ScrollRect scrollRect;
    
    [Header("Exclude parameters")]
    [Tooltip("视口上下多渲染的缓冲距离(像素)，防止滑动过快时出现边缘闪烁")]
    public float bufferZone = 300f; 

    private RectTransform viewportRect;
    private RectTransform contentRect;

    private void Start()
    {
        if (scrollRect == null) scrollRect = GetComponentInParent<ScrollRect>();
        
        viewportRect = scrollRect.viewport;
        contentRect = scrollRect.content;

        // 监听玩家滑动列表的事件
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    private void OnScroll(Vector2 normalizedPos)
    {
        Cull();
    }

    /// <summary>
    /// 核心剔除逻辑
    /// </summary>
    public void Cull()
    {
        if (contentRect == null || viewportRect == null) return;

        // 计算当前玩家视口在 Content 局部坐标系下的可见高度范围
        // contentRect.anchoredPosition.y 表示内容向上滚动了多少像素
        float visibleTop = contentRect.anchoredPosition.y - bufferZone;
        float visibleBottom = contentRect.anchoredPosition.y + viewportRect.rect.height + bufferZone;

        // 遍历所有气泡
        for (int i = 0; i < contentRect.childCount; i++)
        {
            RectTransform child = contentRect.GetChild(i) as RectTransform;
            if (!child.gameObject.activeInHierarchy) continue;

            // 我们的 CustomVerticalLayout 是往下排版的，y是负数，所以取绝对值来比较深度
            float childTop = Mathf.Abs(child.anchoredPosition.y);
            float childBottom = childTop + child.sizeDelta.y;

            // 判断这个气泡是否在我们的可见缓冲范围内
            bool isVisible = (childBottom >= visibleTop) && (childTop <= visibleBottom);

            // 【终极性能优化】：遍历气泡下的所有渲染器（文字、背景图片）
            CanvasRenderer[] renderers = child.GetComponentsInChildren<CanvasRenderer>();
            foreach (var renderer in renderers)
            {
                // cull = true 表示直接从渲染管线中剔除！它不再消耗任何 GPU 绘制和 Canvas 合批性能
                // 但它的 GameObject 依旧活跃，RectTransform 大小不变，完美维持了界面的排版！
                if (renderer.cull != !isVisible) 
                {
                    renderer.cull = !isVisible;
                }
            }
        }
    }
}