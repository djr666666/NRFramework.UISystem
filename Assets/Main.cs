using System;
using NRFramework;
using UnityEngine;

public class Main : MonoBehaviour
{
    [Header("测试用按键（可在 Inspector 里改）")]
    [SerializeField] private KeyCode m_OpenKey = KeyCode.Q;
    [SerializeField] private KeyCode m_CloseKey = KeyCode.E;

    [Header("异步时序测试")]
    [Tooltip("勾上后把加载器换成「延迟若干帧返回」的假异步版本，并自动挂上 UIAsyncTimingTest。\n" +
             "用来验证框架里那些只有加载跨帧时才会走到的分支（Loading 占位 / 加载中取消 / 加载流水号）。\n" +
             "平时开发请取消勾选。")]
    [SerializeField] private bool m_UseDelayLoader = false;
    [SerializeField] private int m_DelayFrames = 5;

    private void Awake()
    {
        //必须在 Game.Init 之前替换，Init 之后再换对已发起的加载不生效
        if (m_UseDelayLoader)
        {
            UIRes.Loader = new DelayUIResLoader(m_DelayFrames);
            gameObject.AddComponent<UIAsyncTimingTest>().delayFrames = m_DelayFrames;
            Debug.Log("<color=#FFD24A>[Main] 已启用延迟加载器。按 1~5 跑单个时序用例，按 0 跑全部</color>");
        }

        Game.Instance.Init();
    }
    void Start()
    {
        //改成按 Q 手动打开，方便观察打开动画。想恢复「一进场景就开」，把下面这行的注释去掉即可。
        //OpenUI_Local<Ui_TEST_1_Temp>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(m_OpenKey))
        {
            Debug.Log($"<color=#7FFF7F>[Main] 按下 {m_OpenKey} → 打开 Ui_TEST_1_Temp</color>");
            OpenUI_Local<Ui_TEST_1_Temp>();
        }

        if (Input.GetKeyDown(m_CloseKey))
        {
            Debug.Log($"<color=#FF9F7F>[Main] 按下 {m_CloseKey} → 关闭 Ui_TEST_1_Temp</color>");
            CloseUI_Local<Ui_TEST_1_Temp>();
        }
    }

    // 异步开面板：加载完成回调里拿到 panel。成功 success=true + panel；失败 success=false + null。
    public static void OpenUI_Local<T>(Action<T> onOpened = null) where T : UIPanel
    {
        var csName = typeof(T).Name;
        string result = csName.Replace("_Temp", "");
        int panelType = (int)UIPathConstants.UILayerDictionary[result];
        var uiroot = Game.Instance.uiRoots[panelType];
        var path = UIPathConstants.UIPathDictionary[result];
        uiroot.uI.CreatePanelAsync<T>(csName, path, (success, uiPanel) =>
        {
            if (!success) { onOpened?.Invoke(null); return; }
            uiPanel.gameObject.transform.SetParent(uiroot.obj.transform);
            UnityEngine.Debug.Log($"csName  ={csName} result ={result}  uiroot ={uiroot} path ={path}");
            onOpened?.Invoke(uiPanel);
        });
    }



    public static void CloseUI_Local<T>() where T : UIPanel
    {
        var csName = typeof(T).Name;
        string result = csName.Replace("_Temp", "");
        int panelType = (int)UIPathConstants.UILayerDictionary[result];
        var uiroot = Game.Instance.uiRoots[panelType];
        uiroot.uI.ClosePanel<T>();
    }
}
