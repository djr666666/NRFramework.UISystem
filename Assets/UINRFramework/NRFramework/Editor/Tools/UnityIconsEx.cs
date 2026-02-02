using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Unity内置图标扩展管理器（包含更多有趣图标）
/// </summary>
public static class UnityIconsEx
{
    private static Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();

    // 补全基本方法
    public static Texture2D GetIcon(string iconName)
    {
        if (string.IsNullOrEmpty(iconName))
            return null;

        if (_iconCache.TryGetValue(iconName, out var cachedIcon))
            return cachedIcon;

        Texture2D icon = null;
        try
        {
            var content = EditorGUIUtility.IconContent(iconName);
            if (content != null && content.image != null)
            {
                icon = content.image as Texture2D;
            }
        }
        catch
        {
            icon = null;
        }

        _iconCache[iconName] = icon;
        return icon;
    }

    public static GUIContent GetContent(string text, string iconName)
    {
        var icon = GetIcon(iconName);
        return new GUIContent(text, icon);
    }

    public static GUIContent GetIconContent(string iconName)
    {
        var icon = GetIcon(iconName);
        return new GUIContent(icon);
    }

    #region 分类图标属性（更全面）

    // ========== 文件夹和文件 ==========
    public static Texture2D Folder => GetIcon("Folder Icon");

    /// <summary>空文件夹图标</summary>
    public static Texture2D FolderEmpty => GetIcon("FolderEmpty Icon");

    /// <summary>打开的文件夹图标</summary>
    public static Texture2D FolderOpened => GetIcon("FolderOpened Icon");

    /// <summary>收藏文件夹图标</summary>
    public static Texture2D FolderFavorite => GetIcon("FolderFavorite Icon");

    /// <summary>锁定文件夹图标</summary>
    public static Texture2D FolderLocked => GetIcon("FolderLocked Icon");

    /// <summary>高亮状态-普通文件夹</summary>
    public static Texture2D FolderIconOn => GetIcon("Folder Icon On");

    /// <summary>高亮状态-空文件夹</summary>
    public static Texture2D FolderEmptyOn => GetIcon("FolderEmpty On");

    /// <summary>高亮状态-打开的文件夹</summary>
    public static Texture2D FolderOpenedOn => GetIcon("FolderOpened On");

    /// <summary>高亮状态-收藏文件夹</summary>
    public static Texture2D FolderFavoriteOn => GetIcon("FolderFavorite On");

    // ========== 文件类型图标 ==========

    /// <summary>文本文件图标</summary>
    public static Texture2D TextFile => GetIcon("TextAsset Icon");

    /// <summary>C#脚本图标</summary>
    public static Texture2D ScriptCS => GetIcon("cs Script Icon");

    /// <summary>JavaScript脚本图标</summary>
    public static Texture2D ScriptJS => GetIcon("js Script Icon");

    /// <summary>DLL动态链接库图标</summary>
    public static Texture2D ScriptDLL => GetIcon("dll Script Icon");

    /// <summary>材质球图标</summary>
    public static Texture2D Material => GetIcon("Material Icon");

    /// <summary>预制体图标</summary>
    public static Texture2D Prefab => GetIcon("Prefab Icon");

    /// <summary>模型预制体图标</summary>
    public static Texture2D PrefabModel => GetIcon("PrefabModel Icon");

    /// <summary>场景文件图标</summary>
    public static Texture2D Scene => GetIcon("SceneAsset Icon");

    /// <summary>音频文件图标</summary>
    public static Texture2D Audio => GetIcon("AudioClip Icon");

    /// <summary>纹理/贴图图标</summary>
    public static Texture2D Texture => GetIcon("Texture Icon");

    /// <summary>精灵/2D图片图标</summary>
    public static Texture2D Sprite => GetIcon("Sprite Icon");

    /// <summary>3D模型/网格图标</summary>
    public static Texture2D Mesh => GetIcon("Mesh Icon");

    /// <summary>动画文件图标</summary>
    public static Texture2D Animation => GetIcon("Animation Icon");

    /// <summary>动画控制器图标</summary>
    public static Texture2D Animator => GetIcon("AnimatorController Icon");

    /// <summary>字体文件图标</summary>
    public static Texture2D Font => GetIcon("Font Icon");

    /// <summary>着色器文件图标</summary>
    public static Texture2D Shader => GetIcon("Shader Icon");

    /// <summary>视频文件图标</summary>
    public static Texture2D Video => GetIcon("MovieTexture Icon");

    /// <summary>光照贴图参数图标</summary>
    public static Texture2D Lightmap => GetIcon("LightmapParameters Icon");

    /// <summary>物理材质图标(3D)</summary>
    public static Texture2D PhysicMaterial => GetIcon("PhysicMaterial Icon");

    /// <summary>物理材质2D图标(2D)</summary>
    public static Texture2D PhysicsMaterial2D => GetIcon("PhysicsMaterial2D Icon");

    // ========== 编辑器窗口图标 ==========

    /// <summary>层级窗口图标</summary>
    public static Texture2D Hierarchy => GetIcon("UnityEditor.HierarchyWindow");

    /// <summary>项目窗口图标</summary>
    public static Texture2D Project => GetIcon("UnityEditor.ProjectWindow");

    /// <summary>控制台窗口图标</summary>
    public static Texture2D Console => GetIcon("UnityEditor.ConsoleWindow");

    /// <summary>检视窗口图标</summary>
    public static Texture2D Inspector => GetIcon("UnityEditor.InspectorWindow");

    /// <summary>游戏视图窗口图标</summary>
    public static Texture2D GameView => GetIcon("UnityEditor.GameView");

    /// <summary>场景视图窗口图标</summary>
    public static Texture2D SceneView => GetIcon("UnityEditor.SceneView");

    /// <summary>动画窗口图标</summary>
    public static Texture2D AnimationWindow => GetIcon("UnityEditor.AnimationWindow");

    /// <summary>性能分析器窗口图标</summary>
    public static Texture2D Profiler => GetIcon("UnityEditor.ProfilerWindow");

    /// <summary>音频混合器图标</summary>
    public static Texture2D AudioMixer => GetIcon("AudioMixerController Icon");

    /// <summary>时间轴窗口图标</summary>
    public static Texture2D Timeline => GetIcon("UnityEditor.Timeline.TimelineWindow");

    /// <summary>包管理器窗口图标</summary>
    public static Texture2D PackageManager => GetIcon("UnityEditor.PackageManager.UI");

    /// <summary>服务窗口图标</summary>
    public static Texture2D Services => GetIcon("Services");

    /// <summary>资源商店图标</summary>
    public static Texture2D AssetStore => GetIcon("Asset Store");

    /// <summary>版本控制图标</summary>
    public static Texture2D VersionControl => GetIcon("UnityEditor.VersionControl");

    // ========== 播放控制图标 ==========

    /// <summary>播放按钮图标</summary>
    public static Texture2D Play => GetIcon("PlayButton");

    /// <summary>暂停按钮图标</summary>
    public static Texture2D Pause => GetIcon("PauseButton");

    /// <summary>停止按钮图标</summary>
    public static Texture2D Stop => GetIcon("StopButton");

    /// <summary>逐帧播放按钮图标</summary>
    public static Texture2D Step => GetIcon("StepButton");

    /// <summary>录制按钮图标</summary>
    public static Texture2D Record => GetIcon("RecordButton");

    /// <summary>高亮状态-播放按钮</summary>
    public static Texture2D PlayOn => GetIcon("PlayButton On");

    /// <summary>高亮状态-暂停按钮</summary>
    public static Texture2D PauseOn => GetIcon("PauseButton On");

    /// <summary>高亮状态-停止按钮</summary>
    public static Texture2D StopOn => GetIcon("StopButton On");

    // ========== 工具和操作图标 ==========

    /// <summary>移动工具图标</summary>
    public static Texture2D MoveTool => GetIcon("MoveTool");

    /// <summary>旋转工具图标</summary>
    public static Texture2D RotateTool => GetIcon("RotateTool");

    /// <summary>缩放工具图标</summary>
    public static Texture2D ScaleTool => GetIcon("ScaleTool");

    /// <summary>矩形工具图标</summary>
    public static Texture2D RectTool => GetIcon("RectTool");

    /// <summary>变换工具图标</summary>
    public static Texture2D TransformTool => GetIcon("TransformTool");

    /// <summary>视图平移工具图标</summary>
    public static Texture2D ViewToolMove => GetIcon("ViewToolMove");

    /// <summary>视图环绕工具图标</summary>
    public static Texture2D ViewToolOrbit => GetIcon("ViewToolOrbit");

    /// <summary>视图缩放工具图标</summary>
    public static Texture2D ViewToolZoom => GetIcon("ViewToolZoom");

    /// <summary>视图第一人称工具图标</summary>
    public static Texture2D ViewToolFPS => GetIcon("ViewToolFPS");

    /// <summary>手形工具图标</summary>
    public static Texture2D HandTool => GetIcon("Hand");

    /// <summary>吸管工具图标</summary>
    public static Texture2D EyeDropper => GetIcon("EyeDropper.Large");

    /// <summary>颜色选择器图标</summary>
    public static Texture2D ColorPicker => GetIcon("ColorPicker.ColorCycle");

    /// <summary>渐变编辑器图标</summary>
    public static Texture2D Gradient => GetIcon("Gradient");

    // ========== 系统图标 ==========

    /// <summary>设置图标</summary>
    public static Texture2D Settings => GetIcon("SettingsIcon");

    /// <summary>搜索图标</summary>
    public static Texture2D Search => GetIcon("Search Icon");

    /// <summary>刷新图标</summary>
    public static Texture2D Refresh => GetIcon("Refresh");

    /// <summary>保存图标</summary>
    public static Texture2D Save => GetIcon("SaveActive");

    /// <summary>另存为图标</summary>
    public static Texture2D SaveAs => GetIcon("SaveAs");

    /// <summary>导入/加载图标</summary>
    public static Texture2D Load => GetIcon("Import");

    /// <summary>导出图标</summary>
    public static Texture2D Export => GetIcon("Export");

    /// <summary>打印图标</summary>
    public static Texture2D Print => GetIcon("Print");

    /// <summary>帮助图标</summary>
    public static Texture2D Help => GetIcon("_Help");

    /// <summary>信息提示图标</summary>
    public static Texture2D Info => GetIcon("console.infoicon");

    /// <summary>警告提示图标</summary>
    public static Texture2D Warning => GetIcon("console.warnicon");

    /// <summary>错误提示图标</summary>
    public static Texture2D Error => GetIcon("console.erroricon");

    // ========== 状态图标 ==========

    /// <summary>成功/通过图标</summary>
    public static Texture2D Success => GetIcon("TestPassed");

    /// <summary>失败图标</summary>
    public static Texture2D Failed => GetIcon("TestFailed");

    /// <summary>忽略图标</summary>
    public static Texture2D Ignored => GetIcon("TestIgnored");

    /// <summary>不确定/未定图标</summary>
    public static Texture2D Inconclusive => GetIcon("TestInconclusive");

    /// <summary>正常/默认状态图标</summary>
    public static Texture2D Normal => GetIcon("TestNormal");

    /// <summary>有效/启用图标</summary>
    public static Texture2D Valid => GetIcon("Valid");

    /// <summary>无效/禁用图标</summary>
    public static Texture2D Invalid => GetIcon("Invalid");

    // ========== 网络和连接图标 ==========

    /// <summary>网络图标</summary>
    public static Texture2D Network => GetIcon("NetworkView Icon");

    /// <summary>云连接图标</summary>
    public static Texture2D Cloud => GetIcon("CloudConnect");

    /// <summary>同步图标</summary>
    public static Texture2D Sync => GetIcon("Sync");

    /// <summary>下载图标</summary>
    public static Texture2D Download => GetIcon("Download");

    /// <summary>上传图标</summary>
    public static Texture2D Upload => GetIcon("Upload");

    /// <summary>已连接状态图标</summary>
    public static Texture2D Connection => GetIcon("Connected");

    /// <summary>未连接状态图标</summary>
    public static Texture2D Disconnected => GetIcon("Disconnected");

    // ========== 时间相关图标 ==========

    /// <summary>时钟/等待图标</summary>
    public static Texture2D Clock => GetIcon("WaitSpin00");

    /// <summary>日历图标</summary>
    public static Texture2D Calendar => GetIcon("Calendar");

    /// <summary>计时器图标</summary>
    public static Texture2D Timer => GetIcon("UnityEditor.AnimationWindow");

    /// <summary>历史记录图标</summary>
    public static Texture2D History => GetIcon("UnityEditor.HistoryWindow");

    // ========== 数学和图表图标 ==========

    /// <summary>图表/图形图标</summary>
    public static Texture2D Graph => GetIcon("UnityEditor.Graphs.AnimatorControllerTool");

    /// <summary>图表图标</summary>
    public static Texture2D Chart => GetIcon("UnityEditor.Chart");

    /// <summary>曲线编辑器图标</summary>
    public static Texture2D Curve => GetIcon("Curve");

    /// <summary>默认网格图标</summary>
    public static Texture2D Grid => GetIcon("Grid.Default");

    /// <summary>大网格图标</summary>
    public static Texture2D GridLarge => GetIcon("Grid.Large");

    /// <summary>小网格图标</summary>
    public static Texture2D GridSmall => GetIcon("Grid.Small");

    // ========== 颜色和视觉效果图标 ==========

    /// <summary>红色图标</summary>
    public static Texture2D ColorRed => GetIcon("ColorRed");

    /// <summary>绿色图标</summary>
    public static Texture2D ColorGreen => GetIcon("ColorGreen");

    /// <summary>蓝色图标</summary>
    public static Texture2D ColorBlue => GetIcon("ColorBlue");

    /// <summary>黄色图标</summary>
    public static Texture2D ColorYellow => GetIcon("ColorYellow");

    /// <summary>青色图标</summary>
    public static Texture2D ColorCyan => GetIcon("ColorCyan");

    /// <summary>洋红色图标</summary>
    public static Texture2D ColorMagenta => GetIcon("ColorMagenta");

    /// <summary>灰色图标</summary>
    public static Texture2D ColorGray => GetIcon("ColorGray");

    /// <summary>白色图标</summary>
    public static Texture2D ColorWhite => GetIcon("ColorWhite");

    /// <summary>显示/可见状态图标</summary>
    public static Texture2D VisibilityOn => GetIcon("VisibilityOn");

    /// <summary>隐藏/不可见状态图标</summary>
    public static Texture2D VisibilityOff => GetIcon("VisibilityOff");

    /// <summary>锁定状态图标</summary>
    public static Texture2D Lock => GetIcon("LockIcon-On");

    /// <summary>解锁状态图标</summary>
    public static Texture2D Unlock => GetIcon("LockIcon");

    // ========== 形状和符号图标 ==========

    /// <summary>圆形图标</summary>
    public static Texture2D Circle => GetIcon("Occlusion");

    /// <summary>方形图标</summary>
    public static Texture2D Square => GetIcon("PreMatCube");

    /// <summary>三角形图标</summary>
    public static Texture2D Triangle => GetIcon("PreMatSphere");

    /// <summary>星星图标</summary>
    public static Texture2D Star => GetIcon("Favorite");

    /// <summary>心形图标</summary>
    public static Texture2D Heart => GetIcon("Favorite Icon");

    /// <summary>旗帜图标</summary>
    public static Texture2D Flag => GetIcon("Flag");

    /// <summary>图钉图标</summary>
    public static Texture2D Pin => GetIcon("Pin");

    /// <summary>标记图标</summary>
    public static Texture2D Marker => GetIcon("Marker");

    // ========== 方向和箭头图标 ==========

    /// <summary>向上箭头图标</summary>
    public static Texture2D ArrowUp => GetIcon("ArrowNavigationUp");

    /// <summary>向下箭头图标</summary>
    public static Texture2D ArrowDown => GetIcon("ArrowNavigationDown");

    /// <summary>向左箭头图标</summary>
    public static Texture2D ArrowLeft => GetIcon("ArrowNavigationLeft");

    /// <summary>向右箭头图标</summary>
    public static Texture2D ArrowRight => GetIcon("ArrowNavigationRight");

    /// <summary>展开箭头图标</summary>
    public static Texture2D ArrowExpand => GetIcon("d_forward");

    /// <summary>折叠箭头图标</summary>
    public static Texture2D ArrowCollapse => GetIcon("d_back");

    /// <summary>最大化箭头图标</summary>
    public static Texture2D ArrowMaximize => GetIcon("Maximize");

    /// <summary>最小化箭头图标</summary>
    public static Texture2D ArrowMinimize => GetIcon("Minimize");

    // ========== 编辑操作图标 ==========

    /// <summary>添加/新建图标</summary>
    public static Texture2D Add => GetIcon("CreateAddNew");

    /// <summary>移除/减少图标</summary>
    public static Texture2D Remove => GetIcon("Toolbar Minus");

    /// <summary>编辑图标</summary>
    public static Texture2D Edit => GetIcon("d_EditIcon");

    /// <summary>复制/克隆图标</summary>
    public static Texture2D Duplicate => GetIcon("TreeEditor.Duplicate");

    /// <summary>复制图标</summary>
    public static Texture2D Copy => GetIcon("Clipboard");

    /// <summary>粘贴图标</summary>
    public static Texture2D Paste => GetIcon("Paste");

    /// <summary>剪切图标</summary>
    public static Texture2D Cut => GetIcon("Cut");

    /// <summary>删除/垃圾桶图标</summary>
    public static Texture2D Delete => GetIcon("TreeEditor.Trash");

    /// <summary>撤销图标</summary>
    public static Texture2D Undo => GetIcon("UndoHistory");

    /// <summary>重做图标</summary>
    public static Texture2D Redo => GetIcon("Redo");

    /// <summary>清除图标</summary>
    public static Texture2D Clear => GetIcon("Clear");

    // ========== 媒体图标 ==========

    /// <summary>摄像机图标</summary>
    public static Texture2D Camera => GetIcon("Camera Icon");

    /// <summary>麦克风图标</summary>
    public static Texture2D Microphone => GetIcon("Microphone");

    /// <summary>扬声器图标</summary>
    public static Texture2D Speaker => GetIcon("AudioSource Icon");

    /// <summary>视频摄像机图标</summary>
    public static Texture2D VideoCamera => GetIcon("VideoClip Icon");

    /// <summary>图片图标</summary>
    public static Texture2D Image => GetIcon("Image Icon");

    // ========== 游戏对象图标 ==========

    /// <summary>游戏对象图标</summary>
    public static Texture2D GameObject => GetIcon("GameObject Icon");

    /// <summary>光源图标</summary>
    public static Texture2D Light => GetIcon("Light Icon");

    /// <summary>粒子系统图标</summary>
    public static Texture2D ParticleSystem => GetIcon("ParticleSystem Icon");

    /// <summary>刚体图标</summary>
    public static Texture2D Rigidbody => GetIcon("Rigidbody Icon");

    /// <summary>碰撞器图标</summary>
    public static Texture2D Collider => GetIcon("BoxCollider Icon");

    /// <summary>关节图标</summary>
    public static Texture2D Joint => GetIcon("ConfigurableJoint Icon");

    /// <summary>地形图标</summary>
    public static Texture2D Terrain => GetIcon("Terrain Icon");

    /// <summary>风力区域图标</summary>
    public static Texture2D Windzone => GetIcon("Windzone Icon");

    // ========== 有趣的特殊图标 ==========

    /// <summary>幽灵图标(用于角色选择)</summary>
    public static Texture2D Ghost => GetIcon("AvatarSelector");

    /// <summary>机器人图标(用于角色枢轴点)</summary>
    public static Texture2D Robot => GetIcon("AvatarPivot");

    /// <summary>巫师图标(用于预制体变体)</summary>
    public static Texture2D Wizard => GetIcon("PrefabVariant Icon");

    /// <summary>王冠图标(用于独立平台设置)</summary>
    public static Texture2D Crown => GetIcon("BuildSettings.Standalone");

    /// <summary>火箭图标(用于Broadcom平台设置)</summary>
    public static Texture2D Rocket => GetIcon("BuildSettings.Broadcom");

    /// <summary>外星人图标(用于Xbox360平台设置)</summary>
    public static Texture2D Alien => GetIcon("BuildSettings.Xbox360");

    /// <summary>手里剑图标(用于Metro平台设置)</summary>
    public static Texture2D NinjaStar => GetIcon("BuildSettings.Metro");

    /// <summary>龙图标(用于WebGL平台设置)</summary>
    public static Texture2D Dragon => GetIcon("BuildSettings.WebGL");

    #endregion

    #region 更多GUIContent快捷方法

    public static GUIContent FolderContent(string text = null) =>
        GetContent(text ?? "文件夹", "Folder Icon");

    public static GUIContent FileContent(string text = null) =>
        GetContent(text ?? "文件", "TextAsset Icon");

    public static GUIContent PlayContent(string text = null) =>
        GetContent(text ?? "播放", "PlayButton");

    public static GUIContent SaveContent(string text = null) =>
        GetContent(text ?? "保存", "SaveActive");

    public static GUIContent RefreshContent(string text = null) =>
        GetContent(text ?? "刷新", "Refresh");

    public static GUIContent SettingsContent(string text = null) =>
        GetContent(text ?? "设置", "SettingsIcon");

    public static GUIContent SearchContent(string text = null) =>
        GetContent(text ?? "搜索", "Search Icon");

    // 网络相关
    public static GUIContent NetworkContent(string text = null) =>
        GetContent(text ?? "网络", "NetworkView Icon");

    public static GUIContent CloudContent(string text = null) =>
        GetContent(text ?? "云端", "CloudConnect");

    public static GUIContent SyncContent(string text = null) =>
        GetContent(text ?? "同步", "Sync");

    public static GUIContent DownloadContent(string text = null) =>
        GetContent(text ?? "下载", "Download");

    public static GUIContent UploadContent(string text = null) =>
        GetContent(text ?? "上传", "Upload");

    // 编辑操作
    public static GUIContent AddContent(string text = null) =>
        GetContent(text ?? "添加", "CreateAddNew");

    public static GUIContent RemoveContent(string text = null) =>
        GetContent(text ?? "删除", "Toolbar Minus");

    public static GUIContent EditContent(string text = null) =>
        GetContent(text ?? "编辑", "d_EditIcon");

    public static GUIContent DuplicateContent(string text = null) =>
        GetContent(text ?? "复制", "TreeEditor.Duplicate");

    public static GUIContent CopyContent(string text = null) =>
        GetContent(text ?? "拷贝", "Clipboard");

    public static GUIContent PasteContent(string text = null) =>
        GetContent(text ?? "粘贴", "Paste");

    public static GUIContent DeleteContent(string text = null) =>
        GetContent(text ?? "删除", "TreeEditor.Trash");

    public static GUIContent UndoContent(string text = null) =>
        GetContent(text ?? "撤销", "UndoHistory");

    public static GUIContent RedoContent(string text = null) =>
        GetContent(text ?? "重做", "Redo");

    // 状态信息
    public static GUIContent InfoContent(string text = null) =>
        GetContent(text ?? "信息", "console.infoicon");

    public static GUIContent WarningContent(string text = null) =>
        GetContent(text ?? "警告", "console.warnicon");

    public static GUIContent ErrorContent(string text = null) =>
        GetContent(text ?? "错误", "console.erroricon");

    public static GUIContent SuccessContent(string text = null) =>
        GetContent(text ?? "成功", "TestPassed");

    public static GUIContent FailedContent(string text = null) =>
        GetContent(text ?? "失败", "TestFailed");

    // 视觉和显示
    public static GUIContent VisibilityOnContent(string text = null) =>
        GetContent(text ?? "显示", "VisibilityOn");

    public static GUIContent VisibilityOffContent(string text = null) =>
        GetContent(text ?? "隐藏", "VisibilityOff");

    public static GUIContent LockContent(string text = null) =>
        GetContent(text ?? "锁定", "LockIcon-On");

    public static GUIContent UnlockContent(string text = null) =>
        GetContent(text ?? "解锁", "LockIcon");

    // 收藏和标记
    public static GUIContent FavoriteContent(string text = null) =>
        GetContent(text ?? "收藏", "Favorite");

    public static GUIContent StarContent(string text = null) =>
        GetContent(text ?? "标星", "Favorite Icon");

    public static GUIContent FlagContent(string text = null) =>
        GetContent(text ?? "标记", "Flag");

    public static GUIContent PinContent(string text = null) =>
        GetContent(text ?? "置顶", "Pin");

    // 方向箭头
    public static GUIContent ArrowUpContent(string text = null) =>
        GetContent(text ?? "向上", "ArrowNavigationUp");

    public static GUIContent ArrowDownContent(string text = null) =>
        GetContent(text ?? "向下", "ArrowNavigationDown");

    public static GUIContent ArrowLeftContent(string text = null) =>
        GetContent(text ?? "向左", "ArrowNavigationLeft");

    public static GUIContent ArrowRightContent(string text = null) =>
        GetContent(text ?? "向右", "ArrowNavigationRight");

    // 颜色相关
    public static GUIContent ColorPickerContent(string text = null) =>
        GetContent(text ?? "取色", "ColorPicker.ColorCycle");

    public static GUIContent PaletteContent(string text = null) =>
        GetContent(text ?? "调色板", "ColorPicker.ColorCycle");

    public static GUIContent GradientContent(string text = null) =>
        GetContent(text ?? "渐变", "Gradient");

    // 时间相关
    public static GUIContent ClockContent(string text = null) =>
        GetContent(text ?? "时钟", "WaitSpin00");

    public static GUIContent TimerContent(string text = null) =>
        GetContent(text ?? "计时", "UnityEditor.AnimationWindow");

    public static GUIContent CalendarContent(string text = null) =>
        GetContent(text ?? "日历", "Calendar");

    public static GUIContent HistoryContent(string text = null) =>
        GetContent(text ?? "历史", "UnityEditor.HistoryWindow");

    // 有趣的特殊图标
    public static GUIContent GhostContent(string text = null) =>
        GetContent(text ?? "幽灵", "AvatarSelector");

    public static GUIContent RobotContent(string text = null) =>
        GetContent(text ?? "机器人", "AvatarPivot");

    public static GUIContent WizardContent(string text = null) =>
        GetContent(text ?? "巫师", "PrefabVariant Icon");

    public static GUIContent RocketContent(string text = null) =>
        GetContent(text ?? "火箭", "BuildSettings.Broadcom");

    public static GUIContent AlienContent(string text = null) =>
        GetContent(text ?? "外星人", "BuildSettings.Xbox360");

    public static GUIContent NinjaContent(string text = null) =>
        GetContent(text ?? "忍者", "BuildSettings.Metro");

    public static GUIContent DragonContent(string text = null) =>
        GetContent(text ?? "龙", "BuildSettings.WebGL");

    #endregion
}