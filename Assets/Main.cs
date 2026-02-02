using NRFramework;
using NRFramework.UI;
using UnityEngine;

public class Main : MonoBehaviour
{
    private void Awake()
    {
        NRFramework.UI.Game.Instance.Init();
    }
    void Start()
    {
       OpenUI_Local<Ui_TEST_1_Temp>();
    }

    public static T OpenUI_Local<T>() where T : UIPanel
    {
        T uiPanel = null;
        var csName = typeof(T).Name;
        string result = csName.Replace("_Temp", "");
        int panelType = (int)UIPathConstants.UILayerDictionary[result];
        var uiroot = Game.Instance.uiRoots[panelType];
        var path = UIPathConstants.UIPathDictionary[result];
        uiPanel = uiroot.uI.CreatePanel<T>(csName, path);
        uiPanel.gameObject.transform.SetParent(uiroot.obj.transform);


        UnityEngine.Debug.Log($"csName  ={csName} result ={result}  uiroot ={uiroot} path ={path}");
        return uiPanel;
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
