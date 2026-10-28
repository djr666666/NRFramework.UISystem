// https://github.com/NRatel/NRFramework.UI

# if USE_LUA
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace NRFramework
{
    public partial class UIRoot
    {
        public void CreatePanelAsync(string panelId, string prefabPath, LuaTable luaPanel, Action<bool, UIPanelLuaCommon> onCreated = null)
        {
            // 判重 + 占位，与 C# 版 CreatePanelAsync 保持一致（否则加载途中再开会二次加载 + 稍后 panelDict.Add 抛 key 重复）
            if (panelStateDict.ContainsKey(panelId))
            {
                Debug.LogWarning($"[NRFramework] 面板 {panelId} 已在打开/加载中，忽略重复的 CreatePanelAsync");
                onCreated?.Invoke(false, null);
                return;
            }
            panelStateDict[panelId] = UIPanelState.Loading;

            UIPanelLuaCommon panel = new UIPanelLuaCommon();
            // 原来第二参传的是 this(UIRoot) 给 Canvas 形参、类型不符（USE_LUA 下才编译、老隐患）；这里改传 uiCanvas
            panel.CreateAsync(panelId, UIManager.Instance.uiCanvas, prefabPath, luaPanel, (ok) =>
            {
                // 结果核验：加载回来时占位已不是 Loading（被 Close/Destroy 取消）→ 丢弃防幽灵
                if (!panelStateDict.TryGetValue(panelId, out var st) || st != UIPanelState.Loading)
                {
                    if (ok) panel.Destroy();
                    onCreated?.Invoke(false, null);
                    return;
                }
                if (!ok)
                {
                    panelStateDict.Remove(panelId);   // 加载失败，回滚占位
                    onCreated?.Invoke(false, null);
                    return;
                }
                int targetSortingOrder = GetIncrementedSortingOrder();
                panel.SetSortingOrder(targetSortingOrder);
                int targetSiblingIndex = GetCurrentSiblingIndex(targetSortingOrder);
                panel.SetSiblingIndex(targetSiblingIndex);
                panelDict.Add(panel.panelId, panel);              // ★ 之前漏登记：不入 panelDict → 关不掉/查不到/焦点背景看不见
                panelStateDict[panelId] = UIPanelState.Show;      // 加载中 → 已显示
                UIManager.Instance.SetBackgroundAndFocus();
                onCreated?.Invoke(true, panel);
            });
        }
    }
}
# endif

