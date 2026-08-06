// https://github.com/NRatel/NRFramework.UI

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NRFramework
{
    [CustomEditor(typeof(UIViewBehaviour))]
    public abstract class UIViewBehaviourEditor : Editor
    {
        protected ReorderableList m_OpElementListRL;

        protected virtual void OnEnable()
        {
            m_OpElementListRL = CreateReorderableList(serializedObject.FindProperty("m_OpElementList"));
        }

        protected void DrawOpElementList()
        {
            m_OpElementListRL.DoLayoutList();
        }

       protected void DrawExpoertButton()
   {
       // 导出记录（纯文本凭据）：改名后与预制体名对不上，开发者据此点下方 UpdateName 刷新脚本名
       var _vb = (UIViewBehaviour)target;
       EditorGUILayout.LabelField("Base 记录", string.IsNullOrEmpty(_vb.exportedBaseName) ? "未导出" : _vb.exportedBaseName);
       EditorGUILayout.LabelField("Temp 记录", string.IsNullOrEmpty(_vb.exportedTempName) ? "未导出" : _vb.exportedTempName);
       EditorGUILayout.Space(4);

       GUILayout.BeginHorizontal();
       {
           if (GUILayout.Button("ExportBase"))
           {
               if (Application.isPlaying) { Debug.LogError("请在非运行时导出"); return; }
               RefreshOpElementList(m_OpElementListRL);
               GenerateUIBaseCode();
           }

            if (GUILayout.Button("ExportTemp"))
   {
       if (Application.isPlaying) { Debug.LogError("请在非运行时导出"); return; }

       // 检查文件是否已存在
       string prefabPath = GetPrefabPath();
       if (!string.IsNullOrEmpty(prefabPath))
       {
           string fullPrefabPath = Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath("Assets", prefabPath)));
           string fullRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.uiPrefabRootDir));
           string subPath = Path.GetRelativePath(fullRootDir, fullPrefabPath);
           string className = Path.GetFileNameWithoutExtension(subPath);
           string subSavePath = Path.Combine(Path.GetDirectoryName(subPath), className + "_Temp.cs");
           string savePath = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.generatedTempUIRootDir, subSavePath));
           string assetPath = "Assets" + savePath.Substring(Application.dataPath.Length).Replace("\\", "/");

           string guid = AssetDatabase.AssetPathToGUID(assetPath);
           if (!string.IsNullOrEmpty(guid))
           {
               bool confirm = EditorUtility.DisplayDialog(
                   "覆盖确认",
                   $"{className}_Temp.cs 已存在，覆盖后手写代码将丢失，确认导出？",
                   "确认覆盖",
                   "取消"
               );
               if (!confirm) return;
           }
       }

       RefreshOpElementList(m_OpElementListRL);
       GenerateUITempCode();
   }
     
       }
       GUILayout.EndHorizontal();

       GUILayout.BeginHorizontal();
       {

           if (GUILayout.Button("FindBase"))
           {
               if (Application.isPlaying) { Debug.LogError("请在非运行时定位"); return; }
               LocateUIBaseCode();
           }
           if (GUILayout.Button("FindTemp"))
           {
               if (Application.isPlaying) { Debug.LogError("请在非运行时定位"); return; }
               LocateUITempCode();
           }
       }
       GUILayout.EndHorizontal();

       GUILayout.BeginHorizontal();
       {
           if (GUILayout.Button("UpdateBaseName"))
           {
               if (Application.isPlaying) { Debug.LogError("请在非运行时操作"); return; }
               UpdateUIBaseName();
           }
           if (GUILayout.Button("UpdateTempName"))
           {
               if (Application.isPlaying) { Debug.LogError("请在非运行时操作"); return; }
               UpdateUITempName();
           }
       }
       GUILayout.EndHorizontal();
   }


      private void LocateUIBaseCode()
      {
          string prefabPath = GetPrefabPath();
          if (string.IsNullOrEmpty(prefabPath))
          {
              Debug.LogError("非预设不可定位");
              return;
          }

          string fullPrefabPath = Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath("Assets", prefabPath)));
          string fullRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.uiPrefabRootDir));

          if (!fullPrefabPath.StartsWith(fullRootDir))
          {
              EditorUtility.DisplayDialog("定位失败",
                  "预设不在配置的可定位根目录下，你写的配置和实际路径不一致（注意大小写）。\n\n" +
                  "配置根目录：" + fullRootDir + "\n" +
                  "当前预设：" + fullPrefabPath + "\n\n" +
                  "请到 EditorSetting 的 Ui Prefab Root Dir 改成一致后，再重试。", "知道了");
              return;
          }

          string subPath = Path.GetRelativePath(fullRootDir, fullPrefabPath);
          string className = Path.GetFileNameWithoutExtension(subPath);
          string subSavePath = Path.Combine(Path.GetDirectoryName(subPath), className + "Base.cs");
          string savePath = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.generatedBaseUIRootDir, subSavePath));

          string assetPath = "Assets" + savePath.Substring(Application.dataPath.Length).Replace("\\", "/");

          // 用 GUID 查找，不触发编译
          string guid = AssetDatabase.AssetPathToGUID(assetPath);
          if (string.IsNullOrEmpty(guid))
          {
              Debug.LogWarning("文件不存在，请先 ExportTemp：" + assetPath);
              return;
          }

          UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
          //EditorUtility.FocusProjectWindow(); 打开inspector 就会跳过去
          //Selection.activeObject = obj; 打开inspector 就会跳过去
          EditorGUIUtility.PingObject(obj);
      }

      private void LocateUITempCode()
      {
          string prefabPath = GetPrefabPath();
          if (string.IsNullOrEmpty(prefabPath))
          {
              Debug.LogError("非预设不可定位");
              return;
          }

          string fullPrefabPath = Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath("Assets", prefabPath)));
          string fullRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.uiPrefabRootDir));

          if (!fullPrefabPath.StartsWith(fullRootDir))
          {
              EditorUtility.DisplayDialog("定位失败",
                  "预设不在配置的可定位根目录下，你写的配置和实际路径不一致（注意大小写）。\n\n" +
                  "配置根目录：" + fullRootDir + "\n" +
                  "当前预设：" + fullPrefabPath + "\n\n" +
                  "请到 EditorSetting 的 Ui Prefab Root Dir 改成一致后，再重试。", "知道了");
              return;
          }

          string subPath = Path.GetRelativePath(fullRootDir, fullPrefabPath);
          string className = Path.GetFileNameWithoutExtension(subPath);
          string subSavePath = Path.Combine(Path.GetDirectoryName(subPath), className + "_Temp.cs");
          string savePath = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.generatedTempUIRootDir, subSavePath));

          string assetPath = "Assets" + savePath.Substring(Application.dataPath.Length).Replace("\\", "/");

          UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
          if (obj == null)
          {
              Debug.LogWarning("文件不存在，请先 ExportTemp：" + assetPath);
              return;
          }

          //EditorUtility.FocusProjectWindow(); 打开inspector 就会跳过去
          //Selection.activeObject = obj; 打开inspector 就会跳过去
          EditorGUIUtility.PingObject(obj);
      }












        private ReorderableList CreateReorderableList(SerializedProperty opElementListSP)
        {
            ReorderableList reorderableList = new ReorderableList(serializedObject, opElementListSP)
            {
                elementHeight = EditorGUIUtility.singleLineHeight * 1.2f
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.y += EditorGUIUtility.singleLineHeight * 0.1f;

                SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                GUI.Label(rect, "OpElementList");
            };

            //重写原因（相对ReorderableList源码中的默认实现DrawFooter）：
            //1、删除 list.displayAdd 和 list.displayRemove 的判断逻辑。（不需要，要让+-按钮永远保留）
            //2、删除 增加+按钮时的 onAddDropdownCallback相关逻辑。（不需要）
            //4、增加一个清空按钮。
            //3、增加一个整理按钮。
            reorderableList.drawFooterCallback = (Rect rect) =>
            {
                ReorderableList list = reorderableList;
                ReorderableList.Defaults defaults = ReorderableList.defaultBehaviours;

                float rightMargin = 10f;
                float leftPading = 10f;
                float rightPading = 10f;
                float singleWidth = 25f;
                float singleHeight = 16f;
                float spacing = 5f;
                int btnsCount = 4;

                float rightEdge = rect.xMax - rightMargin;
                float btnsWidth = leftPading + rightPading + singleWidth * btnsCount + spacing * (btnsCount - 1);
                Rect btnsRect = new Rect(rightEdge - btnsWidth, rect.y, btnsWidth, rect.height);

                Rect addRect = new Rect(btnsRect.x + leftPading, btnsRect.y, singleWidth, singleHeight);
                Rect removeRect = new Rect(btnsRect.x + leftPading + (singleWidth + spacing) * 1, btnsRect.y, singleWidth, singleHeight);
                Rect trashRect = new Rect(btnsRect.x + leftPading + (singleWidth + spacing) * 2, btnsRect.y, singleWidth, singleHeight);
                Rect refreshRect = new Rect(btnsRect.x + leftPading + (singleWidth + spacing) * 3, btnsRect.y, singleWidth, singleHeight);

                if (UnityEngine.Event.current.type == EventType.Repaint)
                {
                    defaults.footerBackground.Draw(btnsRect, false, false, false, false);
                }

                using (new EditorGUI.DisabledScope(list.onCanAddCallback != null && !list.onCanAddCallback(list)))
                {
                    if (GUI.Button(addRect, defaults.iconToolbarPlus, defaults.preButton))
                    {
                        //defaults.DoAddButton(list);
                        list.onAddCallback(list);
                    }
                }

                using (new EditorGUI.DisabledScope(list.index < 0 || list.index >= list.count || (list.onCanRemoveCallback != null && !list.onCanRemoveCallback(list))))
                {
                    if (GUI.Button(removeRect, defaults.iconToolbarMinus, defaults.preButton))
                    {
                        defaults.DoRemoveButton(list);
                        //list.onRemoveCallback(list);
                    }
                }

                using (new EditorGUI.DisabledScope(list.count <= 0))
                {
                    Texture icon = EditorGUIUtility.IconContent("TreeEditor.Trash").image;
                    if (GUI.Button(trashRect, new GUIContent(icon), defaults.preButton))
                    {
                        TrashOpElementList(list);
                    }
                }

                using (new EditorGUI.DisabledScope(list.count <= 0))
                {
                    Texture icon = EditorGUIUtility.IconContent("TreeEditor.Refresh").image;
                    if (GUI.Button(refreshRect, new GUIContent(icon), defaults.preButton))
                    {
                        RefreshOpElementList(list);
                    }
                }
            };

            //重写原因（相对ReorderableList源码中的默认实现DoAddButton）：
            //1、去掉元素为 IList时，实际类型不明的丑陋构造方式，只需将list长度自增即可。
            //2、新增的元素，需要清空。
            reorderableList.onAddCallback = (ReorderableList list) =>
            {
                SerializedProperty listSP = list.serializedProperty;
                listSP.arraySize++;
                list.index = listSP.arraySize - 1;

                SerializedProperty newElementSP = listSP.GetArrayElementAtIndex(listSP.arraySize - 1);
                SerializedProperty targetSP = newElementSP.FindPropertyRelative("m_Target");
                targetSP.objectReferenceValue = null;
                newElementSP.serializedObject.ApplyModifiedProperties();
            };

            return reorderableList;
        }

        private void TrashOpElementList(ReorderableList list)
        {
            SerializedProperty listSP = list.serializedProperty;
            listSP.ClearArray();

            listSP.serializedObject.ApplyModifiedProperties();
        }

        private void RefreshOpElementList(ReorderableList list)
        {
            SerializedProperty listSP = list.serializedProperty;

            //merge components From I to J.
            //将 在I中且不在J中的component加入J，然后将I的Target置为Null。
            for (int i = 1; i < listSP.arraySize; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    SerializedProperty elementSP_I = listSP.GetArrayElementAtIndex(i);
                    SerializedProperty elementSP_J = listSP.GetArrayElementAtIndex(j);

                    SerializedProperty targetSP_I = elementSP_I.FindPropertyRelative("m_Target");
                    SerializedProperty targetSP_J = elementSP_J.FindPropertyRelative("m_Target");

                    if (targetSP_I.objectReferenceValue == null) { continue; }
                    if (targetSP_J.objectReferenceValue == null) { continue; }

                    if (!targetSP_I.objectReferenceValue.Equals(targetSP_J.objectReferenceValue)) { continue; }

                    SerializedProperty componentListSP_I = elementSP_I.FindPropertyRelative("m_ComponentList");
                    SerializedProperty componentListSP_J = elementSP_J.FindPropertyRelative("m_ComponentList");

                    for (int m = 0; m < componentListSP_I.arraySize; m++)
                    {
                        bool isExistInJ = false;
                        SerializedProperty componentSP_IM = componentListSP_I.GetArrayElementAtIndex(m);
                        for (int n = 0; n < componentListSP_J.arraySize; n++)
                        {
                            SerializedProperty componentSP_JN = componentListSP_J.GetArrayElementAtIndex(n);
                            if (componentSP_IM.objectReferenceValue.Equals(componentSP_JN.objectReferenceValue))
                            {
                                isExistInJ = true;
                                break;
                            }
                        }
                        if (!isExistInJ)
                        {
                            componentListSP_J.InsertArrayElementAtIndex(componentListSP_J.arraySize);
                            componentListSP_J.GetArrayElementAtIndex(componentListSP_J.arraySize - 1).objectReferenceValue = componentSP_IM.objectReferenceValue;
                        }
                    }
                    targetSP_I.objectReferenceValue = null;
                }
            }

            //移除所有target为Null 或 componentList 为空的元素
            for (int i = listSP.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty elementSP = listSP.GetArrayElementAtIndex(i);
                SerializedProperty targetSP = elementSP.FindPropertyRelative("m_Target");
                SerializedProperty componentListSP = elementSP.FindPropertyRelative("m_ComponentList");

                if (targetSP.objectReferenceValue == null || componentListSP.arraySize == 0)
                {
                    //注意：这里删除直接DeleteArrayElementAtIndex即可。（不要先置为null，会报错）
                    listSP.DeleteArrayElementAtIndex(i);
                }
            }

            listSP.serializedObject.ApplyModifiedProperties();
        }

        #region 代码生成相关
        private string GetPrefabPath()
        {
            // 如果正确拿到预设所在路径？
            PrefabAssetType singlePrefabType = PrefabUtility.GetPrefabAssetType(target);
            PrefabInstanceStatus singleInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(target);
            string targetAssetPath = AssetDatabase.GetAssetPath(target);
            string prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

            //Debug.Log("singlePrefabType: " + singlePrefabType);
            //Debug.Log("singleInstanceStatus: " + singleInstanceStatus);
            //Debug.Log("targetAssetPath: " + targetAssetPath);
            //Debug.Log("prefabAssetPath: " + prefabAssetPath);
            //Debug.Log("prefabStage: " + prefabStage);

            //1、点击预设时:
            //      singlePrefabType: Regular;
            //      singleInstanceStatus: NotAPrefab
            //      targetAssetPath: 可正确拿到
            //      prefabAssetPath: 可正确拿到
            //      prefabStage: Null

            //2、双击预设并在Hierarchy上选择时:
            //      singlePrefabType: NotAPrefab;    
            //      singleInstanceStatus: NotAPrefab
            //      targetAssetPath: "" (空字符串)
            //      prefabAssetPath: "" (空字符串)
            //      prefabStage: 可正确拿到

            //3、预设拖入Hierarchy并选择时:
            //      singlePrefabType: Regular;
            //      singleInstanceStatus: Connected
            //      targetAssetPath: "" (空字符串)
            //      prefabAssetPath: 可正确拿到
            //      prefabStage: Null

            // 需要覆盖并正确判断这三种情况。
            string finalPrefabPath = null;
            if (singlePrefabType == PrefabAssetType.Regular && !string.IsNullOrEmpty(targetAssetPath))
            {
                finalPrefabPath = targetAssetPath;   //点击预设时
            }
            else if (singlePrefabType == PrefabAssetType.Regular && !string.IsNullOrEmpty(prefabAssetPath))
            {
                finalPrefabPath = prefabAssetPath;  //预设拖入Hierarchy并选择时
            }
            else if (prefabStage != null)
            {
                finalPrefabPath = prefabStage.assetPath; //双击预设并在Hierarchy上选择时
            }

            return finalPrefabPath;
        }

        // 预制体名含空格会让生成的类名编译报错。弹窗确认后把空格换成下划线（预制体一起 RenameAsset），返回新路径；取消/冲突/失败返回 null
        private string EnsurePrefabNameNoSpace(string prefabPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            if (!fileName.Contains(" ")) return prefabPath;   // 没空格，原样返回

            string newName = fileName.Replace(" ", "_");
            string dir = Path.GetDirectoryName(prefabPath).Replace("\\", "/");
            string newPath = dir + "/" + newName + ".prefab";

            // 目标名已存在 → 冲突，交给用户手动处理，不覆盖
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(newPath)))
            {
                EditorUtility.DisplayDialog("改名冲突",
                    "预制体名带空格，需要改成 “" + newName + "”，但同目录已存在同名预制体。\n请先手动处理后再导出。", "知道了");
                return null;
            }

            bool ok = EditorUtility.DisplayDialog("预制体名带空格",
                "预制体 “" + fileName + "” 含空格，导出的脚本类名会编译报错。\n\n" +
                "将自动把空格换成下划线，改名为 “" + newName + "”\n（预制体和生成的脚本都用新名）。",
                "改名并继续", "取消");
            if (!ok) return null;

            string err = AssetDatabase.RenameAsset(prefabPath, newName);
            if (!string.IsNullOrEmpty(err))
            {
                EditorUtility.DisplayDialog("改名失败", "重命名预制体失败：" + err, "知道了");
                return null;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UI导出] 预制体含空格，已自动改名：" + fileName + " → " + newName);
            return newPath;
        }

        private void GenerateUIBaseCode()
        {
            // 校验 EditorSetting 里的 Base 输出路径：为空则弹窗提示，不默默生成到怪位置
            if (EditorSetting.Instance == null || string.IsNullOrEmpty(EditorSetting.Instance.generatedBaseUIRootDir))
            {
                EditorUtility.DisplayDialog("导出失败", "没有有效的 Base 代码输出路径配置，生成失败。\n请在 EditorSetting 中检查并填写 Generated Base UI Root Dir。", "知道了");
                return;
            }

            string prefabPath = GetPrefabPath();

            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("非预设不可导出");
                return;
            }

            // 预制体名带空格 → 生成的类名会编译报错：弹窗确认后自动把空格换成 _（预制体一起改名），用新名继续
            prefabPath = EnsurePrefabNameNoSpace(prefabPath);
            if (string.IsNullOrEmpty(prefabPath)) return;

            string fullPrefabPath = Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath("Assets", prefabPath)));
            string fullRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.uiPrefabRootDir));

            //Debug.Log("fullPrefabPath: " + fullPrefabPath);
            //Debug.Log("uiPrefabRootDir: " + fullRootDir);

            if (!fullPrefabPath.StartsWith(fullRootDir))
            {
                EditorUtility.DisplayDialog("导出失败",
                    "预设不在配置的可导出根目录下，你写的配置和实际路径不一致（注意大小写）。\n\n" +
                    "配置根目录：" + fullRootDir + "\n" +
                    "当前预设：" + fullPrefabPath + "\n\n" +
                    "请到 EditorSetting 的 Ui Prefab Root Dir 改成一致后，再重新导出。", "知道了");
                return;
            }

            string subPath = Path.GetRelativePath(fullRootDir, fullPrefabPath);
            string className = Path.GetFileNameWithoutExtension(subPath);
            string subSavePath = Path.Combine(Path.GetDirectoryName(subPath), className + "Base.cs");
            string savePath = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.generatedBaseUIRootDir, subSavePath));

            string content = UIEditorUtility.kUIBaseCode.Replace("${ClassName}", className + "Base");
            content = content.Replace("${BaseClassName}", target is UIPanelBehaviour ? "UIPanel" : "UIWidget");

            string variantsDefineStr, bindCompsStr, bindEventsStr, unbindEventsStr, unbindCompsStr;
            int retCode = GetExportBaseCodeStrs(out variantsDefineStr, out bindCompsStr, out bindEventsStr, out unbindEventsStr, out unbindCompsStr);
            if (retCode < 0) { return; }

            content = content.Replace("${VariantsDefine}", variantsDefineStr + (!string.IsNullOrEmpty(variantsDefineStr) ? "\r" : string.Empty));
            content = content.Replace("${BindComps}", bindCompsStr);
            content = content.Replace("${BindEvents}", (!string.IsNullOrEmpty(bindEventsStr) ? "\r" : string.Empty) + bindEventsStr + "\r\t");
            content = content.Replace("${UnbindEvents}", unbindEventsStr);
            content = content.Replace("${UnbindComps}", (!string.IsNullOrEmpty(unbindEventsStr) ? "\r" : string.Empty) + unbindCompsStr + "\r\t");

            UIEditorUtility.GenerateCode(savePath, content);

            RecordExportedName("m_ExportedBaseName", className + "Base");   // 记录 Base 类名，供改名后定位

            Debug.Log("Export success!");
        }

        private void GenerateUITempCode()
        {
            // 校验 EditorSetting 里的 Temp 输出路径：为空则弹窗提示，不默默生成到怪位置
            if (EditorSetting.Instance == null || string.IsNullOrEmpty(EditorSetting.Instance.generatedTempUIRootDir))
            {
                EditorUtility.DisplayDialog("导出失败", "没有有效的 Temp 代码输出路径配置，生成失败。\n请在 EditorSetting 中检查并填写 Generated Temp UI Root Dir。", "知道了");
                return;
            }

            string prefabPath = GetPrefabPath();

            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("非预设不可导出");
                return;
            }

            // 预制体名带空格 → 生成的类名会编译报错：弹窗确认后自动把空格换成 _（预制体一起改名），用新名继续
            prefabPath = EnsurePrefabNameNoSpace(prefabPath);
            if (string.IsNullOrEmpty(prefabPath)) return;

            string fullPrefabPath = Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath("Assets", prefabPath)));
            string fullRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.uiPrefabRootDir));

            //Debug.Log("fullPrefabPath: " + fullPrefabPath);
            //Debug.Log("uiPrefabRootDir: " + fullRootDir);

            if (!fullPrefabPath.StartsWith(fullRootDir))
            {
                EditorUtility.DisplayDialog("导出失败",
                    "预设不在配置的可导出根目录下，你写的配置和实际路径不一致（注意大小写）。\n\n" +
                    "配置根目录：" + fullRootDir + "\n" +
                    "当前预设：" + fullPrefabPath + "\n\n" +
                    "请到 EditorSetting 的 Ui Prefab Root Dir 改成一致后，再重新导出。", "知道了");
                return;
            }

            string subPath = Path.GetRelativePath(fullRootDir, fullPrefabPath);
            string className = Path.GetFileNameWithoutExtension(subPath);
            string subSavePath = Path.Combine(Path.GetDirectoryName(subPath), className + "_Temp.cs");
            string savePath = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.generatedTempUIRootDir, subSavePath));

            string content = UIEditorUtility.kUITemporaryCode.Replace("${ClassName}", className + "_Temp");
            content = content.Replace("${BaseClassName}", className + "Base");
            content = content.Replace("${PanelLifeCycleCode}", target is UIPanelBehaviour ? UIEditorUtility.kPanelLifeCycleCode : "");
            content = content.Trim();

            UIEditorUtility.GenerateCode(savePath, content);

            RecordExportedName("m_ExportedTempName", className + "_Temp");   // 记录 Temp 类名，供改名后定位

            Debug.Log("Export success!");
        }

        // 把导出记录写进 prefab 上的组件（序列化字段），改名后仍能读到
        private void RecordExportedName(string propName, string value)
        {
            var sp = serializedObject.FindProperty(propName);
            if (sp == null) return;
            sp.stringValue = value;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        // 改名后：把记录里的旧 Base 脚本改名（文件名+类名）成当前预制体对应的新名，业务代码保留
        private void UpdateUIBaseName()
        {
            string oldClassName = ((UIViewBehaviour)target).exportedBaseName;
            if (string.IsNullOrEmpty(oldClassName)) { EditorUtility.DisplayDialog("无法刷新", "没有 Base 导出记录，请先 ExportBase。", "知道了"); return; }

            string prefabPath = GetPrefabPath();
            if (string.IsNullOrEmpty(prefabPath)) { Debug.LogError("非预设不可操作"); return; }
            string newClassName = Path.GetFileNameWithoutExtension(prefabPath) + "Base";
            if (oldClassName == newClassName) { EditorUtility.DisplayDialog("无需刷新", "Base 脚本名（" + oldClassName + "）已与预制体一致。", "知道了"); return; }

            RenameGeneratedScript(EditorSetting.Instance.generatedBaseUIRootDir, prefabPath, oldClassName, newClassName,
                new (string from, string to)[] { (oldClassName, newClassName) },
                () => RecordExportedName("m_ExportedBaseName", newClassName), "Base");
        }

        // 改名后：把记录里的旧 Temp 脚本改名成新名；同时把它继承的旧 Base 类名一并改掉，业务代码保留
        private void UpdateUITempName()
        {
            UIViewBehaviour vb = (UIViewBehaviour)target;
            string oldTempName = vb.exportedTempName;
            if (string.IsNullOrEmpty(oldTempName)) { EditorUtility.DisplayDialog("无法刷新", "没有 Temp 导出记录，请先 ExportTemp。", "知道了"); return; }

            string prefabPath = GetPrefabPath();
            if (string.IsNullOrEmpty(prefabPath)) { Debug.LogError("非预设不可操作"); return; }
            string baseName = Path.GetFileNameWithoutExtension(prefabPath);
            string newTempName = baseName + "_Temp";
            string newBaseName = baseName + "Base";
            string oldBaseName = string.IsNullOrEmpty(vb.exportedBaseName) ? oldTempName.Replace("_Temp", "Base") : vb.exportedBaseName;
            if (oldTempName == newTempName) { EditorUtility.DisplayDialog("无需刷新", "Temp 脚本名（" + oldTempName + "）已与预制体一致。", "知道了"); return; }

            RenameGeneratedScript(EditorSetting.Instance.generatedTempUIRootDir, prefabPath, oldTempName, newTempName,
                new (string from, string to)[] { (oldTempName, newTempName), (oldBaseName, newBaseName) },
                () => RecordExportedName("m_ExportedTempName", newTempName), "Temp");
        }

        // 通用：把 rootDir 下旧类名脚本改名成新类名（文件名 + 内容里的类名整词替换），业务代码保留
        private void RenameGeneratedScript(string rootDir, string prefabPath, string oldClassName, string newClassName, (string from, string to)[] replaces, Action onSuccess, string tag)
        {
            string fullPrefabPath = Path.GetFullPath(Path.Combine(Application.dataPath, Path.GetRelativePath("Assets", prefabPath)));
            string fullRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, EditorSetting.Instance.uiPrefabRootDir));
            if (!fullPrefabPath.StartsWith(fullRootDir)) { Debug.LogError("预设不在可导出的根目录中：" + fullRootDir); return; }
            string subDir = Path.GetDirectoryName(Path.GetRelativePath(fullRootDir, fullPrefabPath)) ?? "";

            string oldFull = Path.GetFullPath(Path.Combine(Application.dataPath, rootDir, subDir, oldClassName + ".cs"));
            string newFull = Path.GetFullPath(Path.Combine(Application.dataPath, rootDir, subDir, newClassName + ".cs"));
            string oldAssetPath = "Assets" + oldFull.Substring(Application.dataPath.Length).Replace("\\", "/");
            string newAssetPath = "Assets" + newFull.Substring(Application.dataPath.Length).Replace("\\", "/");

            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(oldAssetPath)))
            { EditorUtility.DisplayDialog("刷新失败", "找不到旧的 " + tag + " 脚本（记录名与实际对不上，可能已手动改过）：\n" + oldAssetPath, "知道了"); return; }
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(newAssetPath)))
            { EditorUtility.DisplayDialog("刷新失败", tag + " 目标名已存在，请先处理冲突：\n" + newAssetPath, "知道了"); return; }

            if (!EditorUtility.DisplayDialog("刷新 " + tag + " 脚本名",
                tag + " 脚本将改名并同步类名（手写业务代码保留）：\n\n" + oldClassName + "  →  " + newClassName, "确定", "取消"))
                return;

            string err = AssetDatabase.RenameAsset(oldAssetPath, newClassName);
            if (!string.IsNullOrEmpty(err)) { EditorUtility.DisplayDialog("改名失败", "重命名脚本失败：" + err, "知道了"); return; }

            // 关键：改名与改内容之间不要 Refresh —— 否则 .cs 改动可能触发编译/domain reload 打断执行，
            // 造成“文件名已改、内容里类名没改成”，反而编译报错。集中到最后刷新一次。
            string content = File.ReadAllText(newFull);
            foreach (var r in replaces)
                content = Regex.Replace(content, "\\b" + Regex.Escape(r.from) + "\\b", r.to);
            File.WriteAllText(newFull, content, Encoding.UTF8);

            onSuccess?.Invoke();   // 更新记录（在刷新前完成）

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();   // 最后统一刷新，触发编译（此时文件名与类名已一致）
            Debug.Log("[UI导出] " + tag + " 脚本已刷新名字：" + oldClassName + " → " + newClassName);
        }

        private int GetExportBaseCodeStrs(out string variantsDefineStr, out string bindCompsStr, out string bindEventsStr, out string unbindEventsStr, out string unbindCompsStr)
        {
            HashSet<string> canBindEventCompSet = new HashSet<string>()
            {
                "Button", "Toggle", "Dropdown", "InputField", "Slider", "Scrollbar", "ScrollRect",
                "TMP_Dropdown", "TMP_InputField",
            };

            string variantsDefineTempalte = "protected ${CompType} m_${GoName}_${CompName};";
            string bindCompsLine = "m_${GoName}_${CompName} = (${CompType})viewBehaviour.GetComponentByIndexs(${i}, ${j});";
            string bindEventsLine = "BindEvent(m_${GoName}_${CompName});";
            string unbindEventsLine = "UnbindEvent(m_${GoName}_${CompName});";
            string unbindCompsLine = "m_${GoName}_${CompName} = null;";

            UIViewBehaviour uwb = (UIViewBehaviour)target;

            StringBuilder vdsb = new StringBuilder();
            StringBuilder bcsb = new StringBuilder();
            StringBuilder besb = new StringBuilder();
            StringBuilder ubesb = new StringBuilder();
            StringBuilder ubcsb = new StringBuilder();

            Dictionary<string, string> goNameDict = new Dictionary<string, string>();

            for (int i = 0; i < uwb.opElementList.Count; i++)
            {
                UIOpElement opElement = uwb.opElementList[i];
                string formatedGoName = UIEditorUtility.GetFormatedGoName(opElement.target.name);

                //不允许重名
                if (goNameDict.ContainsKey(formatedGoName))
                {
                    Debug.LogError(string.Format("重复的GameObjectName: {0}、{1}", goNameDict[formatedGoName], opElement.target.name));

                    variantsDefineStr = string.Empty;
                    bindCompsStr = string.Empty;
                    bindEventsStr = string.Empty;
                    unbindEventsStr = string.Empty;
                    unbindCompsStr = string.Empty;

                    return -1;
                }

                for (int j = 0; j < opElement.componentList.Count; j++)
                {
                    Component comp = opElement.componentList[j];
                    string compType = comp.GetType().Name;
                    string shortCompName = UIEditorUtility.GetCompShortName(compType);

                    string vdLine = new string(variantsDefineTempalte);
                    vdLine = vdLine.Replace("${CompType}", compType);
                    vdLine = vdLine.Replace("${GoName}", formatedGoName);
                    vdLine = vdLine.Replace("${CompName}", shortCompName);
                    vdsb.Append("\r\t").Append(vdLine);

                    string bcLine = new string(bindCompsLine);
                    bcLine = bcLine.Replace("${CompType}", compType);
                    bcLine = bcLine.Replace("${GoName}", formatedGoName);
                    bcLine = bcLine.Replace("${CompName}", shortCompName);
                    bcLine = bcLine.Replace("${i}", i.ToString());
                    bcLine = bcLine.Replace("${j}", j.ToString());
                    bcsb.Append("\r\t\t").Append(bcLine);

                    if (canBindEventCompSet.Contains(compType))
                    {
                        string beLine = new string(bindEventsLine);
                        beLine = beLine.Replace("${GoName}", formatedGoName);
                        beLine = beLine.Replace("${CompName}", shortCompName);
                        besb.Append("\r\t\t").Append(beLine);

                        string ubeLine = new string(unbindEventsLine);
                        ubeLine = ubeLine.Replace("${GoName}", formatedGoName);
                        ubeLine = ubeLine.Replace("${CompName}", shortCompName);
                        ubesb.Append("\r\t\t").Append(ubeLine); ;
                    }

                    string ubcLine = new string(unbindCompsLine);
                    ubcLine = ubcLine.Replace("${GoName}", formatedGoName);
                    ubcLine = ubcLine.Replace("${CompName}", shortCompName);
                    ubcsb.Append("\r\t\t").Append(ubcLine);
                }
            }

            variantsDefineStr = vdsb.ToString();
            bindCompsStr = bcsb.ToString();
            bindEventsStr = besb.ToString();
            unbindEventsStr = ubesb.ToString();
            unbindCompsStr = ubcsb.ToString();

            return 0;
        }
        #endregion
    }
}
