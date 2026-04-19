using System;

/// <summary>
/// 角色枚举
/// </summary>
public enum MessageRole
{
    Author, // NPC/AI老师
    Player  // 玩家
}

/// <summary>
/// 动态选项数据结构
/// </summary>
public class ChoiceData
{
    public string content;           // 选项文本
    public bool isPositiveStyle;     // 是否为积极/正确选项（决定背景颜色）
    public Action onSelected;        // 选中后的回调逻辑
}