using UnityEditor;
using UnityEngine;

namespace NRFramework
{
    /// <summary>
    /// 一键把包内的 GGame 模板拷到使用方项目的 Assets/Resources/GGame.prefab。
    /// GGame 是 UI 框架的启动预制体（含 UICanvas / UICamera），框架用 Resources.Load("GGame") 加载。
    /// 拷出来后就是使用方自己的资源，可随便改（拖加载界面 / 改结构 / 加子物体），
    /// 但别改 GGame / UICanvas / UICamera 三个名字（框架靠它们启动，见 Config.cs / README「GGame 约定」）。
    /// </summary>
    public static class GGameCreator
    {
        // 包内模板 NRFramework/Templates/GGame.prefab 的 GUID（与该 .meta 里的 guid 保持一致）
        private const string kTemplateGuid = "b2c4e6a8d0f2b4c6e8a0d2f4b6c8e0a2";
        private const string kTargetPath = "Assets/Resources/GGame.prefab";

        [MenuItem("Tools/NRFramework/创建 GGame（拷到 Assets/Resources）")]
        public static void CreateGGame()
        {
            // 已存在就不覆盖，避免冲掉使用方改过的 GGame
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(kTargetPath)))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(kTargetPath));
                EditorUtility.DisplayDialog("已存在",
                    "已经有 " + kTargetPath + " 了，不重复创建。\n要重建请先手动删掉它再点。", "知道了");
                return;
            }

            string templatePath = AssetDatabase.GUIDToAssetPath(kTemplateGuid);
            if (string.IsNullOrEmpty(templatePath))
            {
                EditorUtility.DisplayDialog("找不到模板",
                    "定位不到 GGame 模板（GUID " + kTemplateGuid + "）。\n请确认包内 NRFramework/Templates/GGame.prefab 存在。", "知道了");
                return;
            }

            // 确保 Assets/Resources 目录存在
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (AssetDatabase.CopyAsset(templatePath, kTargetPath))
            {
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(kTargetPath));
                EditorUtility.DisplayDialog("创建成功",
                    "已生成 " + kTargetPath + "\n\n这是你自己的 GGame，可随便改（拖加载界面 / 改结构 / 加子物体），\n但别改 GGame / UICanvas / UICamera 三个名字（框架靠它们启动）。", "好的");
            }
            else
            {
                EditorUtility.DisplayDialog("创建失败", "拷贝失败。\n源：" + templatePath + "\n目标：" + kTargetPath, "知道了");
            }
        }
    }
}
