using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

[System.Serializable]
public class MessageData
{
    public string senderName;
    public List<string> generatedKeys = new List<string>();
}

[System.Serializable]
public class MessageSaveData
{
    public List<MessageData> allSenders = new List<MessageData>();
}

public class MessageSender : MonoBehaviour
{
    public string senderName;

    [Header("TextBox Prefabs")] 
    public GameObject AuthorTextBox;
    public GameObject PlayerAuthorBox;
    public GameObject ChooseMessagePrefab;

    [Header("Avatar Prefabs")] 
    public GameObject AuthorAvatarPrefab;
    public GameObject PlayerAvatarPrefab;

    [Header("Message List")] 
    public List<string> messageKeys = new List<string>();

    public ScrollRect scroll;

    private HashSet<string> generatedKeys = new HashSet<string>();
    private int currentIndex = 0;
    private string SaveFolder;
    private string SaveFilePath;
    private bool generatedCheck = false;
    private bool isChoosing = false;

    // UniTask 取消令牌，用于在 ResetAll 时安全切断所有异步操作
    private CancellationTokenSource resetCts;

    private void Awake()
    {
        SaveFolder = Path.Combine(Application.persistentDataPath, "data");
        SaveFilePath = Path.Combine(SaveFolder, "messages.json");
        
        // 绑定组件生命周期
        resetCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
    }

    private void Start()
    {
        LoadGeneratedKeys();
        generatedCheck = true;
    }

    public void CreateMessage(string localizationKey) => CreateMessage(localizationKey, false);

    public void CreateMessage(string localizationKey, bool forceCreate)
    {
        if (string.IsNullOrEmpty(localizationKey)) return;
        if (!forceCreate && isChoosing) return;

        if (!forceCreate && generatedCheck && currentIndex < generatedKeys.Count)
        {
            currentIndex++;
            return;
        }

        if (!forceCreate && generatedKeys.Contains(localizationKey))
        {
            Debug.Log($"Key '{localizationKey}' already generated. Skipping.");
            return;
        }

        string prefix = localizationKey.Substring(0, 2);

        if (prefix == "PC")
        {
            string id = localizationKey.Substring(3);
            CreateChooseMessage($"PM_{id}_Y", true);
            CreateChooseMessage($"PM_{id}_N", false);
            return;
        }

        if (prefix == "AC")
        {
            localizationKey = ResolveACKey(localizationKey);
            if (localizationKey == null) return;
            prefix = "AM";
        }

        // 头像处理
        string prevPrefix = generatedKeys.LastOrDefault()?.Substring(0, 2);
        if (prevPrefix != prefix)
        {
            var avatarPrefab = prefix == "AM" ? AuthorAvatarPrefab : PlayerAvatarPrefab;
            if (avatarPrefab != null)
            {
                var avatar = Instantiate(avatarPrefab, transform);
                var rect = avatar.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(prefix == "AM" ? 16f : -16f, rect.anchoredPosition.y);
            }
        }

        // 实例化文本框
        GameObject prefab = prefix == "AM" ? AuthorTextBox : PlayerAuthorBox;
        if (prefab == null) return;

        GameObject msg = Instantiate(prefab, transform);
        msg.transform.localPosition = Vector3.zero;

        // 本地化赋值
        var localized = msg.GetComponent<LocalizeStringEvent>();
        if (localized != null)
        {
            localized.StringReference.TableReference = prefix == "PM" ? "PlayerMessageTable" : "AuthorMessageTable";
            localized.StringReference.TableEntryReference = localizationKey;
            localized.RefreshString(); // 强制立刻刷新翻译
        }

        // 高性能瞬间排版与动画设置
        var box = msg.GetComponent<CustomTextBox>();
        if (box != null)
        {
            box.ForceRefreshSize();
            box.RefreshSizeIfNeeded(); // 立刻计算准确高度

            if (generatedCheck) // 如果是玩家游玩时生成（不是静默读档）
            {
                // 提前隐身，防止一帧闪烁
                box.transform.localScale = Vector3.zero;
                CanvasGroup cg = box.GetComponent<CanvasGroup>();
                if (cg == null) cg = msg.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                
                box.PlayShowAnimation();
            }
        }

        // 记录逻辑
        if (prefix == "AM" || prefix == "PM")
        {
            generatedKeys.Add(localizationKey);
            if (!forceCreate)
            {
                currentIndex++;
                SaveGeneratedKeys();
            }
        }

        // 异步等待布局刷新完成后滚动到底部
        ScrollToBottom(resetCts.Token).Forget();
    }

    private string ResolveACKey(string acKey)
    {
        string targetId = acKey.Substring(3);

        var lastPM = generatedKeys.LastOrDefault(k => k.StartsWith("PM_"));
        if (string.IsNullOrEmpty(lastPM)) return null;

        if (!lastPM.EndsWith("_Y") && !lastPM.EndsWith("_N")) return null;
        string choice = lastPM.EndsWith("_Y") ? "Y" : "N";

        return $"AM_{targetId}_{choice}";
    }

    private void CreateChooseMessage(string key, bool isYes)
    {
        GameObject choose = Instantiate(ChooseMessagePrefab, transform);
        choose.transform.localPosition = Vector3.zero;

        var localized = choose.GetComponent<LocalizeStringEvent>();
        if (localized != null)
        {
            localized.StringReference.TableReference = "PlayerMessageTable";
            localized.StringReference.TableEntryReference = key;
            localized.RefreshString();
        }

        CustomTextBox box = choose.GetComponent<CustomTextBox>();
        if (box != null)
        {
            box.ForceRefreshSize();
            box.RefreshSizeIfNeeded();

            box.transform.localScale = Vector3.zero;
            CanvasGroup cg = choose.GetComponent<CanvasGroup>();
            if (cg == null) cg = choose.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            box.PlayShowAnimation();
        }

        var panel = choose.transform.Find("Panel");
        if (panel)
        {
            var img = panel.GetComponent<Image>();
            if (img) img.color = isYes ? new Color(0, 1, 0, 0.39f) : new Color(1, 0, 0, 0.39f);
        }

        var btn = choose.GetComponent<Button>();
        if (btn)
        {
            btn.onClick.RemoveAllListeners();
            // 使用 UniTask 的 .Forget() 启动异步方法
            btn.onClick.AddListener(() => { DelayedChoose(key, isYes, resetCts.Token).Forget(); });
        }

        isChoosing = true;
        ScrollToBottom(resetCts.Token).Forget();
    }

    private async UniTaskVoid DelayedChoose(string key, bool isYes, CancellationToken token)
    {
        try
        {
            DestroyAllChooseMessages();
            isChoosing = false;
            
            // 等待一帧，确保销毁操作生效
            await UniTask.NextFrame(token);
            
            // 1. 生成玩家的选项气泡 (forceCreate = true)
            CreateMessage(key, true);
            
            currentIndex++;
            SaveGeneratedKeys();
        }
        catch (System.OperationCanceledException)
        {
            // 被 ResetAll 取消时的安全退出
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
    /// 高性能滚动到底部：等待 LateUpdate 布局计算完成后再滑动
    /// </summary>
    private async UniTaskVoid ScrollToBottom(CancellationToken token)
    {
        if (scroll == null) return;
        try
        {
            // 完美配合 CustomVerticalLayout 的 LateUpdate 机制
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);
            scroll.verticalNormalizedPosition = 0;
        }
        catch (System.OperationCanceledException) { }
    }

    // ==========================================
    // 存档与加载部分 (IO操作)
    // ==========================================

    private void LoadGeneratedKeys()
    {
        var saveData = LoadJson();
        var senderData = saveData.allSenders.Find(d => d.senderName == senderName);
        if (senderData == null) return;

        generatedKeys = new HashSet<string>(senderData.generatedKeys);
        string prevPrefix = "";

        foreach (var key in senderData.generatedKeys)
        {
            string prefix = key.Substring(0, 2);
            if (prefix != "AM" && prefix != "PM") continue;

            GameObject prefab = prefix == "AM" ? AuthorTextBox : PlayerAuthorBox;
            GameObject avatarPrefab = prefix == "AM" ? AuthorAvatarPrefab : PlayerAvatarPrefab;
            if (prefab == null || avatarPrefab == null) continue;
            
            if (prevPrefix != prefix)
            {
                var avatar = Instantiate(avatarPrefab, transform);
                var rect = avatar.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(prefix == "AM" ? 16f : -16f, rect.anchoredPosition.y);
            }

            GameObject msg = Instantiate(prefab, transform);
            msg.transform.localPosition = Vector3.zero;

            var localized = msg.GetComponent<LocalizeStringEvent>();
            if (localized != null)
            {
                localized.StringReference.TableReference = prefix == "PM" ? "PlayerMessageTable" : "AuthorMessageTable";
                localized.StringReference.TableEntryReference = key;
                localized.RefreshString();
            }

            var box = msg.GetComponent<CustomTextBox>();
            box?.ForceRefreshSize();
            box?.RefreshSizeIfNeeded();

            prevPrefix = prefix;
        }

        currentIndex = generatedKeys.Count;
        ScrollToBottom(resetCts.Token).Forget();
    }

    private void SaveGeneratedKeys()
    {
        if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);

        var saveData = LoadJson();
        var senderData = saveData.allSenders.Find(d => d.senderName == senderName);
        if (senderData == null)
        {
            senderData = new MessageData { senderName = senderName };
            saveData.allSenders.Add(senderData);
        }

        senderData.generatedKeys = generatedKeys.ToList();
        File.WriteAllText(SaveFilePath, JsonUtility.ToJson(saveData, true));
    }

    private MessageSaveData LoadJson()
    {
        if (File.Exists(SaveFilePath))
        {
            string json = File.ReadAllText(SaveFilePath);
            return JsonUtility.FromJson<MessageSaveData>(json);
        }
        return new MessageSaveData();
    }

    private void DeleteSenderFromJson()
    {
        var saveData = LoadJson();
        saveData.allSenders.RemoveAll(d => d.senderName == senderName);
        File.WriteAllText(SaveFilePath, JsonUtility.ToJson(saveData, true));
    }

    public void ResetAll()
    {
        // 1. 中断所有正在执行的 UniTask（比如延迟排版、延迟滑动）
        resetCts?.Cancel();
        resetCts?.Dispose();
        resetCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        currentIndex = 0; 
        generatedKeys.Clear(); 
        isChoosing = false; 

        // 2. 解除父子关系防止布局空隙，再销毁
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        DeleteSenderFromJson();
    }

    // ==========================================
    // 列表生成控制
    // ==========================================

    public void CreateMessageByList(int idx)
    {
        if (idx < 0 || idx >= messageKeys.Count) return; 
        CreateMessage(messageKeys[idx]);
    }

    public void CreateMessageNext()
    {
        if (isChoosing || currentIndex >= messageKeys.Count) return; 
        CreateMessage(messageKeys[currentIndex]);
    }
    
    public void CreateMessageNext(bool isLoop)
    {
        // 1. 列表为空或正在等待玩家选择时直接返回
        if (isChoosing || messageKeys.Count == 0) return; 

        // 2. 如果索引达到了列表末尾，使其归零，回到第一句话
        if (currentIndex >= messageKeys.Count)
        {
            currentIndex = 0;
        }

        // 3. 传入 forceCreate = true。
        // 绕过 generatedKeys.Contains 的拦截，并且跳过 SaveGeneratedKeys() 避免高频 IO 写入掉帧。
        CreateMessage(messageKeys[currentIndex], true);

        // 4. 当 forceCreate 为 true 时，CreateMessage 内部不会自动增加 currentIndex，
        // 因此需要在这里手动推进。
        currentIndex++;
    }

    public void CreateMessageAllList()
    {
        while (currentIndex < messageKeys.Count && !isChoosing)
            CreateMessage(messageKeys[currentIndex]);
    }
}