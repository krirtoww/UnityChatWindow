using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;

public class PerformanceTestController : MonoBehaviour
{
    [Header("UI 引用")]
    public CustomizedMessageSender chatUI;

    [Header("测试参数")]
    public int messageCount = 200;
    [Tooltip("模拟玩家阅读选项的停顿时间")]
    public float autoClickDelay = 0.2f;

    [Header("实时性能监控 (只读)")]
    public float currentFPS;
    public float minFPS = 999f;
    public long allocatedMemoryMB;
    public int uiNodeCount;

    // 内部状态
    private int frames;
    private float timeAccumulator;
    private CancellationTokenSource cts;

    // 各种长度和复杂度的语料库，用于测试排版自适应的极限
    private readonly string[] randomSentences = new string[] 
    {
        "这是一条普通的短消息。",
        "性能测试正在进行中，请观察左上角的帧率变化。",
        "【极限排版测试】这是一条特别长、特别长、包含很多字符的测试消息。用来检测自动换行、网格重建（ForceMeshUpdate）以及高度自适应算法是否会在长文本时引发明显的 GC 掉帧或者由于浮点数误差导致文字穿模漏出气泡外！",
        "UniTask 异步队列运行正常，脏标记批处理已启动。",
        "Warning: 正在模拟极端长对话环境...",
        "I/O 模拟: 获取到新的视觉特征，准备生成选项分支。",
        "短。"
    };

    private void Start()
    {
        cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        
        // 1. 清空画布
        chatUI.ResetAll();

        // 2. 启动性能监控面板
        StartFPSMonitor().Forget();

        // 3. 瞬间下发 200 条指令进队列
        GenerateStressTestData();

        // 4. 启动“自动代打”外挂，监控并自动点击弹出的选项
        AutoClickMonitor().Forget();
    }

    private void GenerateStressTestData()
    {
        Debug.Log($"[StressTest] 开始向队列注入 {messageCount} 条对话指令...");

        for (int i = 0; i < messageCount; i++)
        {
            int rand = UnityEngine.Random.Range(0, 10);

            if (rand < 2) // 20% 概率生成选项分支
            {
                var choices = new List<ChoiceData>
                {
                    new ChoiceData 
                    { 
                        content = $"自动选项 A (测试节点 {i})", 
                        isPositiveStyle = true, 
                        onSelected = () => { /* 这里可以写回调，但压测时为空即可 */ } 
                    },
                    new ChoiceData 
                    { 
                        content = $"自动选项 B (测试节点 {i})", 
                        isPositiveStyle = false, 
                        onSelected = () => { } 
                    }
                };
                chatUI.CreateChoices(choices);
            }
            else if (rand < 6) // 40% 概率 NPC 发言
            {
                string text = randomSentences[UnityEngine.Random.Range(0, randomSentences.Length)];
                chatUI.CreateMessage(MessageRole.Author, $"[NPC {i}] " + text);
            }
            else // 40% 概率 玩家发言
            {
                string text = randomSentences[UnityEngine.Random.Range(0, randomSentences.Length)];
                chatUI.CreateMessage(MessageRole.Player, $"[Player {i}] " + text);
            }
        }
        
        Debug.Log("[StressTest] 注入完毕！等待 UI 队列消费执行...");
    }

    /// <summary>
    /// 自动外挂：在后台死循环寻找按钮并自动点击
    /// </summary>
    private async UniTaskVoid AutoClickMonitor()
    {
        while (!cts.IsCancellationRequested)
        {
            await UniTask.Yield(); // 每帧轮询一次

            // 寻找当前 UI 树下所有的按钮
            var buttons = chatUI.GetComponentsInChildren<Button>();
            
            if (buttons.Length > 0 && buttons[0].interactable)
            {
                // 找到按钮了！等一小会儿模拟人眼阅读
                await UniTask.Delay(TimeSpan.FromSeconds(autoClickDelay), cancellationToken: cts.Token);

                // 确保按钮在等待期间没被销毁
                if (buttons[0] != null)
                {
                    // 模拟物理点击，触发 MessageSender 里的回调
                    buttons[0].onClick.Invoke();
                    
                    // 【关键防连点】：等待直到 UI 把选项按钮全部销毁，再进行下一次扫描
                    await UniTask.WaitUntil(() => chatUI.GetComponentsInChildren<Button>().Length == 0, cancellationToken: cts.Token);
                }
            }
        }
    }

    /// <summary>
    /// 后台精准计算 FPS 和 内存
    /// </summary>
    private async UniTaskVoid StartFPSMonitor()
    {
        while (!cts.IsCancellationRequested)
        {
            frames++;
            timeAccumulator += Time.unscaledDeltaTime;

            if (timeAccumulator >= 1f) // 每秒更新一次数据
            {
                currentFPS = frames / timeAccumulator;
                
                // 忽略刚启动前 2 秒的加载掉帧，记录之后的最低帧
                if (Time.time > 2f && currentFPS < minFPS) minFPS = currentFPS; 
                
                frames = 0;
                timeAccumulator -= 1f;

                // 获取托管堆的内存分配情况
                allocatedMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
                
                // 获取当前 UI 下挂载了多少个气泡节点
                uiNodeCount = chatUI.transform.childCount;
            }

            await UniTask.Yield();
        }
    }

    /// <summary>
    /// 将性能数据打印到屏幕左上角，脱离 Console 也能看
    /// </summary>
    private void OnGUI()
    {
        int yOffset = 20;
        int fontSize = 24;

        GUIStyle style = new GUIStyle();
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;

        // FPS 显示 (低于 50 帧变红)
        style.normal.textColor = currentFPS > 50 ? Color.green : (currentFPS > 30 ? Color.yellow : Color.red);
        GUI.Label(new Rect(20, yOffset, 400, 50), $"FPS: {currentFPS:F1} (Min: {minFPS:F1})", style);
        
        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(20, yOffset += 30, 400, 50), $"GC Memory: {allocatedMemoryMB} MB", style);
        
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(20, yOffset += 30, 400, 50), $"Active UI Nodes: {uiNodeCount} / {messageCount}", style);
    }
}