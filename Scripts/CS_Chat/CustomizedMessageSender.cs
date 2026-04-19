using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;


public class CustomizedMessageSender : MonoBehaviour
{
    [Header("TextBox Prefabs")]
    public GameObject AuthorTextBox;
    public GameObject PlayerAuthorBox;
    public GameObject ChooseMessagePrefab;

    [Header("Avatar Prefabs")]
    public GameObject AuthorAvatarPrefab;
    public GameObject PlayerAvatarPrefab;

    [Header("UI References")]
    public ScrollRect scroll;

    [Header("Rhythm Settings")]
    public float messageInterval = 0.8f; // 每句话之间的基础停顿时间
    public float charDelay = 0.05f;      // 根据字数动态延迟的单字时间

    // --- 运行时状态 ---
    private CustomVerticalLayout layout;
    private bool isChoosing = false;
    private MessageRole? lastSpeakerRole = null;

    // --- 高性能 UniTask 队列系统 ---
    private Queue<Func<CancellationToken, UniTask>> messageQueue = new Queue<Func<CancellationToken, UniTask>>();
    private bool isProcessingQueue = false;
    
    // 用于安全地一键取消所有正在进行中的异步等待 (解决重置时的逻辑冲突)
    private CancellationTokenSource resetCts;

    private void Awake()
    {
        layout = GetComponent<CustomVerticalLayout>();
        // 将组件的销毁事件与重置事件绑定成一个联合 Token
        resetCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
    }

    /// <summary>
    /// 外部调用：发送一条普通文本消息
    /// </summary>
    public void CreateMessage(MessageRole role, string content, bool animate = true)
    {
        // 将异步任务封装为委托压入队列
        messageQueue.Enqueue((token) => ExecuteCreateMessage(role, content, animate, token));
        
        if (!isProcessingQueue)
        {
            ProcessQueue(resetCts.Token).Forget();
        }
    }

    /// <summary>
    /// 外部调用：生成多个选项
    /// </summary>
    public void CreateChoices(List<ChoiceData> choices)
    {
        messageQueue.Enqueue((token) => ExecuteCreateChoices(choices, token));
        
        if (!isProcessingQueue)
        {
            ProcessQueue(resetCts.Token).Forget();
        }
    }

    // --- UniTask 队列处理器 ---
    private async UniTaskVoid ProcessQueue(CancellationToken token)
    {
        isProcessingQueue = true;

        try
        {
            while (messageQueue.Count > 0)
            {
                // 取出委托并传入当前的取消令牌开始执行
                var taskFunc = messageQueue.Dequeue();
                
                // 执行具体的生成逻辑，并等待其完成
                await taskFunc(token);
                
                // 如果刚刚执行完的是选项任务，则立刻打断队列自动播放
                if (isChoosing)
                {
                    isProcessingQueue = false;
                    return;
                }

                // 每句话显示完后的自然停顿（使用 TimeSpan 配合 Token 保证安全取消）
                await UniTask.Delay(TimeSpan.FromSeconds(messageInterval), cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            // 当 ResetAll 被调用或物体被销毁时，UniTask 会抛出此异常安全终止协程
            // 这是一个正常的生命周期打断，无需抛出错误
        }
        finally
        {
            // 无论正常结束还是被强行取消，都确保状态安全重置
            if (!isChoosing) isProcessingQueue = false;
        }
    }

    // --- 内部执行逻辑 ---

    /// <summary>
    /// 生成一条文本消息的完整流程：
    /// 1. 根据角色切换头像（如果需要）
    /// 2. 实例化对应的文本框预制体
    /// 3. 强制刷新排版并设置初始状态（缩放为零，透明度为零）
    /// 4. 同步执行显示动画和滚动条（已去掉原先的延迟一帧，改为直接在动画里处理）
    /// 5. 根据文本长度动态等待，给予玩家足够的时间阅读（使用 UniTask.Delay 配合 Token 保证安全取消）
    /// 6. 结束后继续队列中的下一条消息，除非当前是选项任务，此时直接打断队列自动播放，等待玩家选择后再继续（通过 isChoosing 标志控制）
    /// 7. 整个流程中任何时候如果调用了 ResetAll，都能安全地掐断正在等待的 UniTask，防止逻辑冲突和异常崩溃（通过 CancellationTokenSource 实现）
    /// 8. 通过这种结构化的队列系统，确保了消息的有序显示、动画的流畅执行，以及玩家交互的及时响应，同时大大提升了代码的可维护性和扩展性。
    /// </summary>
    /// <param name="role"></param>
    /// <param name="content"></param>
    /// <param name="animate"></param>
    /// <param name="token"></param>
    private async UniTask ExecuteCreateMessage(MessageRole role, string content, bool animate, CancellationToken token)
    {
        // 1. 头像处理
        if (lastSpeakerRole != role)
        {
            GameObject avatarPrefab = (role == MessageRole.Author) ? AuthorAvatarPrefab : PlayerAvatarPrefab;
            if (avatarPrefab != null)
            {
                var avatar = Instantiate(avatarPrefab, transform); 
                var rect = avatar.GetComponent<RectTransform>(); 
                rect.anchoredPosition = new Vector2(role == MessageRole.Author ? 16f : -16f, rect.anchoredPosition.y); // 根据角色调整头像位置（左侧或右侧）
            }
            lastSpeakerRole = role; // 更新最后说话角色的状态
        }

        // 2. 实例化
        GameObject prefab = (role == MessageRole.Author) ? AuthorTextBox : PlayerAuthorBox; // 根据角色选择预制体
        GameObject msgObj = Instantiate(prefab, transform);                                 // 实例化文本框预制体
        msgObj.transform.localPosition = Vector3.zero;                                      // 确保新实例的局部位置为零，避免父物体的排版残留位置影响
        msgObj.GetComponentInChildren<TextMeshProUGUI>().text = content;                    // 设置文本内容

        // 3. 瞬间排版与隐身
        var box = msgObj.GetComponent<CustomTextBox>(); // 获取文本框组件
        box.ForceRefreshSize();                         // 强制刷新尺寸，确保动画从正确的大小开始
        box.RefreshSizeIfNeeded();                      // 再次检查是否需要刷新尺寸（如果上面已经刷新过了，这里会被拦截掉，避免重复计算）
        box.transform.localScale = Vector3.zero;        // 初始缩放为零，准备动画
        
        CanvasGroup cg = box.GetComponent<CanvasGroup>();   // 获取或添加 CanvasGroup 组件，用于控制透明度
        if (cg == null) cg = msgObj.AddComponent<CanvasGroup>(); 
        cg.alpha = 0f;      

        layout.RefreshChildren();   // 刷新布局的子物体列表，确保新实例被包含在内
        layout.UpdateLayout();      // 立即更新布局，确保新实例的尺寸和位置被正确计算并应用（避免动画时的闪烁和错位）

        // 4. 同步执行动画与滚动（已去掉延迟一帧的操作）
        if (animate) 
        {
            box.PlayShowAnimation(); // 播放文本框的显示动画（缩放和淡入）
        }
        else 
        { 
            box.transform.localScale = Vector3.one; 
            cg.alpha = 1f; 
        }
        
        if (scroll != null) scroll.verticalNormalizedPosition = 0;  // 确保每次新消息出现时，滚动条都自动拉到底部，显示最新消息

        // 动态等待：让玩家有时间阅读
        float dynamicWait = content.Length * charDelay; // 根据文本长度计算等待时间，给予玩家足够的时间阅读（可以根据需要调整 charDelay 的值来加快或放慢节奏）
        await UniTask.Delay(TimeSpan.FromSeconds(dynamicWait), cancellationToken: token);// 使用 UniTask.Delay 配合 CancellationToken，确保在 ResetAll 被调用时能安全地打断等待，防止逻辑冲突和异常崩溃
    }

    private async UniTask ExecuteCreateChoices(List<ChoiceData> choices, CancellationToken token)
    {
        foreach (var choice in choices)
        {
            GameObject chooseObj = Instantiate(ChooseMessagePrefab, transform);         // 实例化选项预制体
            chooseObj.transform.localPosition = Vector3.zero;                           // 确保新实例的局部位置为零，避免父物体的排版残留位置影响
            chooseObj.GetComponentInChildren<TextMeshProUGUI>().text = choice.content;  // 设置选项文本内容

            CustomTextBox box = chooseObj.GetComponent<CustomTextBox>();                // 获取文本框组件
            box.ForceRefreshSize();                                                     // 强制刷新尺寸，确保动画从正确的大小开始
            box.RefreshSizeIfNeeded();                                                  // 再次检查是否需要刷新尺寸（如果上面已经刷新过了，这里会被拦截掉，避免重复计算）
            box.transform.localScale = Vector3.zero;                                    // 初始缩放为零，准备动画
            
            CanvasGroup cg = box.GetComponent<CanvasGroup>();
            if (cg == null) cg = chooseObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;                                                              // 初始透明度为零，准备动画
            
            var panel = chooseObj.transform.Find("Panel");                      // 查找选项预制体中的 Panel 子物体，用于设置背景颜色
            if (panel) panel.GetComponent<Image>().color = choice.isPositiveStyle ? 
                new Color(0, 1, 0, 0.39f) : new Color(1, 0, 0, 0.39f); // 根据选项的积极/消极属性设置背景颜色（绿色或红色，带有一定透明度）

            var btn = chooseObj.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            // 点击时启动异步回调逻辑，并传入 .Forget() 表明这火即弃，不需要等待其完成
            btn.onClick.AddListener(() => DelayedChoiceProcess(choice, resetCts.Token).Forget());

            box.PlayShowAnimation();
        }

        layout.RefreshChildren();
        layout.UpdateLayout();
        if (scroll != null) scroll.verticalNormalizedPosition = 0;
        
        isChoosing = true;
        
        // 交出控制权一帧，确保 UI 布局渲染完成
        await UniTask.NextFrame(token); 
    }

    private async UniTaskVoid DelayedChoiceProcess(ChoiceData selectedChoice, CancellationToken token)
    {
        try
        {
            DestroyAllChooseMessages();
            isChoosing = false;
            
            // 等待下一帧，让 Destroy() 在引擎层彻底生效清理掉废弃的 UI 物体
            await UniTask.NextFrame(token); 

            // 玩家选择后，将选择的内容作为文字气泡发送
            CreateMessage(MessageRole.Player, selectedChoice.content);

            // 触发你外部编写的后续剧情逻辑
            selectedChoice.onSelected?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // 如果在点击选项的这一帧刚好调用了 ResetAll，安全吞掉异常结束逻辑
        }
    }

    private void DestroyAllChooseMessages()
    {
        foreach (var btn in GetComponentsInChildren<Button>()) 
        {
            Destroy(btn.gameObject);
        }
    }

    /// <summary>
    /// 一键清屏与逻辑阻断
    /// </summary>
    public void ResetAll()
    {
        // 核心安全机制：强行掐断所有正在等待的 UniTask 延迟操作
        resetCts?.Cancel();
        resetCts?.Dispose();
        
        // 重新生成一个新的生命周期令牌
        resetCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        messageQueue.Clear();
        isProcessingQueue = false;
        isChoosing = false;
        lastSpeakerRole = null;
        
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            child.SetParent(null); // 解除父子关系防止排版残留空隙
            Destroy(child.gameObject);
        }
    }
}