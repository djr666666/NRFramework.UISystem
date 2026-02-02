
using System;
using System.Collections.Generic;
using UnityEngine;
using static NRFramework.UI.Game;

namespace NRFramework.UI
{
    public class Game : SingletonNRT<Game>
    {

        private UIRoot _WorldScene;
        private UIRoot _WorldObject;
        private UIRoot _WorldEffect;
        private UIRoot _DragLayer;
        private UIRoot _MainLayer;
        private UIRoot _ScreenLayer;
        private UIRoot _ModalLayer;
        private UIRoot _PopLayer;
        private UIRoot _GuideLayer;
        private UIRoot _TopLayer;
        private UIRoot _LoadingLayer;
        private UIRoot _CursorLayer;


        private enum PanelType
        {
            // ------------------------------------------------------------------第1部分：世界空间UI
            WorldScene = 0,        // 场景UI：地图标记、地面特效
            WorldObject = 1,       // 物体UI：血条、名字、NPC标识
            WorldEffect = 2,       // 特效UI：伤害数字、BUFF图标、交互提示
            // ------------------------------------------------------------------第2部分：特殊状态
            DragLayer = 3,         // 拖拽（比所有世界UI都低）
            // ------------------------------------------------------------------第3部分：屏幕空间UI
            MainLayer = 4,         // 主界面HUD
            ScreenLayer = 5,       // 全屏功能界面
            // ------------------------------------------------------------------第4部分：弹窗交互       
            ModalLayer = 6,        // 模态对话框
            PopLayer = 7,          // 普通弹窗
            // ------------------------------------------------------------------第5部分：提示引导
            GuideLayer = 8,        // 新手引导
            TopLayer = 9,          // 飘字提示/服务器公告
            // ------------------------------------------------------------------第6部分：系统通知
            LoadingLayer = 10,     // 加载界面
            // ------------------------------------------------------------------第7部分：顶级系统
            CursorLayer = 11,      // 鼠标/手势
        }

        List<UIRoot> lstuiRoots = new List<UIRoot>();
        public Dictionary<int, UIRoots> uiRoots = new Dictionary<int, UIRoots>();
        public class UIRoots
        {
            public string LayerName;
            public GameObject obj;
            public UIRoot uI;
        }

        private Game()
        {
            Debug.Log($"<color=#FFFB04>--->NRFramework.UI  Root<---</color>");
            _WorldScene = UIManager.Instance.CreateRoot("_WorldScene", 0, 49);
            _WorldObject = UIManager.Instance.CreateRoot("_WorldObject", 50, 99);
            _WorldEffect = UIManager.Instance.CreateRoot("_WorldEffect", 100, 149);
            _DragLayer = UIManager.Instance.CreateRoot("_DragLayer", 150, 199);
            _MainLayer = UIManager.Instance.CreateRoot("_MainLayer", 200, 249);

            _ScreenLayer = UIManager.Instance.CreateRoot("_ScreenLayer", 250, 349);
            _ModalLayer = UIManager.Instance.CreateRoot("_ModalLayer", 350, 449);
            _PopLayer = UIManager.Instance.CreateRoot("_PopLayer", 450, 549);
            _GuideLayer = UIManager.Instance.CreateRoot("_GuideLayer", 550, 649);
            _TopLayer = UIManager.Instance.CreateRoot("_TopLayer", 650, 749);
            _LoadingLayer = UIManager.Instance.CreateRoot("_LoadingLayer", 750, 849); ;
            _CursorLayer = UIManager.Instance.CreateRoot("_CursorLayer", 850, 949);

            lstuiRoots = new List<UIRoot>() {_WorldScene, _WorldObject,_WorldEffect,_DragLayer,_MainLayer,_ScreenLayer,_ModalLayer,_PopLayer,_GuideLayer,_TopLayer,_LoadingLayer,_CursorLayer,
            };
        }

        /// <summary>
        /// 其他项目 初始化 UI框架启动项（点  .Init 就会跑接下来所有的UI框架配置）
        /// </summary>
        public void Init()
        {
            int index = 0;
            foreach (PanelType type in Enum.GetValues(typeof(PanelType)))
            {
                int _index = index;
                UIRoots uIRoots = new UIRoots();
                uIRoots.LayerName = type.ToString();
                uIRoots.uI = lstuiRoots[_index];
                uiRoots.Add((int)type, uIRoots);
                index++;
            }
            //处理 obj
            foreach (var item in uiRoots)
            {
                var obj = new GameObject(item.Value.LayerName);

                obj.transform.SetParent(UIManager.Instance.uiCanvas.transform);
                obj.transform.SetSiblingIndex(item.Key);
                uiRoots[item.Key].obj = obj;
                uiRoots[item.Key].obj.transform.localPosition = new Vector3(0, 0, 0);
                uiRoots[item.Key].obj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                // 获取或添加RectTransform组件
                RectTransform rectTransform = uiRoots[item.Key].obj.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    rectTransform = uiRoots[item.Key].obj.AddComponent<RectTransform>();
                }

                // 设置RectTransform为全屏拉伸适配
                rectTransform.anchorMin = Vector2.zero;        // 锚点最小(0,0) - 左下角
                rectTransform.anchorMax = Vector2.one;         // 锚点最大(1,1) - 右上角
                rectTransform.offsetMin = Vector2.zero;        // 左下角偏移(0,0)
                rectTransform.offsetMax = Vector2.zero;        // 右上角偏移(0,0)
                rectTransform.pivot = new Vector2(0.5f, 0.5f); // 中心点(0.5,0.5)
                rectTransform.localPosition = Vector3.zero;    // 位置归零
            }
            Debug.Log($"<color=#FFFB04>--->初始化 UI 框架成功<---</color>");
        }

  

    }
}