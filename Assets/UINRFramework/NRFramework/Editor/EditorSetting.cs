// https://github.com/NRatel/NRFramework.UI

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NRFramework
{
    public class EditorSetting : ScriptableObject
    {
        public bool enableOpElementHierarchy = true;

     

        // UI类生成根目录（相对于 Application.dataPath）
        // 将在相对路径下创建对应基类。
        // 将在相对路径下创建快捷类。创建后应自行改名（避免覆盖）。
        public string generatedBaseUIRootDir = "Project/Scripts/Gui/UIPanelBase";
        public string generatedTempUIRootDir = "Project/Scripts/Gui/UIPanel";

        //Project/Prefabs/Gui  自己改的文件夹路径   确保这个地址能找到 预制体
        public string uiPrefabRootDir = "Project/Prefabs/Gui";
        // 配置 asset 存到【使用方项目】的 Assets 下——不能指向包目录：
        //   UPM 引入后本框架在 Packages/ 下（只读），且使用方 Assets 里没有 UINRFramework 目录，
        //   原来硬编码 "Assets/UINRFramework/NRFramework/Editor/..." 会因目录不存在导致 CreateAsset 失败。
        //   改到中性的 Assets/NRFramework/，并在创建前自动建目录。
        private const string kAssetPath = "Assets/NRFramework/EditorSetting.asset";


        private static EditorSetting sm_Instance = null;
        public static EditorSetting Instance
        {
            get
            {
                if (sm_Instance == null)
                {
                    sm_Instance = AssetDatabase.LoadAssetAtPath<EditorSetting>(kAssetPath);
#if UNITY_EDITOR
                    if (sm_Instance == null)
                    {
                        sm_Instance = CreateInstance<EditorSetting>();
                        // 目标目录在使用方项目里可能不存在，先建再创建 asset（否则 CreateAsset 失败）
                        string dir = System.IO.Path.GetDirectoryName(kAssetPath);
                        if (!System.IO.Directory.Exists(dir))
                        {
                            System.IO.Directory.CreateDirectory(dir);
                            AssetDatabase.Refresh();
                        }
                        AssetDatabase.CreateAsset(sm_Instance, kAssetPath);
                        AssetDatabase.SaveAssets();
                    }
#else
                    Debug.Assert(sm_Instance != null);
#endif
                }
                return sm_Instance;
            }
        }

#if UNITY_EDITOR
        [MenuItem("NRFramework/EditorSetting", false, 999)]
        public static void Select()
        {
            Debug.Log("Application.dataPath: " + Application.dataPath);
            Selection.activeObject = Instance;
        }
#endif
    }
}
