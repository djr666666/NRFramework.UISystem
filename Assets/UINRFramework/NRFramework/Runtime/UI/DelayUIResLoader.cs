using System;
using System.Collections;
using UnityEngine;

namespace NRFramework
{
    /// <summary>
    /// 开发期测试用的「假异步」加载器：延迟若干帧再回调，模拟 YooAsset / Addressables 的真实时序。
    ///
    /// 为什么需要它：编辑器默认的 DefaultUIResLoader 是 AssetDatabase 同步加载 + 立即回调，
    /// 回调在 CreatePanelAsync 返回之前就跑完了。于是框架里整套异步保护
    /// （UIPanelState.Loading 占位、加载中被 Close 的取消、加载流水号核验）在它下面永远不会被触发，
    /// 相当于从没测过。接真实加载器之前，先换成这个把时序问题跑出来。
    ///
    /// 用法（在 Game.Instance.Init() 之前）：
    /// <code>UIRes.Loader = new DelayUIResLoader(5);</code>
    ///
    /// 只在编辑器下能取到资源，不要带进正式包。
    /// </summary>
    public class DelayUIResLoader : IUIResLoader
    {
        private readonly int m_DelayFrames;
        private readonly bool m_Verbose;

        public DelayUIResLoader(int delayFrames = 5, bool verbose = true)
        {
            m_DelayFrames = Mathf.Max(1, delayFrames);
            m_Verbose = verbose;
        }

        public void LoadPrefabAsync(string path, Action<GameObject> onLoaded)
        {
            if (m_Verbose) { Debug.Log($"<color=#9FD4FF>[DelayLoader] 开始加载 {path}，{m_DelayFrames} 帧后返回</color>"); }
            Runner.Instance.StartCoroutine(LoadRoutine(path, onLoaded));
        }

        private IEnumerator LoadRoutine(string path, Action<GameObject> onLoaded)
        {
            for (int i = 0; i < m_DelayFrames; i++) { yield return null; }

#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            GameObject prefab = null;
            Debug.LogError($"[NRFramework] DelayUIResLoader 只能在编辑器里用，正式包请换成真实加载器。path={path}");
#endif
            if (m_Verbose) { Debug.Log($"<color=#9FD4FF>[DelayLoader] 加载返回 {path}（prefab={(prefab == null ? "null" : prefab.name)}）</color>"); }
            onLoaded?.Invoke(prefab);
        }

        public void ReleasePrefab(string path) { }

        //协程需要一个 MonoBehaviour 宿主。加载器本身不是组件，这里自带一个常驻的空壳。
        private class Runner : MonoBehaviour
        {
            static private Runner sm_Instance;

            static public Runner Instance
            {
                get
                {
                    if (sm_Instance == null)
                    {
                        GameObject go = new GameObject("#DelayUIResLoader#");
                        DontDestroyOnLoad(go);
                        sm_Instance = go.AddComponent<Runner>();
                    }
                    return sm_Instance;
                }
            }
        }
    }
}
