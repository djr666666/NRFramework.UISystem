using System;
using NRFramework;
using UnityEngine;

public class Main : MonoBehaviour
{
    private void Awake()
    {
        Game.Instance.Init();
    }
    void Start()
    {
       OpenUI_Local<Ui_TEST_1_Temp>();
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
