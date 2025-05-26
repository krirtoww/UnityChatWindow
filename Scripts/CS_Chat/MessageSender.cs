using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Linq;

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
    private CustomVerticalLayout layout;
    private string SaveFolder;
    private string SaveFilePath;
    private bool generatedCheck = false;
    private bool isChoosing = false;

    private void Awake()
    {
        SaveFolder = Path.Combine(Application.persistentDataPath, "data");
        SaveFilePath = Path.Combine(SaveFolder, "messages.json");
    }

    private void Start()
    {
        layout = GetComponent<CustomVerticalLayout>();
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
            if (localizationKey == null)
            {
                return;
            }
            prefix = "AM";
        }

        // Create avatar if speaker changed
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

        GameObject prefab = prefix == "AM" ? AuthorTextBox : PlayerAuthorBox;
        if (prefab == null) return;

        GameObject msg = Instantiate(prefab, transform);
        msg.transform.localPosition = Vector3.zero;

        var localized = msg.GetComponent<LocalizeStringEvent>();
        if (localized != null)
        {
            localized.StringReference.TableReference = prefix == "PM" ? "PlayerMessageTable" : "AuthorMessageTable";
            localized.StringReference.TableEntryReference = localizationKey;
            localized.RefreshString();
        }

        var box = msg.GetComponent<CustomTextBox>();
        box?.ForceRefreshSize();
        if (generatedCheck) box?.PlayShowAnimation();

        layout.RefreshChildren();
        layout.RefreshAllTextBoxWidths();
        layout.UpdateLayout();

        if (prefix == "AM" || prefix == "PM")
        {
            generatedKeys.Add(localizationKey);
            if (!forceCreate)
            {
                currentIndex++;
                SaveGeneratedKeys();
            }
        }
        if (scroll != null) scroll.verticalNormalizedPosition = 0;

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
        box.ForceRefreshSize();
        layout.RefreshChildren();
        layout.RefreshAllTextBoxWidths();
        layout.UpdateLayout();

        box.PlayShowAnimation();

        var panel = choose.transform.Find("Panel");
        if (panel)
        {
            var img = panel.GetComponent<Image>();
            if (img)
                img.color = isYes ? new Color(0, 1, 0, 0.39f) : new Color(1, 0, 0, 0.39f);
        }

        var btn = choose.GetComponent<Button>();
        if (btn)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(DelayedChoose(key, isYes));
            });
        }

        isChoosing = true;
        if (scroll != null) scroll.verticalNormalizedPosition = 0;
    }

    private IEnumerator DelayedChoose(string key, bool isYes)
    {
        DestroyAllChooseMessages();
        isChoosing = false;
        yield return null;
        CreateMessage(key);
        layout.RefreshChildren();
        layout.RefreshAllTextBoxWidths();
        layout.UpdateLayout();
    }

    private void DestroyAllChooseMessages()
    {
        foreach (var btn in GetComponentsInChildren<Button>())
        {
            Destroy(btn.gameObject);
        }
    }

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

            prevPrefix = prefix;
        }

        currentIndex = generatedKeys.Count;
        Debug.Log(currentIndex);
        if (scroll != null) scroll.verticalNormalizedPosition = 0;
    }

    private void SaveGeneratedKeys()
    {
        if (!Directory.Exists(SaveFolder))
            Directory.CreateDirectory(SaveFolder);

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

    public void ResetAll()
    {
        currentIndex = 0;
        generatedKeys.Clear();
        isChoosing = false;

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        DeleteSenderFromJson();
    }

    private void DeleteSenderFromJson()
    {
        var saveData = LoadJson();
        saveData.allSenders.RemoveAll(d => d.senderName == senderName);
        File.WriteAllText(SaveFilePath, JsonUtility.ToJson(saveData, true));
    }

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

    public void CreateMessageAllList()
    {
        while (currentIndex < messageKeys.Count && !isChoosing)
            CreateMessage(messageKeys[currentIndex]);
    }
}
