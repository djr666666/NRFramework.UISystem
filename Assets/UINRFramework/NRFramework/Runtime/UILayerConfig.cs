using System.Collections.Generic;
using UnityEngine;

namespace NRFramework
{
    /// <summary>
    /// UI 层级配置（单一数据源）。放使用方某个 Resources/ 目录下、命名 UILayerConfig.asset；
    /// Game.Init 与 UI 管理器都读它；没配置则用内置 Default() 的 12 层（与原框架完全一致）。
    /// 约定：list 顺序 = 层级从低到高，【下标就是层级 id】（业务 uiRoots[id]、生成的 xxx_UIlayer 都用它）；
    ///       order 区间必须递增、不重叠。
    /// </summary>
    [CreateAssetMenu(menuName = "NRFramework/UILayerConfig", fileName = "UILayerConfig")]
    public class UILayerConfig : ScriptableObject
    {
        [System.Serializable]
        public class Layer
        {
            public string name;         // 层名（不带下划线，如 WorldScene）——UIRoot 会加 "_" 前缀
            public int startOrder;
            public int endOrder;
            public string displayName;  // 编辑器里显示的中文名（如 世界场景层）
            public Color color = new Color(0.55f, 0.58f, 0.62f);  // 编辑器色条 / chip 颜色
        }

        public List<Layer> layers = new List<Layer>();

#if UNITY_EDITOR
        // 新建的 SO 是空的 → 在 Inspector 右上角「⋮」菜单点这个，一键填入默认 12 层再改，省得手敲
        [ContextMenu("填入默认 12 层")]
        void FillDefaultLayers()
        {
            layers = Default();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        /// <summary>内置默认 12 层：名 / order / 中文名 / 颜色 全与原框架一致，保证不配 SO 时行为不变、层级 id 兼容。</summary>
        public static List<Layer> Default() => new List<Layer>
        {
            new Layer { name="WorldScene",   startOrder=0,   endOrder=49,  displayName="世界场景层", color=new Color(0.30f,0.72f,0.65f) },
            new Layer { name="WorldObject",  startOrder=50,  endOrder=99,  displayName="世界物体层", color=new Color(0.40f,0.60f,0.85f) },
            new Layer { name="WorldEffect",  startOrder=100, endOrder=149, displayName="世界特效层", color=new Color(0.55f,0.75f,0.40f) },
            new Layer { name="DragLayer",    startOrder=150, endOrder=199, displayName="拖拽层",     color=new Color(0.60f,0.60f,0.66f) },
            new Layer { name="MainLayer",    startOrder=200, endOrder=249, displayName="主界面层",   color=new Color(0.29f,0.62f,1.00f) },
            new Layer { name="ScreenLayer",  startOrder=250, endOrder=349, displayName="全屏界面层", color=new Color(0.18f,0.83f,0.75f) },
            new Layer { name="ModalLayer",   startOrder=350, endOrder=449, displayName="模态弹窗层", color=new Color(0.65f,0.55f,0.98f) },
            new Layer { name="PopLayer",     startOrder=450, endOrder=549, displayName="普通弹窗层", color=new Color(0.97f,0.47f,0.73f) },
            new Layer { name="GuideLayer",   startOrder=550, endOrder=649, displayName="新手引导层", color=new Color(0.22f,0.77f,0.81f) },
            new Layer { name="TopLayer",     startOrder=650, endOrder=749, displayName="顶层通知",   color=new Color(0.49f,0.55f,1.00f) },
            new Layer { name="LoadingLayer", startOrder=750, endOrder=849, displayName="加载界面层", color=new Color(0.94f,0.53f,0.24f) },
            new Layer { name="CursorLayer",  startOrder=850, endOrder=949, displayName="鼠标光标层", color=new Color(0.55f,0.58f,0.62f) },
        };
    }
}
