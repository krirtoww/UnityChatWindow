using System.Collections.Generic;
using UnityEngine;

public class FairyTaleController : MonoBehaviour
{
    // 引用我们刚刚重构好的纯净版 UI 渲染器
    public CustomizedMessageSender chatUI;

    void Start()
    {
        // 游戏开始时，确保清理掉上一局的残留
        chatUI.ResetAll();
        
        // 开启第一章
        StartChapter1();
    }

    // ==========================================
    // 📖 第一章：岔路口
    // ==========================================
    private void StartChapter1()
    {
        // NPC 连续发送两句话介绍背景
        chatUI.CreateMessage(MessageRole.Author, "欢迎来到迷雾森林的交界处，勇敢的旅行者。");
        chatUI.CreateMessage(MessageRole.Author, "我是这里的守林猫头鹰。摆在你面前的有两条路，你要走哪一条？");

        // 构建分支选项
        var choices = new List<ChoiceData>
        {
            new ChoiceData 
            { 
                content = "走左边幽暗的【低语小径】", 
                isPositiveStyle = false, // 用红色底暗示危险
                onSelected = () => Chapter2_WhisperPath() // 点击后跳转到低语小径节点
            },
            new ChoiceData 
            { 
                content = "走右边明亮的【回音山谷】", 
                isPositiveStyle = true,  // 用绿色底暗示安全
                onSelected = () => Chapter2_EchoValley()  // 点击后跳转到回音山谷节点
            }
        };

        // 将选项推给 UI
        chatUI.CreateChoices(choices);
    }

    // ==========================================
    // 📖 第二章 A线：低语小径
    // ==========================================
    private void Chapter2_WhisperPath()
    {
        chatUI.CreateMessage(MessageRole.Author, "咕咕！你走进了低语小径。这里阴冷潮湿。");
        chatUI.CreateMessage(MessageRole.Author, "突然，你发现前方有一只巨大的红宝石巨龙正在沉睡，它庞大的身躯挡住了去路！");

        var choices = new List<ChoiceData>
        {
            new ChoiceData 
            { 
                content = "拔出长剑，准备战斗！", 
                isPositiveStyle = false, 
                onSelected = () => Ending_DragonFight() 
            },
            new ChoiceData 
            { 
                content = "踮起脚尖，悄悄溜过去。", 
                isPositiveStyle = true, 
                onSelected = () => Ending_DragonSneak() 
            }
        };

        chatUI.CreateChoices(choices);
    }

    // ==========================================
    // 📖 第二章 B线：回音山谷
    // ==========================================
    private void Chapter2_EchoValley()
    {
        chatUI.CreateMessage(MessageRole.Author, "聪明的选择！这里阳光明媚，但山风非常刺骨。");
        chatUI.CreateMessage(MessageRole.Author, "你的体力正在快速流失，前方有一个废弃的营地。");

        var choices = new List<ChoiceData>
        {
            new ChoiceData 
            { 
                content = "停下来生火取暖。", 
                isPositiveStyle = true, 
                onSelected = () => Ending_CampFire() 
            },
            new ChoiceData 
            { 
                content = "咬紧牙关，继续赶路寻找出口。", 
                isPositiveStyle = false, 
                onSelected = () => Ending_KeepRunning() 
            }
        };

        chatUI.CreateChoices(choices);
    }

    // ==========================================
    // 🏁 结局节点区
    // ==========================================
    
    private void Ending_DragonFight()
    {
        chatUI.CreateMessage(MessageRole.Author, "你的剑砍在龙鳞上，竟然断成了两截！");
        chatUI.CreateMessage(MessageRole.Author, "巨龙醒了，打了个喷嚏，把你吹回了森林入口。");
        chatUI.CreateMessage(MessageRole.Author, "【结局：不自量力的勇者】");
        
        OfferRestart();
    }

    private void Ending_DragonSneak()
    {
        chatUI.CreateMessage(MessageRole.Author, "你屏住呼吸，成功绕过了巨龙。");
        chatUI.CreateMessage(MessageRole.Author, "在巨龙的尾巴后面，你发现了一个装满金币的宝箱！");
        chatUI.CreateMessage(MessageRole.Author, "【结局：满载而归的潜行者】");

        OfferRestart();
    }

    private void Ending_CampFire()
    {
        chatUI.CreateMessage(MessageRole.Author, "火焰让你恢复了温暖。");
        chatUI.CreateMessage(MessageRole.Author, "但火光引来了热情的地精，他们邀请你参加了一整晚的篝火晚会。你度过了愉快的一夜。");
        chatUI.CreateMessage(MessageRole.Author, "【结局：森林里的派对之王】");

        OfferRestart();
    }

    private void Ending_KeepRunning()
    {
        chatUI.CreateMessage(MessageRole.Author, "你没有停下，但最终在寒风中冻成了一座冰雕。");
        chatUI.CreateMessage(MessageRole.Author, "几百年后，你成了回音山谷的著名旅游景点。");
        chatUI.CreateMessage(MessageRole.Author, "【结局：永恒的冰雕】");

        OfferRestart();
    }

    // ==========================================
    // 🔄 循环控制：重新开始
    // ==========================================
    private void OfferRestart()
    {
        var choices = new List<ChoiceData>
        {
            new ChoiceData 
            { 
                content = "再玩一次！", 
                isPositiveStyle = true, 
                onSelected = () => 
                {
                    // 清空屏幕，重新调用第一章
                    chatUI.ResetAll();
                    StartChapter1();
                } 
            }
        };
        
        chatUI.CreateChoices(choices);
    }
}