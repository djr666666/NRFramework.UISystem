using System;
using UnityEngine;

namespace NRFramework
{
    /// <summary>
    /// UI 资源加载接口 —— 框架加载 panel/widget 预制体的唯一入口抽象。
    /// 默认走编辑器 AssetDatabase（见 DefaultUIResLoader）；要换 YooAsset / Addressables / Resources 时，
    /// 实现本接口 + 启动前设置 UIRes.Loader = 你的实现即可，【无需改框架源码】。
    /// </summary>
    public interface IUIResLoader
    {
        /// <summary>按路径异步加载预制体，加载完回调 onLoaded(prefab)（失败回调 null）。path 语义由实现决定：AssetDatabase=资产路径 / YooAsset=地址 / Resources=相对路径。
        /// 编辑器默认实现是"同步加载 + 立即回调"（见 DefaultUIResLoader）；换 YooAsset 时在其异步完成回调里调 onLoaded 即可。</summary>
        void LoadPrefabAsync(string path, Action<GameObject> onLoaded);

        /// <summary>释放（YooAsset 等需要按 handle 释放；Resources / AssetDatabase 版空实现即可）。</summary>
        void ReleasePrefab(string path);
    }

    /// <summary>
    /// UI 资源加载器全局入口。默认 DefaultUIResLoader；接入方在 Game.Init 前替换成自己的实现：
    /// <code>UIRes.Loader = new YourYooAssetLoader();  Game.Instance.Init();</code>
    /// </summary>
    public static class UIRes
    {
        public static IUIResLoader Loader = new DefaultUIResLoader();
    }

    /// <summary>
    /// 默认加载器：编辑器下用 AssetDatabase（开箱即用，方便开发期直接跑）；
    /// 打包运行时若没被替换，会报一条明确错误提示你注入 —— 生产/打包请务必换成 YooAsset 等实现。
    /// </summary>
    public class DefaultUIResLoader : IUIResLoader
    {
        public void LoadPrefabAsync(string path, Action<GameObject> onLoaded)
        {
#if UNITY_EDITOR
            // 编辑器：AssetDatabase 同步加载 + 立即回调（体验等同同步，小项目/编辑器无损）
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            onLoaded?.Invoke(prefab);
#else
            Debug.LogError("[NRFramework] 未注入 UI 资源加载器！打包运行时不能用 AssetDatabase 加载 UI。\n" +
                           "请实现 IUIResLoader（如 YooAsset 版）并在 Game.Init 前设置 UIRes.Loader = 你的实现。path=" + path);
            onLoaded?.Invoke(null);
#endif
        }

        public void ReleasePrefab(string path) { }
    }
}
