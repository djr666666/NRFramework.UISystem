// https://github.com/NRatel/NRFramework.UI

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NRFramework
{
    /// <summary>
    /// 用来管理 界面的打开和关闭   常用的就是  creat  和  close 可以在外头封装一层 
    /// UIRoot 提供 Panel 的创建、关闭、销毁、设置显隐等接口，并对当前层的层级进行管理
    /// 
    /// </summary>

    public partial class UIRoot
    {
        public string rootId;
        public int startOrder;
        public int endOrder;

        public Dictionary<string, UIPanel> panelDict { private set; get; }
        public Dictionary<string, UIPanelState> panelStateDict { get; private set; }

        //每次异步加载发一个流水号，用来在回调里认出「这是不是最后一次发起的加载」
        private Dictionary<string, int> m_LoadTokenDict;
        private int m_NextLoadToken;

        public UIRoot()
        {
            panelDict = new Dictionary<string, UIPanel>();
            panelStateDict = new Dictionary<string, UIPanelState>();
            m_LoadTokenDict = new Dictionary<string, int>();
        }

        public void CreatePanelAsync<T>(string panelId, string prefabPath, int sortingOrder, Action<bool, T> onCreated) where T : UIPanel
        {
            // 层级溢出：本层的 order 区间被占满了（同时开的面板太多，或某些面板 thickness 给得过大）。
            // 这里只能警告不能拦截——拦了界面就打不开。用 LogError 而不是 Debug.Assert，后者在 Release 包里会被剥离，
            // 真机上面板会悄悄串到相邻层级之上/之下，表现为「某个界面莫名其妙被别的界面盖住」，极难排查。
            if (sortingOrder < startOrder || sortingOrder > endOrder)
            {
                Debug.LogError($"[NRFramework] 层级溢出：root \"{rootId}\" 的区间是 [{startOrder}, {endOrder}]，" +
                               $"面板 {panelId} 算出来的 sortingOrder = {sortingOrder}，已经超出去了。\n" +
                               $"请调大该层区间（UILayerConfig），或调小面板的 Thickness。");
            }

            // 【加载期判重·对策①】已有占位（加载中 或 已打开 或 已隐藏）→ 不重复发起。
            // 真异步(YooAsset)下若不拦，加载途中再开同一个会二次加载 + 稍后第二次 panelDict.Add 抛 key 重复。
            if (panelStateDict.TryGetValue(panelId, out UIPanelState existState))
            {
                // 按真实状态给提示。尤其是 Hidden：面板其实还活着，只是被 SetPanelVisible(false) 藏了，
                // 此时该调 SetPanelVisible(true) 把它放出来，而不是再 Open 一次。
                string reason = existState switch
                {
                    UIPanelState.Loading => "正在异步加载中",
                    UIPanelState.Hidden => "已存在但被隐藏了，请改用 SetPanelVisible(true) 显示，而不是重新打开",
                    _ => "已经打开了",
                };
                Debug.LogWarning($"[NRFramework] 面板 {panelId} {reason}，忽略本次 CreatePanelAsync");
                onCreated?.Invoke(false, null);
                return;
            }
            // 占位为“加载中”（还没进 panelDict）。加载期间被 Close/Destroy 会撤掉这个占位 = 取消。
            panelStateDict[panelId] = UIPanelState.Loading;

            // 本次加载的流水号。只靠 panelStateDict 的状态判不出「是哪一次加载回来了」：
            // 加载中被 Close（撤占位）后立刻再 Open（重新占位 Loading），先回来的第一次会误以为自己还有效，
            // 抢先占住 panelDict，而真正发起第二次打开的调用方反而收到失败回调。带上流水号才能认准身份。
            int token = m_NextLoadToken++;
            m_LoadTokenDict[panelId] = token;

            T panel = Activator.CreateInstance(typeof(T)) as T;
            panel.CreateAsync(panelId, this, prefabPath, (ok) =>
            {
                // 【结果核验·对策②】加载回来时占位已不是 Loading（被 Close/Destroy 取消/清掉）、
                // 或者流水号已经被更晚的一次加载顶掉 → 结果作废，丢弃防幽灵。
                bool isLatest = m_LoadTokenDict.TryGetValue(panelId, out int curToken) && curToken == token;
                if (!isLatest || !panelStateDict.TryGetValue(panelId, out var st) || st != UIPanelState.Loading)
                {
                    if (ok) panel.Destroy();   // 销毁刚加载出来的实例，别泄漏
                    onCreated?.Invoke(false, null);
                    return;
                }
                if (!ok)
                {
                    panelStateDict.Remove(panelId);   // 加载失败，回滚占位
                    m_LoadTokenDict.Remove(panelId);
                    onCreated?.Invoke(false, null);
                    return;
                }
                panel.SetSortingOrder(sortingOrder);
                int siblingIndex = GetCurrentSiblingIndex(sortingOrder);
                panel.SetSiblingIndex(siblingIndex);
                panelDict.Add(panel.panelId, panel);
                panelStateDict[panelId] = UIPanelState.Show;   // 加载中 → 已显示

                UIManager.Instance.SetBackgroundAndFocus();

                onCreated?.Invoke(true, panel);
            });
        }

        public void CreatePanelAsync<T>(string panelId, string prefabPath, Action<bool, T> onCreated) where T : UIPanel
        {
            CreatePanelAsync<T>(panelId, prefabPath, GetIncrementedSortingOrder(), onCreated);
        }

        public void CreatePanelAsync<T>(string prefabPath, int sortingOrder, Action<bool, T> onCreated) where T : UIPanel
        {
            CreatePanelAsync<T>(typeof(T).Name, prefabPath, sortingOrder, onCreated);
        }

        public void CreatePanelAsync<T>(string prefabPath, Action<bool, T> onCreated) where T : UIPanel
        {
            CreatePanelAsync<T>(typeof(T).Name, prefabPath, onCreated);
        }

        public void ClosePanel(string panelId, Action onFinish = null)
        {
            // 【加载期取消·对策②】面板还在异步加载中（占位在、但实例还没进 panelDict）→ 撤占位当作取消，
            // 加载回来时上面的“结果核验”会把它丢弃；不能往下走 panelDict[panelId]（会 KeyNotFound）。
            if (!panelDict.ContainsKey(panelId))
            {
                if (panelStateDict.Remove(panelId))
                    Debug.Log($"[NRFramework] 面板 {panelId} 尚在加载中就被关闭，已标记取消");
                m_LoadTokenDict.Remove(panelId);
                onFinish?.Invoke();
                return;
            }

            UIPanel panel = panelDict[panelId];
            panelDict.Remove(panelId);
            panelStateDict.Remove(panelId);   // 关闭 = 面板不再存在，必须清占位；否则再开会被 CreatePanelAsync 当重复打开
            m_LoadTokenDict.Remove(panelId);
            panel.Close(onFinish);

            UIManager.Instance.SetBackgroundAndFocus();
        }

        public void ClosePanel<T>(Action onFinish = null) where T : UIPanel
        {
            ClosePanel(typeof(T).Name, onFinish);
        }

        public void DestroyPanel(string panelId)
        {
            // 【加载期取消·对策②】同 ClosePanel：还在加载中就被销毁 → 撤占位当取消，加载回来会被丢弃。
            if (!panelDict.ContainsKey(panelId))
            {
                if (panelStateDict.Remove(panelId))
                    Debug.Log($"[NRFramework] 面板 {panelId} 尚在加载中就被销毁，已标记取消");
                m_LoadTokenDict.Remove(panelId);
                return;
            }

            UIPanel panel = panelDict[panelId];
            panelDict.Remove(panelId);
            panelStateDict.Remove(panelId);   // 销毁 = 面板不再存在，必须清占位；同 ClosePanel
            m_LoadTokenDict.Remove(panelId);
            panel.Destroy();

            UIManager.Instance.SetBackgroundAndFocus();
        }

        public void DestroyPanel<T>() where T : UIPanel
        {
            DestroyPanel(typeof(T).Name);
        }

        public void SetPanelVisible(string panelId, bool visible)
        {
           

            //foreach (var key in panelDict.Keys)
            //{
            //    Debug.Log($"报错？？？？？  查看 panelDict 的 key = {key}  ");
            //}
            //Debug.Log($"SetPanelVisible 这个方法 panelId ={panelId}");


            // 不能只靠 Debug.Assert：它在 Release 包里会被剥离，之后 panelDict[panelId] 直接抛 KeyNotFoundException。
            // 面板已关闭 / 还在异步加载中时调用显隐是常见情况，这里安全返回并给出可定位的日志。
            if (!panelDict.TryGetValue(panelId, out UIPanel panel))
            {
                Debug.LogWarning($"[NRFramework] SetPanelVisible 失败：面板 {panelId} 不存在（已关闭，或仍在异步加载中）");
                return;
            }

            panel.SetVisible(visible);
            if (visible)
                panelStateDict[panelId] = UIPanelState.Show;
            else
                panelStateDict[panelId] = UIPanelState.Hidden;
            UIManager.Instance.SetBackgroundAndFocus();
        }

        public void SetPanelVisible<T>(bool visible) where T : UIPanel
        {
            SetPanelVisible(typeof(T).Name, visible);
        }

        /// <summary>取面板实例；面板没打开时返回 null（不会抛异常，可以直接 ?. 用）。</summary>
        public UIPanel GetPanel(string panelId)
        {
            //原来写的是 panelDict[panelId]，面板没开就抛 KeyNotFoundException。
            //取不到是很常见的情况（界面已关、还在加载），返回 null 让调用方自己判更合理。
            return panelDict.TryGetValue(panelId, out UIPanel panel) ? panel : null;
        }

        public T GetPanel<T>(string panelId) where T : UIPanel
        {
            return GetPanel(panelId) as T;
        }

        public T GetPanel<T>() where T : UIPanel
        {
            return GetPanel(typeof(T).Name) as T;
        }

        public bool ExistPanel(string panelId)
        {
            return panelDict.ContainsKey(panelId);
        }

        public bool ExistPanel<T>()
        {
            return ExistPanel(typeof(T).Name);
        }

        public bool IsPanelOpened<T>() where T : UIPanel
        {
            var id = typeof(T).Name;
            return panelStateDict.TryGetValue(id, out var state)&& state == UIPanelState.Show;
        }
        public int FindPanelComponent<T>(string panelId, string compDefine, out T comp) where T : Component
        {
            comp = null;
            if (string.IsNullOrEmpty(panelId)) { return FindCompErrorCode.PANEL_ID_IS_NULL_OR_EMPTY; }
            if (!ExistPanel(panelId)) { return FindCompErrorCode.NOT_EXIST_THIS_PANEL; }   //漏了这句的话 GetPanel 会直接抛 KeyNotFoundException
            UIPanel panel = GetPanel(panelId);
            return panel.FindComponent<T>(compDefine, out comp);
        }

        public int FindWidgetComponent<T>(string panelId, string[] widgetIds, string compDefine, out T comp) where T : Component
        {
            comp = null;
            if (string.IsNullOrEmpty(panelId)) { return FindCompErrorCode.PANEL_ID_IS_NULL_OR_EMPTY; }
            if (!ExistPanel(panelId)) { return FindCompErrorCode.NOT_EXIST_THIS_PANEL; }
            UIPanel panel = GetPanel(panelId);
            return panel.FindWidgetComponent<T>(widgetIds, compDefine, out comp);
        }

        private int GetIncrementedSortingOrder()
        {
            UIPanel topestPanel = null;
            foreach (KeyValuePair<string, UIPanel> kvPair in panelDict)
            {
                UIPanel panel = kvPair.Value;
                if (topestPanel == null || panel.canvas.sortingOrder > topestPanel.canvas.sortingOrder)
                {
                    topestPanel = panel;
                }
            }

            return topestPanel != null ? (topestPanel.canvas.sortingOrder + topestPanel.panelBehaviour.thickness + 1) : startOrder;
        }

        private int GetCurrentSiblingIndex(int sortingOrder)
        {
            List<UIPanel> panels = UIManager.Instance.FilterPanels((panel) =>
            { return sortingOrder > panel.canvas.sortingOrder; });

            return panels.Count;
        }
    }
}
