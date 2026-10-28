using System;
using NRFramework;
using UnityEngine;

public class Main : MonoBehaviour
{
    [Header("测试用按键（可在 Inspector 里改）")]
    [SerializeField] private KeyCode m_OpenKey = KeyCode.Q;
    [SerializeField] private KeyCode m_CloseKey = KeyCode.E;

    private void Awake()
    {
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
