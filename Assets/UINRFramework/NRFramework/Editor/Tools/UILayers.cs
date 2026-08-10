using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NRFramework
{
    /// <summary>
    /// 【编辑器侧层级数据源】UI 管理器所有"层级"信息都走这里 —— 读 UILayerConfig SO；
    /// 没配 SO 就退回 UILayerConfig.Default() 的 12 层（与原 enum 完全一致）。
    /// 这样支持开发者在 SO 里增/减/改层，UI 管理器"一键刷新"后按这份数据重排。
    /// 下标 = 层级 id（与 Game 运行时的 uiRoots[id]、生成的 xxx_UIlayer 对齐）。
    /// </summary>
    public static class UILayers
    {
        // 缓存的层级列表 —— 只在首次访问 / 手动 Reload() 时读 SO，绝不每帧读
        // （否则 OnGUI 一帧几百次 AssetDatabase.FindAssets，面板直接卡死）。契合"手动刷新"设计。
        private static List<UILayerConfig.Layer> _cache;

        /// <summary>当前生效的层级列表（读缓存）。首次自动加载；改了 SO 后需手动 Reload() 才刷新。</summary>
        public static List<UILayerConfig.Layer> Layers()
        {
            if (_cache == null) Reload();
            return _cache;
        }

        /// <summary>重新从 SO 读层级（SO 优先，否则默认 12 层）。打开 UI 管理器 / 点"刷新层级"时调一次。</summary>
        public static void Reload()
        {
            var guids = AssetDatabase.FindAssets("t:UILayerConfig");
            if (guids != null && guids.Length > 0)
            {
                var so = AssetDatabase.LoadAssetAtPath<UILayerConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (so != null && so.layers != null && so.layers.Count > 0) { _cache = so.layers; return; }
            }
            _cache = UILayerConfig.Default();
        }

        public static int Count => Layers().Count;

        /// <summary>英文层名（= UIRoot 去掉下划线的名，也用于生成 enum 成员名）。</summary>
        public static string EngName(int i)
        {
            var l = Layers();
            return (i >= 0 && i < l.Count && !string.IsNullOrEmpty(l[i].name)) ? l[i].name : "Layer" + i;
        }

        /// <summary>中文显示名（SO 的 displayName，空则退英文名）。</summary>
        public static string DisplayName(int i)
        {
            var l = Layers();
            return (i >= 0 && i < l.Count && !string.IsNullOrEmpty(l[i].displayName)) ? l[i].displayName : EngName(i);
        }

        /// <summary>层级颜色（SO 的 color）。</summary>
        public static Color LayerColor(int i)
        {
            var l = Layers();
            return (i >= 0 && i < l.Count) ? l[i].color : new Color(0.55f, 0.58f, 0.62f);
        }

        /// <summary>默认层的下标：优先名为 "MainLayer" 的，找不到则 0。DetectLayer 猜不中时兜底用。</summary>
        public static int DefaultIndex()
        {
            var l = Layers();
            for (int i = 0; i < l.Count; i++) if (l[i].name == "MainLayer") return i;
            return 0;
        }

        /// <summary>按英文层名精确找下标；找不到(如该层已被开发者删掉)回默认层。DetectUILayer 用。</summary>
        public static int IndexOf(string engName)
        {
            var l = Layers();
            for (int i = 0; i < l.Count; i++) if (l[i].name == engName) return i;
            return DefaultIndex();
        }

        /// <summary>按预制体名（小写）猜层级 → 返回下标；命中任一层名即算，猜不中回默认层。</summary>
        public static int Detect(string lowerName)
        {
            if (string.IsNullOrEmpty(lowerName)) return DefaultIndex();
            var l = Layers();
            for (int i = 0; i < l.Count; i++)
                if (!string.IsNullOrEmpty(l[i].name) && lowerName.Contains(l[i].name.ToLower())) return i;
            return DefaultIndex();
        }
    }
}
