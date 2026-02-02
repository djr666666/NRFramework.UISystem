// ===========================================
// 自动生成的UI路径常量类
// 生成时间: 2026-02-02 16:23:36
// 总数量: 1
// ===========================================

using System.Collections.Generic;

public static class UIPathConstants
{

    // ===== UI层级定义 ===== //
    public enum UILayer
    {
        /// <summary>
        /// 世界场景层
        /// </summary>
        WorldScene = 0,
        /// <summary>
        /// 世界物体层
        /// </summary>
        WorldObject = 1,
        /// <summary>
        /// 世界特效层
        /// </summary>
        WorldEffect = 2,
        /// <summary>
        /// 拖拽层
        /// </summary>
        DragLayer = 3,
        /// <summary>
        /// 主界面层
        /// </summary>
        MainLayer = 4,
        /// <summary>
        /// 全屏界面层
        /// </summary>
        ScreenLayer = 5,
        /// <summary>
        /// 模态弹窗层
        /// </summary>
        ModalLayer = 6,
        /// <summary>
        /// 普通弹窗层
        /// </summary>
        PopLayer = 7,
        /// <summary>
        /// 新手引导层
        /// </summary>
        GuideLayer = 8,
        /// <summary>
        /// 顶层通知
        /// </summary>
        TopLayer = 9,
        /// <summary>
        /// 加载界面层
        /// </summary>
        LoadingLayer = 10,
        /// <summary>
        /// 鼠标光标层
        /// </summary>
        CursorLayer = 11,
    }

    // ===== 主界面层 (MainLayer) ===== //

    /// <summary>
    /// Ui_TEST_1
    /// 路径: Assets/Prefabs/Ui_TEST_1.prefab
    /// 层级: 主界面层
    /// 层级值: 4
    /// </summary>
    public const string Ui_TEST_1_UIPanel = "Assets/Prefabs/Ui_TEST_1.prefab";
    public const int Ui_TEST_1_UIlayer = 4;


    // ===== UI路径字典 ===== //
    public static readonly Dictionary<string, string> UIPathDictionary = new Dictionary<string, string>()
    {
            { "Ui_TEST_1", Ui_TEST_1_UIPanel }
    };

    // ===== UI层级字典 ===== //
    public static readonly Dictionary<string, UILayer> UILayerDictionary = new Dictionary<string, UILayer>()
    {
            { "Ui_TEST_1", UILayer.MainLayer }
    };

}
