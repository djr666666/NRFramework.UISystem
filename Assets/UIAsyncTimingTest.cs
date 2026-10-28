using System.Collections;
using NRFramework;
using UnityEngine;

/// <summary>
/// 异步加载时序测试（开发期工具，不参与正式流程）。
///
/// 要验的是框架里那些「只有在加载跨帧时才会走到」的分支：Loading 占位、加载中取消、加载流水号核验。
/// 编辑器默认加载器是同步加载 + 立即回调，这些分支一行都不会执行，
/// 所以平时跑着没事，不代表接了 YooAsset / Addressables 之后还没事。
///
/// 用法：
/// 1. 在启动脚本里、Game.Instance.Init() 之前加一行：UIRes.Loader = new DelayUIResLoader(5);
/// 2. 把本组件挂到场景里任意一个常驻物体上（比如挂 Main 的那个）
/// 3. 运行，点一下 Game 窗口让它获得焦点，按主键盘 1~5 跑单个用例，按 0 依次跑完全部
/// 4. 看 Console 里每个用例结尾的【通过】/【失败】
///
/// 用例里写死了 Ui_TEST_1_Temp，换成自己的面板改 kPanelId 即可。
/// </summary>
public class UIAsyncTimingTest : MonoBehaviour
{
    private const string kPanelId = nameof(Ui_TEST_1_Temp);

    [Tooltip("要和 DelayUIResLoader 的延迟帧数保持一致或更大，用来决定每步等多久")]
    public int delayFrames = 5;

    private bool m_Running;

    private void Update()
    {
        if (m_Running) { return; }

        if (Input.GetKeyDown(KeyCode.Alpha1)) { StartCoroutine(Run(Case1_OpenClose())); }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) { StartCoroutine(Run(Case2_CloseWhileLoading())); }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) { StartCoroutine(Run(Case3_ReopenRightAfterCancel())); }
        else if (Input.GetKeyDown(KeyCode.Alpha4)) { StartCoroutine(Run(Case4_OpenTwiceSameFrame())); }
        else if (Input.GetKeyDown(KeyCode.Alpha5)) { StartCoroutine(Run(Case5_DestroyWhileLoading())); }
        else if (Input.GetKeyDown(KeyCode.Alpha0)) { StartCoroutine(RunAll()); }
    }

    private IEnumerator RunAll()
    {
        yield return Run(Case1_OpenClose());
        yield return Run(Case2_CloseWhileLoading());
        yield return Run(Case3_ReopenRightAfterCancel());
        yield return Run(Case4_OpenTwiceSameFrame());
        yield return Run(Case5_DestroyWhileLoading());
        Debug.Log("<color=#FFD24A>========== 全部用例跑完 ==========</color>");
    }

    private IEnumerator Run(IEnumerator routine)
    {
        m_Running = true;
        yield return CleanUp();
        yield return routine;
        yield return CleanUp();
        m_Running = false;
    }

    #region 用例

    private IEnumerator Case1_OpenClose()
    {
        Begin(1, "正常开关（基线）", "加载完成后面板存在；关闭后 panelDict / panelStateDict 都清空，场景里不残留实例");

        UIPanel opened = null;
        bool called = false;
        Main.OpenUI_Local<Ui_TEST_1_Temp>((p) => { opened = p; called = true; });

        yield return WaitFrames(delayFrames + 3);
        Check("打开回调已触发且拿到实例", called && opened != null);
        Check("面板在 panelDict 里", Root.ExistPanel(kPanelId));
        Check("场景里只有 1 个面板实例", CountPanelObjects() == 1);

        Main.CloseUI_Local<Ui_TEST_1_Temp>();
        yield return WaitFrames(3);
        Check("面板已从 panelDict 移除", !Root.ExistPanel(kPanelId));
        Check("panelStateDict 占位已清", !Root.panelStateDict.ContainsKey(kPanelId));
        Check("场景里没有残留实例", CountPanelObjects() == 0);

        End();
    }

    private IEnumerator Case2_CloseWhileLoading()
    {
        Begin(2, "加载中途关闭", "这次打开应被取消：回调返回失败，面板不出现，场景里不留幽灵实例");

        bool called = false;
        UIPanel opened = null;
        Main.OpenUI_Local<Ui_TEST_1_Temp>((p) => { opened = p; called = true; });

        yield return WaitFrames(1);
        Check("此刻状态是 Loading（说明确实在异步加载）",
            Root.panelStateDict.TryGetValue(kPanelId, out UIPanelState st) && st == UIPanelState.Loading);

        Main.CloseUI_Local<Ui_TEST_1_Temp>();
        yield return WaitFrames(delayFrames + 5);

        Check("打开回调已触发", called);
        Check("回调结果是失败（被取消）", opened == null);
        Check("面板没有进 panelDict", !Root.ExistPanel(kPanelId));
        Check("panelStateDict 占位已清", !Root.panelStateDict.ContainsKey(kPanelId));
        Check("场景里没有幽灵实例", CountPanelObjects() == 0);

        End();
    }

    private IEnumerator Case3_ReopenRightAfterCancel()
    {
        Begin(3, "加载中关闭后立刻重开",
            "第二次打开应该成功：这是「加载流水号」修复的关键用例。修之前先返回的第一次会抢占 panelDict，第二次反而收到失败");

        bool called1 = false, called2 = false;
        UIPanel opened1 = null, opened2 = null;
        Main.OpenUI_Local<Ui_TEST_1_Temp>((p) => { opened1 = p; called1 = true; });

        yield return WaitFrames(1);
        Main.CloseUI_Local<Ui_TEST_1_Temp>();
        Main.OpenUI_Local<Ui_TEST_1_Temp>((p) => { opened2 = p; called2 = true; });

        yield return WaitFrames(delayFrames * 2 + 6);

        Check("第一次回调已触发且结果是失败（已被取消）", called1 && opened1 == null);
        Check("第二次回调已触发且结果是成功", called2 && opened2 != null);
        Check("面板在 panelDict 里", Root.ExistPanel(kPanelId));
        Check("场景里只有 1 个面板实例（没有被取消的那个残留）", CountPanelObjects() == 1);

        End();
    }

    private IEnumerator Case4_OpenTwiceSameFrame()
    {
        Begin(4, "同一帧连开两次", "第二次应被判重拒绝并打警告，只创建 1 个实例");

        bool called1 = false, called2 = false;
        UIPanel opened1 = null, opened2 = null;
        Main.OpenUI_Local<Ui_TEST_1_Temp>((p) => { opened1 = p; called1 = true; });
        Main.OpenUI_Local<Ui_TEST_1_Temp>((p) => { opened2 = p; called2 = true; });

        yield return WaitFrames(delayFrames + 5);

        Check("第一次回调成功", called1 && opened1 != null);
        Check("第二次回调被拒绝（返回失败）", called2 && opened2 == null);
        Check("场景里只有 1 个面板实例", CountPanelObjects() == 1);

        End();
    }

    private IEnumerator Case5_DestroyWhileLoading()
    {
        Begin(5, "加载中途 DestroyPanel", "同用例 2，但走的是销毁分支：不应留下任何实例或占位");

        bool called = false;
        UIPanel opened = null;
        Main.OpenUI_Local<Ui_TEST_1_Temp>((p) => { opened = p; called = true; });

        yield return WaitFrames(1);
        Root.DestroyPanel(kPanelId);
        yield return WaitFrames(delayFrames + 5);

        Check("打开回调已触发且结果是失败", called && opened == null);
        Check("面板没有进 panelDict", !Root.ExistPanel(kPanelId));
        Check("panelStateDict 占位已清", !Root.panelStateDict.ContainsKey(kPanelId));
        Check("场景里没有幽灵实例", CountPanelObjects() == 0);

        End();
    }

    #endregion

    #region 工具

    //每个用例开始前把环境恢复干净，免得上一个用例的残留影响判断
    private IEnumerator CleanUp()
    {
        Root.DestroyPanel(kPanelId);
        yield return WaitFrames(delayFrames + 3);
        Root.DestroyPanel(kPanelId);   //可能有慢回来的加载在这期间把面板塞进去，再清一次
        yield return WaitFrames(2);
    }

    private static UIRoot Root
    {
        get
        {
            string result = kPanelId.Replace("_Temp", "");
            int layer = (int)UIPathConstants.UILayerDictionary[result];
            return Game.Instance.uiRoots[layer].uI;
        }
    }

    private static int CountPanelObjects()
    {
        return FindObjectsOfType<UIPanelBehaviour>(true).Length;
    }

    private static IEnumerator WaitFrames(int count)
    {
        for (int i = 0; i < count; i++) { yield return null; }
    }

    private int m_Failed;

    private void Begin(int index, string title, string expect)
    {
        m_Failed = 0;
        Debug.Log($"<color=#FFD24A>———— 用例 {index}：{title} ————</color>\n预期：{expect}");
    }

    private void Check(string desc, bool ok)
    {
        if (ok) { Debug.Log($"<color=#7FFF7F>  ✔ {desc}</color>"); }
        else { m_Failed++; Debug.LogError($"  ✘ {desc}"); }
    }

    private void End()
    {
        if (m_Failed == 0) { Debug.Log("<color=#7FFF7F>【通过】</color>"); }
        else { Debug.LogError($"【失败】{m_Failed} 项不符合预期"); }
    }

    #endregion
}
