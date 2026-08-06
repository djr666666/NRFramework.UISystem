// https://github.com/NRatel/NRFramework.UI

namespace NRFramework
{
    /// <summary>
    /// 框架硬约束常量。这些是【代码编译期依赖】—— UIManager 直接引用它们，
    /// 所以必须留在本包（NRFramework.Runtime）内，【不能】像 EditorSetting 那样移到使用方 Assets，
    /// 否则框架程序集引用不到、直接编译报错。
    /// </summary>
    public class Config
    {
        // ============ GGame 三大硬约束（使用方可自由改 GGame 内容，但这三个“名字”不能动）============
        // 启动预制体 GGame 现由【使用方】维护：放在使用方 Assets 下任意 Resources/ 目录里，
        // 可以往里拖加载界面 / 改布局 / 加子物体 / 别人扩展结构，都随便 ——
        // 唯独下面这三个名字被 UIManager.Awake() 写死引用，改了框架就找不到 UI 根，务必保持：

        /// <summary>UI 根画布的 GameObject 名。UIManager 用 GameObject.Find("UICanvas") 全局按名字找，必须唯一、不可改名。</summary>
        public const string kUICanvasPath = "UICanvas";

        /// <summary>UI 相机的 GameObject 名。UIManager 用 GameObject.Find("UICamera") 全局按名字找，必须唯一、不可改名。</summary>
        public const string kUICameraPath = "UICamera";

        /// <summary>启动预制体名。UIManager 用 Resources.Load&lt;GameObject&gt;("GGame") 加载 —— 使用方的 GGame 预制体必须叫这个名、且在某个 Resources/ 目录下。</summary>
        public const string GGame = "GGame";

        /// <summary>实例化后重设的名字，与 GGame 保持一致。</summary>
        public const string GGameName = "GGame";
        // =====================================================================================

        /// <summary>UIPanel 默认厚度（普通默认值，非硬约束）。</summary>
        public const int kDefaultPanelThickness = 10;
    }
}
