using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 高性能自适应竖直布局类
/// 支持：自动感知子物体增减、感知窗口宽度拖拽拉伸、脏标记批处理
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CustomVerticalLayout : MonoBehaviour
{
    [Header("Vertical layout spacing settings")]
    public float topSpacing = 20f;
    public float bottomSpacing = 40f;
    public float spacing = 10f;

    private RectTransform rectTransform;
    private List<RectTransform> children = new List<RectTransform>();

    // 性能优化核心：脏标记
    private bool isDirty = false;
    
    // 记录容器上一帧的宽度，用于判断是否真的被横向拉伸了
    private float lastContainerWidth = -1f; 

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        lastContainerWidth = rectTransform.rect.width;
    }

    void OnEnable()
    {
        RefreshChildren();
        MarkDirty();
    }

    /// <summary>
    /// 当子物体增减时触发
    /// </summary>
    private void OnTransformChildrenChanged()
    {
        RefreshChildren();
        MarkDirty();
    }

    /// <summary>
    /// 当容器自身尺寸改变时触发（比如玩家拖拽了窗口边缘）
    /// </summary>
    private void OnRectTransformDimensionsChange()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;
        }

        // 核心安全检查：只在“宽度”真正发生变化时才刷新子物体。
        // 这非常重要，因为重新计算高度也会触发这个方法，如果不加判断会导致无限死循环！
        if (!Mathf.Approximately(rectTransform.rect.width, lastContainerWidth))
        {
            lastContainerWidth = rectTransform.rect.width;

            // 遍历通知所有气泡：容器宽度变了，重新算一下你们的换行和尺寸！
            foreach (var child in children)
            {
                if (child == null || !child.gameObject.activeInHierarchy) continue;
                
                var textbox = child.GetComponent<CustomTextBox>();
                if (textbox != null)
                {
                    textbox.RefreshSizeIfNeeded();
                }
            }
            
            // 子物体尺寸变了，标记整体容器需要重新堆叠高度
            MarkDirty(); 
        }
    }

    /// <summary>
    /// 刷新子物体列表，并订阅它们的尺寸变化事件,以便在子物体尺寸变化时能及时标记整体布局为脏，等待下一帧重排。
    /// </summary>
    public void RefreshChildren()
    {
        children.Clear();
        
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i) as RectTransform;
            if (child == null) continue;
            
            children.Add(child);

            var textbox = child.GetComponent<CustomTextBox>();
            if (textbox != null)
            {
                textbox.OnSizeChanged -= MarkDirty;
                textbox.OnSizeChanged += MarkDirty;
            }
        }
    }

    public void MarkDirty()
    {
        isDirty = true;
    }

    private void LateUpdate()
    {
        if (isDirty)
        {
            UpdateLayout();
        }
    }

    /// <summary>
    /// 核心方法：根据当前子物体列表和设置的间距，计算并更新每个子物体的 anchoredPosition 和整个容器的 sizeDelta,实现自动堆叠和适应高度的功能。
    /// </summary>
    public void UpdateLayout()
    {
        float currentY = topSpacing;
        
        foreach (var child in children)
        {
            if (!child.gameObject.activeInHierarchy) continue;

            child.anchoredPosition = new Vector2(child.anchoredPosition.x, -currentY);
            currentY += child.sizeDelta.y + spacing;
        }

        float totalHeight = currentY + bottomSpacing;
        totalHeight = Mathf.Max(0, totalHeight);
        
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, totalHeight);
        
        isDirty = false;
        
        var culler = GetComponent<ChatVisibilityCuller>();
        if (culler != null) culler.Cull();
    }
}