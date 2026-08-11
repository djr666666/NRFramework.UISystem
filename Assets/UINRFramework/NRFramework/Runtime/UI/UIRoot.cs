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
        
        public UIRoot()
        {
            panelDict = new Dictionary<string, UIPanel>();
            panelStateDict = new Dictionary<string, UIPanelState>();
        }

        public void CreatePanelAsync<T>(string panelId, string prefabPath, int sortingOrder, Action<bool, T> onCreated) where T : UIPanel
        {
            Debug.Assert(sortingOrder >= startOrder && sortingOrder <= endOrder);

            // 【加载期判重·对策①】已有占位（加载中 或 已打开）→ 不重复发起。
            // 真异步(YooAsset)下若不拦，加载途中再开同一个会二次加载 + 稍后第二次 panelDict.Add 抛 key 重复。
            if (panelStateDict.ContainsKey(panelId))
            {
                Debug.LogWarning($"[NRFramework] 面板 {panelId} 已在打开/加载中，忽略重复的 CreatePanelAsync");
                onCreated?.Invoke(false, null);
                return;
            }
            // 占位为“加载中”（还没进 panelDict）。加载期间被 Close/Destroy 会撤掉这个占位 = 取消。
            panelStateDict[panelId] = UIPanelState.Loading;

            T panel = Activator.CreateInstance(typeof(T)) as T;
            panel.CreateAsync(panelId, this, prefabPath, (ok) =>
            {
                // 【结果核验·对策②】加载回来时占位已不是 Loading（被 Close/Destroy 取消/清掉）→ 结果作废，丢弃防幽灵。
                if (!panelStateDict.TryGetValue(panelId, out var st) || st != UIPanelState.Loading)
                {
                    if (ok) panel.Destroy();   // 销毁刚加载出来的实例，别泄漏
                    onCreated?.Invoke(false, null);
                    return;
                }
                if (!ok)
                {
                    panelStateDict.Remove(panelId);   // 加载失败，回滚占位
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
                onFinish?.Invoke();
                return;
            }

            UIPanel panel = panelDict[panelId];
            panelDict.Remove(panelId);
            panelStateDict[panelId] = UIPanelState.Hidden;
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
                return;
            }

            UIPanel panel = panelDict[panelId];
            panelDict.Remove(panelId);
            panelStateDict[panelId] = UIPanelState.Hidden;
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


            Debug.Assert( panelDict.ContainsKey(panelId));

            UIPanel panel = panelDict[panelId];
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

        public UIPanel GetPanel(string panelId)
        {
            return panelDict[panelId];
        }

        public T GetPanel<T>(string panelId) where T : UIPanel
        {
            return panelDict[panelId] as T;
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
