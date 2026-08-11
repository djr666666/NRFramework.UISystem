// https://github.com/NRatel/NRFramework.UI

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NRFramework
{
    /// <summary>
    /// UIManager 中管理整体 Panel，包括背景处理、焦点管理、返回键回退逻辑。提供 创建 UIRoot 、Panel 筛选、组件反射查找（从root开始）等的接口。
    /// </summary>
    [MonoSingletonSetting(HideFlags.NotEditable, "#UIManager#")]
    public class UIManager : MonoSingletonNRT<UIManager>
    {
        public Canvas uiCanvas { private set; get; }

        public Camera uiCamera { private set; get; }

        public Dictionary<string, UIRoot> rootDict { private set; get; }

        private List<UIPanel> m_FocusingPanels;
        private List<UIPanel> m_TempNewFocusingPanels;


        private void Awake()
        {

            Debug.Log($"<color=#FFFB04>--->  Resources.Load GGame <---</color>");
            var go = Resources.Load<GameObject>(Config.GGame);
            if (go == null)
            {
                // 漏建 GGame 是最常见的接入错误。给明确指引，别让它烂在 Instantiate(null) 的隐晦报错里。
                Debug.LogError($"[NRFramework] 启动失败：Resources 里找不到启动预制体 \"{Config.GGame}\"。\n" +
                               $"请先用菜单【Tools ▸ NRFramework ▸ 创建 GGame】生成一份到 Assets/Resources/{Config.GGame}.prefab 再启动（见 README「GGame 约定」）。");
                return;
            }

            var obj = GameObject.Instantiate(go);
            DontDestroyOnLoad(obj);
            obj.name = Config.GGameName;
            obj.SetActive(true);
            obj.transform.localScale = new Vector3(1, 1, 1);
            obj.transform.localPosition = new Vector3(0, 0, 0);

            // 从 GGame 实例【自身子树】里按名找 UICanvas / UICamera —— 不用 GameObject.Find 全局搜：
            // 避免使用方场景里存在同名物体被误绑，也更快（框架本就持有这个实例）。
            var canvasTrans = FindChildByName(obj.transform, Config.kUICanvasPath);
            var cameraTrans = FindChildByName(obj.transform, Config.kUICameraPath);
            if (canvasTrans == null || cameraTrans == null)
            {
                Debug.LogError($"[NRFramework] 启动失败：启动预制体 \"{Config.GGame}\" 里没找到 " +
                               $"\"{Config.kUICanvasPath}\" 或 \"{Config.kUICameraPath}\" 子物体。\n" +
                               $"这两个名字是框架启动约定、不能改（见 Config.cs / README「GGame 约定」）。");
                return;
            }
            uiCanvas = canvasTrans.GetComponent<Canvas>();
            uiCamera = cameraTrans.GetComponent<Camera>();

            rootDict = new Dictionary<string, UIRoot>();
            m_FocusingPanels = new List<UIPanel>();
            m_TempNewFocusingPanels = new List<UIPanel>();
        }

        // 在指定根的子树里（含自身、含未激活）按名字找第一个 Transform，找不到返回 null。
        // 用于在 GGame 实例内定位 UICanvas / UICamera，替代全局 GameObject.Find（避免场景同名误绑、更快）。
        private static Transform FindChildByName(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name) return all[i];
            }
            return null;
        }


        public UIRoot CreateRoot(string rootId, int startOrder, int endOrder)
        {
            Debug.Assert(!rootDict.ContainsKey(rootId));    //uiRoot已存在
            Debug.Assert(startOrder >= 0);                  //必须使startOrder >= 0
            Debug.Assert(endOrder >= startOrder);           //必须使endOrder >= startOrder

            UIRoot uiRoot = new UIRoot() { rootId = rootId, startOrder = startOrder, endOrder = endOrder };
            rootDict.Add(rootId, uiRoot);

            return uiRoot;
        }

        public UIRoot GetRoot(string rootId)
        {
            return rootDict[rootId];
        }

        public bool ExistRoot(string rootId)
        {
            return rootDict.ContainsKey(rootId);
        }

        public List<UIPanel> FilterPanels(Func<UIPanel, bool> filterFunc = null)
        {
            List<UIPanel> panels = new List<UIPanel>();

            foreach (KeyValuePair<string, UIRoot> kvPair in rootDict)
            {
                foreach (KeyValuePair<string, UIPanel> kvPair2 in kvPair.Value.panelDict)
                {
                    if (filterFunc == null || filterFunc(kvPair2.Value))
                    {
                        panels.Add(kvPair2.Value);
                    }
                }
            }
            return panels;
        }

        public UIPanel FilterTopestPanel(Func<UIPanel, bool> filterFunc = null)
        {
            List<UIPanel> panels = FilterPanels(filterFunc);

            panels.Sort((a, b) => { return a.canvas.sortingOrder - b.canvas.sortingOrder; });

            return panels.Count > 0 ? panels[panels.Count - 1] : null;
        }

        public UIPanel GetTopestPanel()
        {
            return FilterTopestPanel((panel)=> { return panel.showState != UIPanelShowState.Hidden; });
        }

        public List<UIPanel> GetFocusingPanels()
        {
            List<UIPanel> vaildPanels = new List<UIPanel>();
            foreach (UIPanel panel in m_FocusingPanels)
            {
                if (panel != null) { vaildPanels.Add(panel); }
            }
            return vaildPanels;
        }

        public int FindPanelComponent<T>(string rootId, string panelId, string compDefine, out T comp) where T : Component
        {
            comp = null;
            if (!ExistRoot(rootId)) { return FindCompErrorCode.NOT_EXIST_THIS_ROOT; }

            UIRoot root = GetRoot(rootId);
            return root.FindPanelComponent<T>(panelId, compDefine, out comp);
        }

        public int FindWidgetComponent<T>(string rootId, string panelId, string[] widgetIds, string compDefine, out T comp) where T : Component
        {
            comp = null;
            if (!ExistRoot(rootId)) { return FindCompErrorCode.NOT_EXIST_THIS_ROOT; }

            UIRoot root = GetRoot(rootId);
            return root.FindWidgetComponent<T>(panelId, widgetIds, compDefine, out comp);
        }

        public int FindComponentByPath<T>(string path, string compDefine, out T comp) where T : Component
        {
            comp = null;
            if (string.IsNullOrEmpty(path)) { return FindCompErrorCode.VIEW_PATH_IS_NULL_OR_EMPTY; }
            string[] strs = path.Split("/");
            if (strs.Length < 2) { return FindCompErrorCode.VIEW_PATH_IS_TOO_SHORT; }
            string rootId = strs[0];
            string panelId = strs[1];

            if (strs.Length > 2)
            {
                string[] widgetIds = new string[strs.Length - 2];
                for (int i = 0; i < strs.Length - 2; i++)
                { widgetIds[i] = strs[i + 2]; }
                return FindWidgetComponent<T>(rootId, panelId, widgetIds, compDefine, out comp);
            }
            else
            {
                return FindPanelComponent<T>(rootId, panelId, compDefine, out comp);
            }
        }

        internal void SetBackgroundAndFocus()
        {
            List<UIPanel> panels = FilterPanels((panel) =>
            { return panel.showState != UIPanelShowState.Hidden; });
            panels.Sort((a, b) => { return a.canvas.sortingOrder - b.canvas.sortingOrder; });

            UIPanel needBgPanel = null;
            bool collectFocusCanBreak = false;

            for (int i = panels.Count - 1; i >= 0; i--)
            {
                UIPanel panel = panels[i];

                if (needBgPanel == null && panel.panelBehaviour.hasBg)
                {
                    needBgPanel = panel;
                }

                if (panel.panelBehaviour.getFocusType == UIPanelGetFocusType.GetWithOthers)
                {
                    m_TempNewFocusingPanels.Add(panel);
                }
                else if (panel.panelBehaviour.getFocusType == UIPanelGetFocusType.Get)
                {
                    m_TempNewFocusingPanels.Add(panel);
                    collectFocusCanBreak = true;
                }

                if (needBgPanel != null && collectFocusCanBreak) { break; }
            }

            //设置/移除背景
            if (needBgPanel != null) { needBgPanel.SetBackground(); }
            else { UIBlocker.Instance.Unbind(); }

            //丢失焦点时，由顶至下
            for (int i = m_FocusingPanels.Count - 1; i >= 0; i--)
            {
                UIPanel panel = m_FocusingPanels[i];
                if (panel.panelId != null && !m_TempNewFocusingPanels.Contains(panel))
                {
                    panel.SetFocus(false);
                }
            }

            //获得焦点时，由底至上
            for (int i = 0; i < m_TempNewFocusingPanels.Count; i++)
            {
                UIPanel panel = m_TempNewFocusingPanels[i];
                if (!m_FocusingPanels.Contains(panel))
                {
                    panel.SetFocus(true);
                }
            }

            List<UIPanel> t = m_FocusingPanels;
            m_FocusingPanels = m_TempNewFocusingPanels;
            m_TempNewFocusingPanels = t;
            m_TempNewFocusingPanels.Clear();
            t = null;
        }

        private void Update()
        {
            // ESC 返回：旧 Input API 仅在启用「旧输入(Input Manager)」时可用。
            // 纯 Input System 模式（Player ▸ Active Input Handling = Input System Package）下，
            // 这段不编译，避免 UnityEngine.Input 抛 InvalidOperationException。
            // 需要 ESC 请把 Active Input Handling 设为 Both；或接入 Input System 版检测（见类外说明）。
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIPanel topestPanel = FilterTopestPanel((panel) =>
                { return panel.showState != UIPanelShowState.Hidden && panel.panelBehaviour.escPressEventType != UIPanelEscPressEventType.DontCheck; });

                if (topestPanel == null) { return; }
                if (topestPanel.showState != UIPanelShowState.Idle) { return; }

                topestPanel.DoEscPress();
            }
#endif
        }
    }
}