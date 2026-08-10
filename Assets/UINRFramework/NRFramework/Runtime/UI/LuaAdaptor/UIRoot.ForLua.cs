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
            Debug.Assert(!panelDict.ContainsKey(panelId));  //panel已存在

            UIPanelLuaCommon panel = new UIPanelLuaCommon();
            // 原来第二参传的是 this(UIRoot) 给 Canvas 形参、类型不符（USE_LUA 下才编译、老隐患）；这里改传 uiCanvas
            panel.CreateAsync(panelId, UIManager.Instance.uiCanvas, prefabPath, luaPanel, (ok) =>
            {
                if (!ok) { onCreated?.Invoke(false, null); return; }
                int targetSortingOrder = GetIncrementedSortingOrder();
                panel.SetSortingOrder(targetSortingOrder);
                int targetSiblingIndex = GetCurrentSiblingIndex(targetSortingOrder);
                panel.SetSiblingIndex(targetSiblingIndex);
                UIManager.Instance.SetBackgroundAndFocus();
                onCreated?.Invoke(true, panel);
            });
        }
    }
}
# endif

