using System.Collections.Generic;
using UnityEngine;

namespace NRFramework
{
    public class Game : SingletonNRT<Game>
    {
        public Dictionary<int, UIRoots> uiRoots = new Dictionary<int, UIRoots>();
        public class UIRoots
        {
            public string LayerName;
            public GameObject obj;
            public UIRoot uI;
        }

        private Game() { }

        /// <summary>
        /// 初始化 UI 框架启动项。层级从 UILayerConfig 读（使用方某个 Resources/ 下的 UILayerConfig.asset）；
        /// 没配置就用内置默认 12 层（与原框架完全一致）。加/改层只改那份 SO，这里不用动。
        /// </summary>
        public void Init()
        {
            var config = Resources.Load<UILayerConfig>("UILayerConfig");
            var layers = (config != null && config.layers != null && config.layers.Count > 0)
                         ? config.layers
                         : UILayerConfig.Default();

            for (int i = 0; i < layers.Count; i++)
            {
                var L = layers[i];

                // ⚠ 保持原框架的两套名：UIRoot 的 rootId 带下划线("_WorldScene")、layer GameObject 名不带("WorldScene")
                UIRoot root = UIManager.Instance.CreateRoot("_" + L.name, L.startOrder, L.endOrder);

                var obj = new GameObject(L.name);
                obj.transform.SetParent(UIManager.Instance.uiCanvas.transform);
                obj.transform.SetSiblingIndex(i);
                obj.transform.localPosition = new Vector3(0, 0, 0);
                obj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                RectTransform rectTransform = obj.GetComponent<RectTransform>();
                if (rectTransform == null) rectTransform = obj.AddComponent<RectTransform>();
                // 全屏拉伸适配（照搬原 Init）
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.localPosition = Vector3.zero;

                uiRoots.Add(i, new UIRoots { LayerName = L.name, obj = obj, uI = root });
            }

            Debug.Log($"<color=#FFFB04>--->初始化 UI 框架成功（{layers.Count} 层）<---</color>");
        }
    }
}
