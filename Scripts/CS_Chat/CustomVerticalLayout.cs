using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CustomVerticalLayout : MonoBehaviour
{
    public float topSpacing = 20f;
    public float bottomSpacing = 40f;
    public float spacing = 10f;

    private RectTransform rectTransform;
    private List<RectTransform> children = new List<RectTransform>();

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        RefreshChildren();
    }


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
                textbox.OnSizeChanged -= UpdateLayout;
                textbox.OnSizeChanged += UpdateLayout;
            }
        }
    }

    public void RefreshAllTextBoxWidths()
    {
        foreach (var child in children)
        {
            var textbox = child.GetComponent<CustomTextBox>();
            if (textbox != null)
            {
                textbox.RefreshSizeIfNeeded(); 
            }
        }
    }

    public void UpdateLayout()
    {
        float currentY = topSpacing;
        foreach (var child in children)
        {
            child.anchoredPosition = new Vector2(child.anchoredPosition.x, -currentY);
            currentY += child.sizeDelta.y + spacing;
        }

        float totalHeight = currentY + bottomSpacing;
        totalHeight = Mathf.Max(0, totalHeight);
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, totalHeight);
    }

}
