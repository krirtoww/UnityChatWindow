![image](https://github.com/Alivecat/UnityChatWindow/blob/main/Demo.gif)


# 项目快速上手指南

---

## 🛠️ 环境要求与资源准备

* **Unity 版本**：本项目使用 **Unity 6000.3.13f1** 创建。
* **推荐字体**：**Noto Sans SC**
    * [Google Fonts 下载链接](https://fonts.google.com/noto/specimen/Noto+Sans+SC?preview.script=Hans)
    * *注：你也可以根据需要使用其他中文字体。*

---

## 1. 项目初始化与插件导入

1. **创建项目**：新建一个本地 **UPR (Universal Project Report)** 项目。
2. **资源导入**：
    * 将下载的资源文件复制到工程的 `Assets` 文件夹中。
    * 依次导入以下三个核心插件：`DOTween`, `UniTask`, `Localization`。
3. **输入系统配置**：
    * 依次点击：`Project Settings` -> `Player` -> `Active Input Handling`。
    * 将选项改为 **Both**。
    * **注意**：修改后 Unity 会自动重启以应用设置。

---

## 2. 本地化配置 (Localization)

### 基础设置
1. 在 `Project Settings` -> `Localization` -> `Active Setting` 中点击添加 **Localization Settings**。
2. 在 `Available Locales` 中点击 **Add All**，确保 **Chinese (Simplified) (zh)** 和 **English (en)** 出现。

### Table 资源设置
1. **Addressable 标记**：
    在 `Assets/Localization/Tables` 文件夹下，选中以下文件，并在 **Inspector** 面板左上角勾选 **Addressable**：
    * `AuthorMessageTable Shared Data / AuthorMessageTable_en / AuthorMessageTable_zh`
    * `PlayerMessageTable Shared Data / PlayerMessageTable_en / PlayerMessageTable_zh`
2. **预加载 (Preload) 设置**：
    * 选中 `AuthorMessageTable`，点击 Inspector 中的 **Open In Table Editor**。
    * 在窗口中勾选 **Preload**。并在 `Selected collection` 中确保两张表均已勾选。
3. **刷新资源**：右键点击 `Assets/Localization/Tables` 文件夹，选择 **Reimport**。

---

## 3. TextMesh Pro (TMP) 中文字体配置

1. **生成 SDF 字体**：右键点击字体文件 -> `Create` -> `TextMeshPro` -> **Font Asset**。系统会自动生成对应的 `SDF` 资产。
2. **替换字体资产**：在 `Prefab` 文件夹中找到 `ChooseBox`, `TexBoxLeft`, `TextBoxRight`。将其 `TextMeshPro - Text` 脚本中的 **Font Asset** 替换为新生成的 `SDF` 资产。

---

## 4. 运行 Demo 1 (基础功能)

1. 打开 `ChatWindowsSample` 场景，点击 **Play**。
2. 点击画面右上角 **方块图标** 弹出窗口，点击 **AddMessage** 验证显示。

---

## 5. 运行 Demo 2 (性能测试与自定义内容)

1. **场景路径**：打开 `SimpleChatScene` 场景。
2. **性能测试 (Performance Test)**：
    * 场景中的 `Main Camera` 默认挂载了 `PerformanceTestController`。
    * 运行后，系统会自动推入 **200 条** 中文对话框以进行性能压力测试。
    * *注：脚本会自动尝试点击选项，若失灵请手动点击。*
3. **交互故事 (Interactive Story)**：
    * 将 `Main Camera` 上的脚本更换为 `FairyTaleController`。
    * **重要**：确保在 Inspector 中手动绑定 `ChatUI` 引用。
    * 运行后可体验一个简单的中文交互式小故事。
4. **技术说明**：
    * 这两个例子展示了如何通过 `CustomizedMessageSender` 类直接输入自定义对话内容，实现**不依赖本地化表 (Localization Tables)** 的动态文本推送。


---


# Project Quick Start Guide

---

## 🛠️ Prerequisites & Resources

* **Unity Version**: Created with **Unity 6000.3.13f1**.
* **Recommended Font**: **Noto Sans SC**
    * [Download from Google Fonts](https://fonts.google.com/noto/specimen/Noto+Sans+SC?preview.script=Hans)
    * *Note: Other Chinese fonts are also supported.*

---

## 1. Project Initialization

1. **Create Project**: Create a new local **UPR** project.
2. **Import Assets**: Copy files to `Assets` and import `DOTween`, `UniTask`, and `Localization`.
3. **Input System**: Set `Active Input Handling` to **Both** in `Project Settings -> Player`. Unity will restart.

---

## 2. Localization Setup

1. **Settings**: Add **Localization Settings** in Project Settings. Add **Chinese (Simplified) (zh)** and **English (en)** in `Available Locales`.
2. **Addressables**: In `Assets/Localization/Tables`, mark all Table files as **Addressable** in the Inspector.
3. **Preload**: Open `AuthorMessageTable` in **Table Editor**, check **Preload**, and ensure both tables are selected under `Selected collection`.
4. **Reimport**: Right-click the Tables folder and select **Reimport**.

---

## 3. TextMesh Pro (TMP) Setup

1. **SDF Generation**: Right-click your font -> `Create -> TextMeshPro -> Font Asset`.
2. **Assign Font**: In the `Prefab` folder, update `ChooseBox`, `TexBoxLeft`, and `TextBoxRight` by replacing their **Font Asset** with the new `SDF` asset.

---

## 4. Running Demo 1 (Basic)

1. Open `ChatWindowsSample` scene and press **Play**.
2. Click the **Square Icon** (top-right) and use **AddMessage** to test.

---

## 5. Running Demo 2 (Performance & Custom Content)

1. **Scene Path**: Open the `SimpleChatScene`.
2. **Performance Test**:
    * The `Main Camera` is equipped with the `PerformanceTestController` by default.
    * It automatically injects **200 Chinese dialogue boxes** to simulate a stress test.
    * *Note: The script attempts to auto-click options; click manually if it stalls.*
3. **Interactive Story**:
    * Replace the controller on `Main Camera` with `FairyTaleController`.
    * **Crucial**: Manually bind the `ChatUI` reference in the Inspector.
    * This runs a simple interactive narrative in Chinese.
4. **Technical Highlight**:
    * These examples demonstrate the `CustomizedMessageSender` class, allowing you to inject custom dialogue content dynamically **without relying on Localization Tables**.
