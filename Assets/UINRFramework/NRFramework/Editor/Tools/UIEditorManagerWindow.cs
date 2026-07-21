#if UNITY_EDITOR
using NRFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIEditorManagerWindow : EditorWindow
{

    [System.Serializable]
    public class UIItem
    {
        public string guid;           // 预制体GUID
        public string prefabPath;     // 预制体路径
        public string prefabName;     // 预制体名称
        public string folderPath;     // 文件夹路径（GUI下的相对路径）
        public string fullFolderPath; // 完整文件夹路径
        public UILayer uiLayer;       // UI层级
        public bool isActive = true;  // 是否激活
        public int sortOrder = 0;     // 排序值
        public bool layerConfirmed = true; // 层级是否已人工确认(false=新扫描自动识别的，进"待确认区")

        // —— 静态体检(扫描时分析预制体，仅供参考；精确 DrawCall 需运行时测) ——
        public int graphicCount;   // 图形总数(Image/Text/RawImage/TMP)
        public int imageCount;     // 图片数
        public int textCount;      // 文本数(含TMP)
        public int maskCount;      // Mask/RectMask2D 数(会打断合批)
        public int materialCount;  // 不同材质数
        public int textureCount;   // 不同纹理/图集数
        public int estBatches;     // 预估批次(粗算，参考用)
        public bool analyzed;      // 是否已体检过
        public int realBatches = -1; // 运行时实测批次(-1=未测)
        public long memBytes;      // 依赖纹理内存(字节，加载后≈占多少RAM)
        public int raycastCount;   // 开了 raycastTarget 的图形数(交互射线开销)
    }

    public enum UILayer
    {
        // ------------------------------------------------------------------第1部分：世界空间UI
        WorldScene = 0,        // 场景UI：地图标记、地面特效
        WorldObject = 1,       // 物体UI：血条、名字、NPC标识
        WorldEffect = 2,       // 特效UI：伤害数字、BUFF图标、交互提示
                               // ------------------------------------------------------------------第2部分：特殊状态
        DragLayer = 3,         // 拖拽（比所有世界UI都低）
                               // ------------------------------------------------------------------第3部分：屏幕空间UI
        MainLayer = 4,         // 主界面HUD
        ScreenLayer = 5,       // 全屏功能界面
                               // ------------------------------------------------------------------第4部分：弹窗交互       
        ModalLayer = 6,        // 模态对话框
        PopLayer = 7,          // 普通弹窗
                               // ------------------------------------------------------------------第5部分：提示引导
        GuideLayer = 8,        // 新手引导
        TopLayer = 9,          // 飘字提示/服务器公告
                               // ------------------------------------------------------------------第6部分：系统通知
        LoadingLayer = 10,     // 加载界面
                               // ------------------------------------------------------------------第7部分：顶级系统
        CursorLayer = 11,      // 鼠标/手势
    }

    [System.Serializable]
    public class UIConfigData
    {
        public List<UIItem> uiItems = new List<UIItem>();
        public List<string> scanPaths = new List<string>();
        public string exportTime;
        public int totalCount;
    }

    [System.Serializable]
    public class UIPathConfig
    {
        public List<string> uiScanPaths = new List<string>();
    }

    // 数据存储
    private List<UIItem> allUIItems = new List<UIItem>();

    // 按层级分组的数据结构：UILayer -> List<UIItem>
    private Dictionary<UILayer, List<UIItem>> layerItems =
        new Dictionary<UILayer, List<UIItem>>();

    private List<string> customScanPaths = new List<string>();

    // UI状态
    private Vector2 scrollPosition;
    private string searchText = "";
    private bool showOnlyActive = true;
    private UILayer filterLayer = UILayer.MainLayer;
    private bool _filterByLayer = false; // false = 显示全部层级，true = 按 filterLayer 过滤
    private bool showSettings = false;
    private bool hideEmptyLayers = false; // 隐藏空层级，列表更聚焦
    private bool _dirty = false;          // 有未保存改动(改层级/激活但没点保存)

    // 标脏 + 刷新标题(*表示有未保存改动)
    void MarkDirty() { _dirty = true; if (this != null) titleContent.text = "UI管理器 *"; }
    void ClearDirty() { _dirty = false; if (this != null) titleContent.text = "UI管理器"; }

    // 展开状态
    private Dictionary<string, bool> layerFoldoutStates = new Dictionary<string, bool>();

    // 所有预定义的UI层级（用于显示空层级）
    private UILayer[] allLayers = new UILayer[]
    {
        
        UILayer.WorldScene,
        UILayer.WorldObject,
        UILayer.WorldEffect,
        UILayer.DragLayer,
        UILayer.MainLayer,
        UILayer.ScreenLayer,
        UILayer.ModalLayer,
        UILayer.PopLayer,
        UILayer.GuideLayer,
        UILayer.TopLayer,
        UILayer.LoadingLayer,
        UILayer.CursorLayer,
    };

    // 配置文件路径
    private const string PATH_CONFIG_FILE = "Assets/Resources/path_config.json";
    private const string UI_CONFIG_FILE = "Assets/Resources/UIConfig.json";
    private const string UI_PATH_CONSTANTS_FILE = "Assets/Resources/UIPath.cs"; // 常量文件路径

    [MenuItem("Tools/UI管理器")]
    public static void ShowWindow()
    {
        var window = GetWindow<UIEditorManagerWindow>("UI管理器");
        window.minSize = new Vector2(1200, 700); // 增加宽度以适应更长的路径
        window.LoadConfig(); // 打开时自动加载配置
    }

    void OnEnable()
    {
        // 初始化所有层级（确保空层级也能显示）
        InitializeAllLayers();

        // 加载路径配置
        LoadPathConfig();

        // 如果已有数据，加载UI配置，否则扫描
        if (File.Exists(UI_CONFIG_FILE))
        {
            LoadConfig();
        }
        else
        {
            ScanUIComponents();
        }
    }

    // 关窗口时若有未保存改动，提醒保存(防手滑丢层级/激活配置)
    void OnDestroy()
    {
        if (_dirty)
        {
            if (EditorUtility.DisplayDialog("有未保存的改动",
                "你修改了层级/激活状态但还没保存，是否保存？", "保存", "不保存"))
            {
                SaveConfig();
            }
        }
    }

    void InitializeAllLayers()
    {
        // 确保所有预定义层级都在字典中存在
        foreach (var layer in allLayers)
        {
            if (!layerItems.ContainsKey(layer))
            {
                layerItems[layer] = new List<UIItem>();
            }
        }
    }

    void LoadPathConfig()
    {
        if (File.Exists(PATH_CONFIG_FILE))
        {
            string json = File.ReadAllText(PATH_CONFIG_FILE);
            UIPathConfig config = JsonUtility.FromJson<UIPathConfig>(json);
            customScanPaths = config.uiScanPaths ?? new List<string>();
        }
        else
        {
            // 配置默认路径（修改为GUI文件夹）
            customScanPaths = new List<string>
            {
                "Assets/Project/Prefabs/GUI"
            };
            SavePathConfig();
        }
    }

    void SavePathConfig()
    {
        UIPathConfig config = new UIPathConfig { uiScanPaths = customScanPaths };
        string json = JsonUtility.ToJson(config, true);
        string directory = Path.GetDirectoryName(PATH_CONFIG_FILE);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(PATH_CONFIG_FILE, json);
        AssetDatabase.Refresh();
    }

    void OnGUI()
    {
        // 鼠标移动就重绘 —— 否则窗口静止时 tooltip 不会弹(悬停会亮但没提示)
        wantsMouseMove = true;
        if (Event.current.type == EventType.MouseMove) Repaint();

        DrawToolbar();
        DrawSettingsPanel();
        DrawStatistics();
        DrawUIList();
        DrawBottomButtons();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // 扫描按钮"🔍 扫描"
        if (GUILayout.Button(UnityIconsEx.SearchContent("扫描"), EditorStyles.toolbarButton, GUILayout.Width(100), GUILayout.Height(50)))
        {
            ScanUIComponents();
        }

        // 加载配置按钮"📂 加载配置" 
        if (GUILayout.Button(UnityIconsEx.FolderContent("加载配置"), EditorStyles.toolbarButton, GUILayout.Width(100), GUILayout.Height(50)))
        {
            LoadConfig(true);   // 手动点 → 浮层反馈
        }

        // 设置按钮"⚙️ 设置"（开启时高亮蓝底，做出"选中tab"的感觉）
        Color tbg0 = GUI.backgroundColor;
        if (showSettings) GUI.backgroundColor = new Color(0.29f, 0.55f, 0.95f);
        showSettings = GUILayout.Toggle(showSettings, UnityIconsEx.SettingsContent("设置"), EditorStyles.toolbarButton, GUILayout.Width(100), GUILayout.Height(50));
        GUI.backgroundColor = tbg0;

        // 搜索框
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
        string newSearchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
        if (newSearchText != searchText)
        {
            searchText = newSearchText;
            FilterUIItems();
        }

        // 层级过滤
        bool newFilterByLayer = GUILayout.Toggle(_filterByLayer, "层级:", EditorStyles.toolbarButton, GUILayout.Width(40));
        if (newFilterByLayer != _filterByLayer) { _filterByLayer = newFilterByLayer; FilterUIItems(); }
        UILayer newFilterLayer = (UILayer)EditorGUILayout.EnumPopup(filterLayer, EditorStyles.toolbarDropDown, GUILayout.Width(100));
        if (newFilterLayer != filterLayer) { filterLayer = newFilterLayer; if (_filterByLayer) FilterUIItems(); }

        // 显示已激活
        showOnlyActive = GUILayout.Toggle(showOnlyActive, "仅激活", EditorStyles.toolbarButton);

        // 隐藏空层级
        hideEmptyLayers = GUILayout.Toggle(hideEmptyLayers, "隐藏空层", EditorStyles.toolbarButton);

        GUILayout.FlexibleSpace();

        // 快速操作
        if (GUILayout.Button("📋 操作", EditorStyles.toolbarDropDown, GUILayout.Width(60)))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("全部激活"), false, () => SetAllActive(true));
            menu.AddItem(new GUIContent("全部禁用"), false, () => SetAllActive(false));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("重置层级"), false, ResetAllLayers);
            menu.AddItem(new GUIContent("自动排序"), false, AutoSortByLayer);
            menu.AddSeparator("");
            //menu.AddItem(new GUIContent("导出为CSV"), false, ExportToCSV);
            menu.AddItem(new GUIContent("显示所有层级"), false, () =>
            {
                foreach (var layer in allLayers)
                {
                    string layerKey = $"layer_{layer}";
                    if (!layerFoldoutStates.ContainsKey(layerKey))
                        layerFoldoutStates[layerKey] = true;
                }
                Repaint();
            });
            menu.DropDown(GUILayoutUtility.GetLastRect());
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawSettingsPanel()
    {
        if (!showSettings) return;

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("🔧 扫描路径设置", EditorStyles.boldLabel);

        // 路径列表
        for (int i = 0; i < customScanPaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            customScanPaths[i] = EditorGUILayout.TextField($"路径 {i + 1}:", customScanPaths[i]);

            //"🗑️"（红色 hover 感：删除用红底）
            Color dbg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.85f, 0.32f, 0.29f);
            if (GUILayout.Button(UnityIconsEx.Delete, GUILayout.Width(30)))
            {
                customScanPaths.RemoveAt(i);
                GUI.backgroundColor = dbg;
                break;
            }
            GUI.backgroundColor = dbg;
            EditorGUILayout.EndHorizontal();
        }

        // 添加新路径
        if (GUILayout.Button("+ 添加扫描路径", GUILayout.Height(25)))
        {
            customScanPaths.Add("Assets/");
        }

        EditorGUILayout.Space();

        // 路径操作
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从文件夹选择", GUILayout.Height(25)))
        {
            string path = EditorUtility.OpenFolderPanel("选择UI文件夹", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为相对路径
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                    if (!customScanPaths.Contains(path))
                        customScanPaths.Add(path);
                }
            }
        }

        if (GUILayout.Button("保存路径设置", GUILayout.Height(25)))
        {
            SavePathConfig();
            EditorUtility.DisplayDialog("保存成功", "扫描路径设置已保存", "确定");
        }

        if (GUILayout.Button("恢复默认", GUILayout.Height(25)))
        {
            customScanPaths = new List<string> { "Assets/Project/Prefabs/GUI" };  //自己配置
            SavePathConfig();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    void DrawStatistics()
    {
        int total = allUIItems.Count;
        int active = allUIItems.Count(i => i.isActive);

        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("📊", GUILayout.Width(20));
        GUILayout.Label($"总数 {total}", EditorStyles.boldLabel, GUILayout.Width(64));

        Color old = GUI.contentColor;
        GUI.contentColor = new Color(0.40f, 0.85f, 0.42f);
        GUILayout.Label($"激活 {active}", EditorStyles.boldLabel, GUILayout.Width(64));
        GUI.contentColor = old;

        // 彩色计数 chip：只显示有内容的层级(省得挤)
        foreach (var layer in allLayers)
        {
            int count = layerItems.ContainsKey(layer) ? layerItems[layer].Count : 0;
            if (count == 0) continue;

            Rect dot = GUILayoutUtility.GetRect(9, 9, GUILayout.Width(9), GUILayout.Height(16));
            dot.height = 9; dot.y += 4;
            EditorGUI.DrawRect(dot, LayerColor(layer));

            // chip 可点击 → 直接筛选该层(再点"层级"开关可取消)
            var chip = new GUIContent($"{GetLayerName(layer)} {count}", "点击只看该层级");
            if (GUILayout.Button(chip, EditorStyles.miniLabel))
            {
                _filterByLayer = true; filterLayer = layer; FilterUIItems();
            }
            GUILayout.Space(6);
        }

        // 批次列图例：告诉别的开发者 ~估 / 实 是什么
        GUILayout.Space(10);
        Color lg = GUI.contentColor;
        GUI.contentColor = new Color(1f, 1f, 1f, 0.7f);
        GUILayout.Label("｜ 批次列：", EditorStyles.miniLabel, GUILayout.Width(58));
        GUI.contentColor = new Color(0.4f, 0.8f, 0.45f);
        GUILayout.Label("~估=静态预估", EditorStyles.miniLabel, GUILayout.Width(78));
        GUI.contentColor = new Color(0.4f, 0.7f, 1f);
        GUILayout.Label("实=实测(点【测】)", EditorStyles.miniLabel, GUILayout.Width(108));
        GUI.contentColor = new Color(1f, 1f, 1f, 0.7f);
        GUILayout.Label("XM=纹理运行内存(非打包体积)  射线N=RaycastTarget开启数(纯装饰可关)", EditorStyles.miniLabel, GUILayout.Width(360));
        GUI.contentColor = lg;

        GUILayout.FlexibleSpace();

        // Play 模式下显示当前帧真实总批次(整个 Game 视图)，实测的参考基准
        if (EditorApplication.isPlaying)
        {
            Color pcc = GUI.contentColor;
            GUI.contentColor = new Color(0.4f, 0.7f, 1f);
            GUILayout.Label($"▶ 当前帧总批次 {UnityStats.batches}", EditorStyles.miniBoldLabel);
            GUI.contentColor = pcc;
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawUIList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (allUIItems.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到UI预制体，请点击【扫描】按钮", MessageType.Info);
        }
        else
        {
            // —— 顶部"待确认层级"区：自动识别、还没人工确认的 UI 单独放这，样式明显区别于真实层级 ——
            var pending = new List<UIItem>();
            foreach (var kv in layerItems)
                foreach (var it in kv.Value)
                    if (!it.layerConfirmed) pending.Add(it);
            DrawPendingSection(pending);   // 常驻显示(空时也在，兼作"记得点扫描"的引导)

            // 遍历所有预定义的层级，确保每个层级都显示
            foreach (var layer in allLayers.OrderBy(l => (int)l))
            {
                string layerKey = $"layer_{layer}";

                // 初始化展开状态
                if (!layerFoldoutStates.ContainsKey(layerKey))
                    layerFoldoutStates[layerKey] = layerItems.ContainsKey(layer) && layerItems[layer].Count > 0;

                // 获取该层级下的UI列表
                List<UIItem> items = null;
                if (layerItems.ContainsKey(layer))
                {
                    items = layerItems[layer];
                }
                else
                {
                    items = new List<UIItem>();
                }

                // 该层级只显示"已确认"的项(待确认的在顶部区)
                var shown = items.Where(i => i.layerConfirmed).ToList();

                // 层级标题 - 显示中文名称、层级值和数量
                int itemCount = shown.Count;

                // 隐藏空层级(勾选后跳过没内容的层)
                if (hideEmptyLayers && itemCount == 0) continue;

                // 标题栏底纹染上层级主题色(有内容深一点，空层淡一点)，分区感更强
                var headStyle = new GUIStyle(EditorStyles.toolbar) { fixedHeight = 24 };
                Color hbg0 = GUI.backgroundColor;
                GUI.backgroundColor = Color.Lerp(Color.white, LayerColor(layer), itemCount > 0 ? 0.55f : 0.18f);
                EditorGUILayout.BeginHorizontal(headStyle, GUILayout.Height(24));
                GUI.backgroundColor = hbg0;   // 立刻还原，别染到里面的文字/圆点

                // 层级主题色圆点
                Rect gdot = GUILayoutUtility.GetRect(14, 24, GUILayout.Width(14), GUILayout.Height(24));
                gdot.x += 3; gdot.y += 8; gdot.width = 9; gdot.height = 9;
                Color gdc = LayerColor(layer);        // 空层级也用本色，只是调淡提示"暂无内容"
                if (itemCount == 0) gdc.a = 0.4f;
                EditorGUI.DrawRect(gdot, gdc);

                var foldStyle = new GUIStyle(EditorStyles.foldout) { fontSize = 13, fontStyle = FontStyle.Bold };
                layerFoldoutStates[layerKey] = EditorGUILayout.Foldout(
                    layerFoldoutStates[layerKey],
                    $" {GetLayerName(layer)}  ·  {layer}   ({itemCount})",
                    true,
                    foldStyle
                );

                // 如果该层级为空，显示提示
                if (itemCount == 0)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("(空)", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndHorizontal();

                if (layerFoldoutStates[layerKey] && items != null)
                {
                    EditorGUILayout.BeginVertical("box");

                    // 显示该层级下的所有UI项
                    if (shown.Count == 0)
                    {
                        // 空层级：一行小灰字，别用占地方的 HelpBox
                        Color ec0 = GUI.contentColor;
                        GUI.contentColor = new Color(1f, 1f, 1f, 0.4f);
                        GUILayout.Label("     — 该层暂无 UI —", EditorStyles.miniLabel);
                        GUI.contentColor = ec0;
                    }
                    else
                    {
                        // 按排序值和名称排序
                        var sortedItems = shown
                            .OrderBy(i => i.sortOrder)
                            .ThenBy(i => i.prefabName)
                            .ToList();

                        for (int ri = 0; ri < sortedItems.Count; ri++)
                        {
                            DrawUIItem(sortedItems[ri], ri);
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawUIItem(UIItem item, int rowIndex = 0)
    {
        // 斑马纹：奇数行底色略深，长列表更好扫读
        Color rbg0 = GUI.backgroundColor;
        if (rowIndex % 2 == 1) GUI.backgroundColor = new Color(0.86f, 0.87f, 0.9f);
        Rect rowRect = EditorGUILayout.BeginHorizontal("box");
        GUI.backgroundColor = rbg0;
        GUILayout.Space(5);   // 给左侧色条留位置

        // 激活开关
        bool newActive = EditorGUILayout.Toggle(item.isActive, GUILayout.Width(20));
        if (newActive != item.isActive)
        {
            item.isActive = newActive;
            MarkDirty();
        }

        // 禁用项整行置灰(一眼看出哪些被关掉)，末尾还原
        Color rowColor0 = GUI.color;
        if (!item.isActive) GUI.color = new Color(1f, 1f, 1f, 0.45f);

        // 预制体名称（可点击）
        GUIContent nameContent = new GUIContent(item.prefabName,
            $"\n     层级: {item.uiLayer}\n     排序: {item.sortOrder} \n     完整路径:      {item.prefabPath}");

        if (GUILayout.Button(nameContent, EditorStyles.label, GUILayout.Width(200)))
        {
            OpenPrefab(item.prefabPath);
        }

        // 待确认项：一个绿色 ✓ 按钮，点了就确认(移出待确认区)
        if (!item.layerConfirmed)
        {
            Color cb = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.30f, 0.70f, 0.38f);
            if (GUILayout.Button(new GUIContent("✓", "确认该层级(自动识别的，确认后移入正式层级分组)"), GUILayout.Width(26)))
            {
                item.layerConfirmed = true;
                MarkDirty();
            }
            GUI.backgroundColor = cb;
        }

        // 层级选择 - 这里会触发重新分组
        EditorGUILayout.LabelField("层级:", GUILayout.Width(35));
        UILayer oldLayer = item.uiLayer;
        Color lbg0 = GUI.backgroundColor;
        GUI.backgroundColor = Color.Lerp(Color.white, LayerColor(item.uiLayer), 0.35f);  // 下拉染层级色，和左侧色条呼应
        UILayer newLayer = (UILayer)EditorGUILayout.EnumPopup(item.uiLayer, GUILayout.Width(100));
        GUI.backgroundColor = lbg0;
        if (newLayer != oldLayer)
        {
            item.uiLayer = newLayer;
            item.layerConfirmed = true;   // 手动选了层级 = 确认，移出待确认区
            // 层级改变，重新分组
            UpdateItemLayer(item, oldLayer, newLayer);
            MarkDirty();
        }

        // 排序值
        //EditorGUILayout.LabelField("排序:", GUILayout.Width(35));
        //int newSort = EditorGUILayout.IntField(item.sortOrder, GUILayout.Width(50));
        //if (newSort != item.sortOrder)
        //{
        //    item.sortOrder = newSort;
        //    // 排序值改变，重新排序显示
        //    Repaint();
        //}

        //// 文件夹路径显示（仅显示GUI下的相对路径）
        //EditorGUILayout.LabelField(item.folderPath, EditorStyles.miniLabel, GUILayout.Width(200));

        // 完整路径显示（可直接选中复制：鼠标划选 → Ctrl+C）
        Color pc = GUI.contentColor;
        GUI.contentColor = new Color(0.45f, 0.68f, 1f);   // 淡蓝，像可复制的链接
        EditorGUILayout.SelectableLabel(item.prefabPath, EditorStyles.label,
            GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
        GUI.contentColor = pc;

        // 批次小标：估(静态) + 实(运行时)，红绿灯着色，悬停看构成
        if (item.analyzed)
        {
            string tip = $"静态预估(参考,精确需运行时测)\n" +
                         $"图形 {item.graphicCount}(图片{item.imageCount}/文本{item.textCount})\n" +
                         $"材质 {item.materialCount} · 纹理/图集 {item.textureCount} · Mask {item.maskCount}\n" +
                         $"预估批次 ≈ {item.estBatches}" +
                         (item.realBatches >= 0 ? $"\n运行时实测 = {item.realBatches}" : "\n(实测:Play模式下点【测】)");
            Color bc0 = GUI.contentColor;
            GUI.contentColor = EstBatchColor(item.estBatches);
            GUILayout.Label(new GUIContent($"~估{item.estBatches}", tip), EditorStyles.miniBoldLabel, GUILayout.Width(46));
            if (item.realBatches >= 0)
            {
                GUI.contentColor = EstBatchColor(item.realBatches);
                GUILayout.Label(new GUIContent($"实{item.realBatches}", tip), EditorStyles.miniBoldLabel, GUILayout.Width(42));
            }
            else
            {
                GUI.contentColor = new Color(1f, 1f, 1f, 0.4f);
                GUILayout.Label(new GUIContent("实?", "Play模式下点右侧【测】实测"), EditorStyles.miniLabel, GUILayout.Width(42));
            }
            GUI.contentColor = bc0;
        }
        else
        {
            GUILayout.Label(new GUIContent("未测", "点【扫描】做静态体检"), EditorStyles.miniLabel, GUILayout.Width(88));
        }

        // 内存(纹理) + 射线目标
        if (item.analyzed)
        {
            float mb = item.memBytes / 1048576f;
            Color mc0 = GUI.contentColor;
            GUI.contentColor = mb < 2f ? new Color(0.4f, 0.8f, 0.45f) : mb < 6f ? new Color(0.95f, 0.7f, 0.2f) : new Color(0.9f, 0.35f, 0.3f);
            GUILayout.Label(new GUIContent($"{mb:0.0}M",
                    $"纹理运行内存 ≈ {mb:0.00} MB\n加载到内存(RAM)后占用，纹理为主，不用build\n注意：这是【运行内存】，不是打包磁盘体积；共享图集会各行重复计，别直接相加\n想降内存靠：压缩格式/降分辨率/关Mipmap(打图集主要降的是批次)"),
                EditorStyles.miniBoldLabel, GUILayout.Width(46));
            GUI.contentColor = item.raycastCount <= 8 ? new Color(0.4f, 0.8f, 0.45f) : item.raycastCount <= 20 ? new Color(0.95f, 0.7f, 0.2f) : new Color(0.9f, 0.35f, 0.3f);
            GUILayout.Label(new GUIContent($"射线{item.raycastCount}",
                    $"raycastTarget 处于【开启】状态的图形数：{item.raycastCount}\n纯装饰/不需要接收点击的，把 Image/Text 的 Raycast Target 取消勾选即可省输入射线开销"),
                EditorStyles.miniBoldLabel, GUILayout.Width(48));
            GUI.contentColor = mc0;
        }

        // 操作按钮
        EditorGUILayout.BeginHorizontal(GUILayout.Width(120));

        // 实测批次 "测"(Play模式下有效)
        Color mb0 = GUI.backgroundColor;
        if (EditorApplication.isPlaying) GUI.backgroundColor = new Color(0.29f, 0.55f, 0.95f);
        if (GUILayout.Button(new GUIContent("测", "运行(Play)模式下实测该界面真实批次"), GUILayout.Width(26), GUILayout.Height(25)))
        {
            StartMeasureBatches(item);
        }
        GUI.backgroundColor = mb0;



        // 在系统文件夹中定位 "📁"
        var cFolder = new GUIContent(UnityIconsEx.Folder) { tooltip = "在系统文件夹中定位该预制体" };
        if (GUILayout.Button(cFolder, GUILayout.Width(25), GUILayout.Height(25)))
        {
            EditorUtility.RevealInFinder(item.prefabPath);
        }

        // 打开预制体编辑 "🔍"
        var cPrefab = new GUIContent(UnityIconsEx.Prefab) { tooltip = "打开预制体(进入编辑模式)" };
        if (GUILayout.Button(cPrefab, GUILayout.Width(25), GUILayout.Height(25)))
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabPath));
        }

        // 在 Project 中选中定位 "📦"
        var cInfo = new GUIContent(UnityIconsEx.Info) { tooltip = "在 Project 窗口中选中并高亮" };
        if (GUILayout.Button(cInfo, GUILayout.Width(25), GUILayout.Height(25)))
        {
            OpenPrefab(item.prefabPath);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndHorizontal();

        GUI.color = rowColor0;   // 还原禁用置灰的整行 tint

        // 左侧色条：已确认=层级色；待确认=橙色警示
        if (Event.current.type == EventType.Repaint)
        {
            Color stripe = item.layerConfirmed ? LayerColor(item.uiLayer) : new Color(0.95f, 0.6f, 0.15f);
            EditorGUI.DrawRect(new Rect(rowRect.x + 2, rowRect.y + 1, 3, rowRect.height - 2), stripe);
        }
    }

    // 顶部"待确认层级"独立模块：常驻、高标题、大字，带边框，和下面层级明显分开。
    // 有待确认=橙色警示；无=平静绿 + 引导语(养成"新增预制体就点扫描"的习惯)。
    void DrawPendingSection(List<UIItem> pending)
    {
        bool has = pending.Count > 0;
        Color mark = has ? new Color(0.95f, 0.60f, 0.15f)    // 橙:有待确认
                         : new Color(0.40f, 0.70f, 0.45f);   // 绿:一切就绪

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);   // 外框 → 独立模块感

        // 高标题条(~42px) + 大字
        Rect head = GUILayoutUtility.GetRect(0, 42, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(head, new Color(mark.r, mark.g, mark.b, 0.16f));                 // 淡色底
        EditorGUI.DrawRect(new Rect(head.x, head.y, 4, head.height), mark);                 // 左侧粗色条
        EditorGUI.DrawRect(new Rect(head.x + 16, head.y + head.height / 2 - 5, 10, 10), mark); // 圆点

        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
        var subStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft, wordWrap = false };
        titleStyle.normal.textColor = mark;

        string title = has ? $"⚠  待确认层级（{pending.Count}）" : "✓  待确认层级（0）";
        string sub = has ? "自动识别的层级，请核对后点 ✓ 或改下拉确认"
                         : "新增了 UI 预制体？点上方【扫描】，新识别的会出现在这里";
        GUI.Label(new Rect(head.x + 36, head.y + 4, head.width - 44, 20), title, titleStyle);
        Color sc0 = GUI.contentColor; GUI.contentColor = new Color(1f, 1f, 1f, 0.75f);
        GUI.Label(new Rect(head.x + 36, head.y + 22, head.width - 44, 18), sub, subStyle);
        GUI.contentColor = sc0;

        if (has)
        {
            for (int i = 0; i < pending.Count; i++) DrawUIItem(pending[i], i);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(12);   // 和下面层级列表明显拉开距离
    }

    void UpdateItemLayer(UIItem item, UILayer oldLayer, UILayer newLayer)
    {
        // 从旧层级移除
        if (layerItems.ContainsKey(oldLayer))
        {
            layerItems[oldLayer].Remove(item);
        }

        // 添加到新层级（确保新层级存在）
        if (!layerItems.ContainsKey(newLayer))
        {
            layerItems[newLayer] = new List<UIItem>();
        }

        layerItems[newLayer].Add(item);

        // 确保层级展开状态存在
        string layerKey = $"layer_{newLayer}";
        if (!layerFoldoutStates.ContainsKey(layerKey))
            layerFoldoutStates[layerKey] = true;

        // 更新显示
        Repaint();
    }

    void OpenPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
    }

    void DrawBottomButtons()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        Color oldBg = GUI.backgroundColor;

        // 保存按钮"💾 保存配置"（绿色主色）
        GUI.backgroundColor = new Color(0.30f, 0.70f, 0.38f);
        if (GUILayout.Button(UnityIconsEx.SaveContent("保存配置"), GUILayout.Width(110), GUILayout.Height(30)))
        {
            SaveConfig();
            ShowNotification(new GUIContent("💾 已保存 UI 配置"));
        }

        // 生成路径常量按钮 "🚀 生成路径常量"（蓝色强调）
        GUI.backgroundColor = new Color(0.29f, 0.55f, 0.95f);
        if (GUILayout.Button(UnityIconsEx.StarContent("生成路径常量"), GUILayout.Width(130), GUILayout.Height(30)))
        {
            GenerateUIPathConstants();
        }

        GUI.backgroundColor = oldBg;
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();
    }

    void ScanUIComponents()
    {
        // 保存当前的UIItems以便合并
        List<UIItem> existingItems = new List<UIItem>(allUIItems);

        // 清空临时数据
        List<UIItem> scannedItems = new List<UIItem>();
        layerItems.Clear();
        layerFoldoutStates.Clear();

        // 初始化所有层级
        InitializeAllLayers();

        List<string> allPrefabPaths = new List<string>();

        // 1. 扫描所有自定义路径
        foreach (string scanPath in customScanPaths)
        {
            if (!Directory.Exists(scanPath)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { scanPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!allPrefabPaths.Contains(path))
                    allPrefabPaths.Add(path);
            }
        }

        // 2. 创建字典存储现有配置（按GUID和路径）
        Dictionary<string, UIItem> existingItemsByGuid = existingItems.ToDictionary(item => item.guid);
        Dictionary<string, UIItem> existingItemsByPath = existingItems.ToDictionary(item => item.prefabPath);

        // 3. 处理每个预制体（带进度条）
        int _scanTotal = allPrefabPaths.Count;
        int _scanIdx = 0;
        try
        {
        foreach (string path in allPrefabPaths)
        {
            _scanIdx++;
            EditorUtility.DisplayProgressBar("扫描 UI 预制体",
                $"({_scanIdx}/{_scanTotal})  {Path.GetFileName(path)}",
                _scanTotal == 0 ? 1f : (float)_scanIdx / _scanTotal);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // 检查是否有Canvas或UI组件
            UIPanelBehaviour uIPanelBehaviour = prefab.GetComponentInChildren<UIPanelBehaviour>(true);
            if (uIPanelBehaviour == null)
                continue;
            RectTransform rectTransform = prefab.GetComponentInChildren<RectTransform>(true);
            if (rectTransform == null) continue;

            // 获取预制体GUID
            string guid = AssetDatabase.AssetPathToGUID(path);

            // 检查是否已存在配置
            UIItem existingItem = null;
            if (existingItemsByGuid.TryGetValue(guid, out existingItem) ||
                existingItemsByPath.TryGetValue(path, out existingItem))
            {
                // 使用现有配置（保留层级、激活状态等设置）
                AnalyzeUIStat(existingItem, prefab);   // 刷新静态体检
                scannedItems.Add(existingItem);
                AddItemToLayer(existingItem);
                continue;
            }

            // 获取文件夹信息（GUI下的相对路径）
            string folderPath = Path.GetDirectoryName(path).Replace("Assets/Project/Prefabs/GUI", "");
            string guiRelativeFolderPath = folderPath;

            // 尝试提取GUI下的相对路径
            string[] pathParts = folderPath.Split('/');
            for (int i = 0; i < pathParts.Length; i++)
            {
                if (pathParts[i].Equals("GUI", System.StringComparison.OrdinalIgnoreCase))
                {
                    // 找到GUI目录，提取其后面的部分
                    guiRelativeFolderPath = string.Join("/", pathParts.Skip(i));
                    break;
                }
            }

            // 创建新的UI项
            UIItem item = new UIItem
            {
                guid = guid,
                prefabPath = path,
                prefabName = Path.GetFileNameWithoutExtension(path),
                folderPath = guiRelativeFolderPath,      // GUI下的相对路径
                fullFolderPath = folderPath,             // 完整文件夹路径
                // 新发现的UI默认使用normalRoot层级
                uiLayer = DetectUILayer(prefab, path),
                sortOrder = 0,
                layerConfirmed = false   // 自动识别的，进"待确认区"让用户过目
            };

            // 如果没有检测到明确层级，使用MainLayer作为默认
            if (item.uiLayer == UILayer.MainLayer)
            {
                // 可以在这里添加更多默认层级判断逻辑
            }

            AnalyzeUIStat(item, prefab);   // 静态体检
            scannedItems.Add(item);
            AddItemToLayer(item);
        }
        }
        finally
        {
            EditorUtility.ClearProgressBar();   // 保证异常也清掉进度条，不卡死编辑器
        }

        // 4. 更新数据
        allUIItems = scannedItems;

        FilterUIItems();

        int newCount = allUIItems.Count - existingItems.Count;
        Debug.Log($"扫描完成，找到 {allUIItems.Count} 个UI预制体（新发现 {newCount} 个）");

        // 5. 自动保存配置
        SaveConfig();

        // 6. 完成弹窗：明确告诉用户结果，避免只打log被忽略("像点了没反应")
        EditorUtility.DisplayDialog("扫描完成",
            $"共找到 {allUIItems.Count} 个 UI 预制体\n" +
            $"其中新发现 {newCount} 个\n\n" +
            $"配置已自动保存到 Resources/UIConfig.json",
            "确定");
    }

    void AddItemToLayer(UIItem item)
    {
        // 确保层级字典存在
        if (!layerItems.ContainsKey(item.uiLayer))
        {
            layerItems[item.uiLayer] = new List<UIItem>();
        }

        // 添加项目
        layerItems[item.uiLayer].Add(item);

        // 确保层级展开状态存在
        string layerKey = $"layer_{item.uiLayer}";
        if (!layerFoldoutStates.ContainsKey(layerKey))
            layerFoldoutStates[layerKey] = true;
    }

    UILayer DetectUILayer(GameObject prefab, string path)
    {
        string name = prefab.name.ToLower();
        string folder = Path.GetDirectoryName(path).ToLower();

        // 将路径分割为各级目录名，用于精确段匹配（避免 "load" 匹配 "download" 这类误判）
        var segments = new HashSet<string>(folder.Replace('\\', '/').Split('/'), StringComparer.OrdinalIgnoreCase);
        bool HasSeg(string s) => segments.Contains(s);

        // 第一优先级：名称或文件夹含完整枚举名
        if (name.Contains("worldscene")   || HasSeg("worldscene"))   return UILayer.WorldScene;
        if (name.Contains("worldobject")  || HasSeg("worldobject"))  return UILayer.WorldObject;
        if (name.Contains("worldeffect")  || HasSeg("worldeffect"))  return UILayer.WorldEffect;
        if (name.Contains("draglayer")    || HasSeg("draglayer"))    return UILayer.DragLayer;
        if (name.Contains("mainlayer")    || HasSeg("mainlayer"))    return UILayer.MainLayer;
        if (name.Contains("screenlayer")  || HasSeg("screenlayer"))  return UILayer.ScreenLayer;
        if (name.Contains("modallayer")   || HasSeg("modallayer"))   return UILayer.ModalLayer;
        if (name.Contains("poplayer")     || HasSeg("poplayer"))     return UILayer.PopLayer;
        if (name.Contains("guidelayer")   || HasSeg("guidelayer"))   return UILayer.GuideLayer;
        if (name.Contains("toplayer")     || HasSeg("toplayer"))     return UILayer.TopLayer;
        if (name.Contains("loadinglayer") || HasSeg("loadinglayer")) return UILayer.LoadingLayer;
        if (name.Contains("cursorlayer")  || HasSeg("cursorlayer"))  return UILayer.CursorLayer;

        // 第二优先级：按文件夹段名模糊匹配（精确到段，防止误判）
        if (HasSeg("world")    || HasSeg("scene"))     return UILayer.WorldScene;
        if (HasSeg("object")   || HasSeg("character")) return UILayer.WorldObject;
        if (HasSeg("effect")   || HasSeg("damage"))    return UILayer.WorldEffect;
        if (HasSeg("drag")     || HasSeg("follow"))    return UILayer.DragLayer;
        if (HasSeg("pop")      || HasSeg("dialog"))    return UILayer.PopLayer;
        if (HasSeg("modal")    || HasSeg("mask"))      return UILayer.ModalLayer;
        if (HasSeg("guide")    || HasSeg("tutorial"))  return UILayer.GuideLayer;
        if (HasSeg("top")      || HasSeg("notice"))    return UILayer.TopLayer;
        if (HasSeg("loading")  || HasSeg("load"))      return UILayer.LoadingLayer;
        if (HasSeg("cursor")   || HasSeg("mouse"))     return UILayer.CursorLayer;

        return UILayer.MainLayer; // 默认主界面层
    }

    // 静态体检：数图形/材质/纹理/Mask，粗估批次。预制体已加载(扫描时调)，不额外开销。
    // 注意：这是"静态预估"，合批的运行时因素(渲染顺序/图集打包)算不到，精确值请用运行时测。
    void AnalyzeUIStat(UIItem item, GameObject prefab)
    {
        if (prefab == null) return;
        var graphics = prefab.GetComponentsInChildren<Graphic>(true);

        item.graphicCount = graphics.Length;
        item.imageCount = graphics.Count(g => g is Image || g is RawImage);
        item.textCount = graphics.Count(g => g.GetType().Name.Contains("Text")); // UI.Text + TMP 都含"Text"
        item.maskCount = prefab.GetComponentsInChildren<Mask>(true).Length
                       + prefab.GetComponentsInChildren<RectMask2D>(true).Length;
        item.materialCount = graphics.Where(g => g != null).Select(g => g.material).Distinct().Count();
        item.textureCount = graphics.Select(g => g.mainTexture).Where(t => t != null).Distinct().Count();

        // 预估批次(粗算)：不同(材质×纹理)组合 + 每个Mask打断算+1。仅参考。
        int matTex = graphics.Where(g => g.enabled)
                             .Select(g => (g.material, g.mainTexture))
                             .Distinct().Count();
        item.estBatches = Mathf.Max(1, matTex) + item.maskCount;

        // 射线目标数：开了 raycastTarget 的图形(纯装饰却开着=浪费，可关)
        item.raycastCount = graphics.Count(g => g.raycastTarget);

        // 依赖纹理内存(加载后≈占多少RAM)：对预制体所有纹理依赖求运行时内存
        long mem = 0;
        foreach (var dep in AssetDatabase.GetDependencies(item.prefabPath, true))
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(dep);
            if (tex != null) mem += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
        }
        item.memBytes = mem;

        item.analyzed = true;
    }

    // ===== A. 运行时实测批次 =====
    // UnityStats.batches 只在 Play 模式、Game 视图渲染后有效，且是"整帧"总数。
    // 所以做法：记录加载前基准 → 实例化预制体 → 等几帧渲染 → 读差值 ≈ 该界面贡献的批次。
    // 想更纯净：在一个尽量空的场景里进 Play 再测。
    private UIItem _measItem;
    private GameObject _measRoot;
    private int _measBase;
    private int _measFrames;

    void StartMeasureBatches(UIItem item)
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("需要运行模式",
                "实测真实批次要在 Play 模式(▶)下进行。\n\n请先点 Unity 顶部的 ▶ 进入运行模式(建议用一个尽量空的场景)，再点【测】。",
                "知道了");
            return;
        }
        // 自愈：若上次测量卡住了，先清干净再开始(不再永久卡死)
        CleanupMeasure();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabPath);
        if (prefab == null) { Debug.LogError($"[UI测批次] 加载失败: {item.prefabPath}"); return; }

        try
        {
            Debug.Log($"[UI测批次] 开始测量: {item.prefabName}");
            _measBase = UnityStats.batches;  // 加载前基准(上一帧)

            var canvasGo = new GameObject("__UIBatchTest__");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform);
            Canvas.ForceUpdateCanvases();

            _measItem = item;
            _measRoot = canvasGo;
            _measFrames = 4;                 // 等几帧让它真正渲染出来
            EditorApplication.update += MeasureTick;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UI测批次] 启动失败: {e}");
            CleanupMeasure();
        }
    }

    void MeasureTick()
    {
        try
        {
            _measFrames--;
            Debug.Log($"[UI测批次] tick 剩 {_measFrames} 帧");
            if (_measFrames > 0) { Repaint(); return; }

            int after = UnityStats.batches;
            if (_measItem != null)
            {
                _measItem.realBatches = Mathf.Max(0, after - _measBase);
                Debug.Log($"[UI测批次] {_measItem.prefabName} 实测 = {_measItem.realBatches} (基准{_measBase} → {after})");
                ShowNotification(new GUIContent($"实测 {_measItem.prefabName} = {_measItem.realBatches} 批"));
                MarkDirty();
            }
            CleanupMeasure();
            Repaint();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UI测批次] 测量出错: {e}");
            CleanupMeasure();
        }
    }

    // 清理测量状态(退订/销毁临时对象/复位)，可安全重复调用 → 自愈防卡死
    void CleanupMeasure()
    {
        EditorApplication.update -= MeasureTick;
        if (_measRoot != null) UnityEngine.Object.DestroyImmediate(_measRoot);
        _measItem = null;
        _measRoot = null;
    }

    // 预估批次的颜色(体检红绿灯)
    Color EstBatchColor(int est)
    {
        if (est <= 8)  return new Color(0.40f, 0.80f, 0.45f); // 绿:轻
        if (est <= 16) return new Color(0.95f, 0.70f, 0.20f); // 黄:中
        return new Color(0.90f, 0.35f, 0.30f);                // 红:重
    }

    void FilterUIItems()
    {
        // 清空当前分组
        foreach (var layer in layerItems.Keys.ToList())
        {
            layerItems[layer].Clear();
        }

        // 重新添加所有符合条件的项目
        var filteredItems = allUIItems.Where(item =>
            (string.IsNullOrEmpty(searchText) ||
             item.prefabName.Contains(searchText, System.StringComparison.OrdinalIgnoreCase) ||
             item.folderPath.Contains(searchText, System.StringComparison.OrdinalIgnoreCase) ||
             item.prefabPath.Contains(searchText, System.StringComparison.OrdinalIgnoreCase)) &&
            (!showOnlyActive || item.isActive) &&
           (!_filterByLayer || item.uiLayer == filterLayer)
        );

        foreach (var item in filteredItems)
        {
            AddItemToLayer(item);
        }

        Repaint();
    }

    void SetAllActive(bool active)
    {
        foreach (var item in allUIItems)
        {
            item.isActive = active;
        }
        FilterUIItems();
    }

    void ResetAllLayers()
    {
        // 清空分组
        foreach (var layer in layerItems.Keys.ToList())
        {
            layerItems[layer].Clear();
        }

        foreach (var item in allUIItems)
        {
            item.uiLayer = DetectUILayer(
                AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabPath),
                item.prefabPath
            );
            AddItemToLayer(item);
        }

        FilterUIItems();
    }

    void AutoSortByLayer()
    {
        // 为每个层级的UI分配连续排序值
        var layerGroups = allUIItems.GroupBy(i => i.uiLayer)
                                   .OrderBy(g => (int)g.Key);

        int baseOrder = 0;
        foreach (var group in layerGroups)
        {
            int orderInGroup = 0;
            foreach (var item in group.OrderBy(i => i.prefabName))
            {
                item.sortOrder = baseOrder + orderInGroup * 10;
                orderInGroup++;
            }
            baseOrder += 1000;
        }

        // 重新分组以确保正确显示
        foreach (var layer in layerItems.Keys.ToList())
        {
            layerItems[layer].Clear();
        }

        foreach (var item in allUIItems)
        {
            AddItemToLayer(item);
        }

        FilterUIItems();
    }

    string GetLayerName(UILayer layer)
    {
        switch (layer)
        {
            case UILayer.WorldScene: return "世界场景层";
            case UILayer.WorldObject: return "世界物体层";
            case UILayer.WorldEffect: return "世界特效层";
            case UILayer.DragLayer: return "拖拽层";
            case UILayer.MainLayer: return "主界面层";
            case UILayer.ScreenLayer: return "全屏界面层";
            case UILayer.ModalLayer: return "模态弹窗层";
            case UILayer.PopLayer: return "普通弹窗层";
            case UILayer.GuideLayer: return "新手引导层";
            case UILayer.TopLayer: return "顶层通知";
            case UILayer.LoadingLayer: return "加载界面层";
            case UILayer.CursorLayer: return "鼠标光标层";
            default: return layer.ToString();
        }
    }

    // 每个层级一个主题色（色条/圆点/统计chip 用），让面板一眼分区
    Color LayerColor(UILayer layer)
    {
        switch (layer)
        {
            case UILayer.WorldScene:   return new Color(0.30f, 0.72f, 0.65f);
            case UILayer.WorldObject:  return new Color(0.40f, 0.60f, 0.85f);
            case UILayer.WorldEffect:  return new Color(0.55f, 0.75f, 0.40f);
            case UILayer.DragLayer:    return new Color(0.60f, 0.60f, 0.66f);
            case UILayer.MainLayer:    return new Color(0.29f, 0.62f, 1.00f); // 蓝
            case UILayer.ScreenLayer:  return new Color(0.18f, 0.83f, 0.75f); // 青
            case UILayer.ModalLayer:   return new Color(0.65f, 0.55f, 0.98f); // 紫
            case UILayer.PopLayer:     return new Color(0.97f, 0.47f, 0.73f); // 粉
            case UILayer.GuideLayer:   return new Color(0.22f, 0.77f, 0.81f); // 青蓝
            case UILayer.TopLayer:     return new Color(0.49f, 0.55f, 1.00f); // 靛
            case UILayer.LoadingLayer: return new Color(0.94f, 0.53f, 0.24f); // 橙
            case UILayer.CursorLayer:  return new Color(0.55f, 0.58f, 0.62f); // 灰
            default:                   return new Color(0.55f, 0.58f, 0.62f);
        }
    }

    // ========== 配置保存和加载 ==========

    void SaveConfig()
    {
        UIConfigData config = new UIConfigData
        {
            uiItems = allUIItems,
            scanPaths = customScanPaths,
            exportTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            totalCount = allUIItems.Count
        };

        string json = JsonUtility.ToJson(config, true);

        // 保存到Resources目录
        string resourcesPath = Application.dataPath + "/Resources";
        if (!Directory.Exists(resourcesPath))
            Directory.CreateDirectory(resourcesPath);

        File.WriteAllText(UI_CONFIG_FILE, json);
        AssetDatabase.Refresh();

        ClearDirty();   // 已保存，清掉标题的 *
        Debug.Log($"配置已保存: {UI_CONFIG_FILE}");
    }

    // notify=true 时手动点按钮，弹轻量浮层反馈；自动加载(OnEnable/ShowWindow)传 false 不打扰
    void LoadConfig(bool notify = false)
    {
        if (!File.Exists(UI_CONFIG_FILE))
        {
            Debug.LogWarning("配置文件不存在，请先扫描并保存配置");
            if (notify) ShowNotification(new GUIContent("⚠ 无配置文件，请先扫描并保存"));
            return;
        }

        try
        {
            string json = File.ReadAllText(UI_CONFIG_FILE);
            UIConfigData config = JsonUtility.FromJson<UIConfigData>(json);

            // 更新数据
            allUIItems = config.uiItems ?? new List<UIItem>();
            customScanPaths = config.scanPaths ?? new List<string>();

            // 已存进配置的都算"已确认"(既兼容旧json无此字段，也符合"存过=你确认过")
            foreach (var it in allUIItems) it.layerConfirmed = true;

            // 重新分组
            layerItems.Clear();
            layerFoldoutStates.Clear();

            // 初始化所有层级
            InitializeAllLayers();

            foreach (var item in allUIItems)
            {
                AddItemToLayer(item);
            }

            FilterUIItems();
            Debug.Log($"配置加载完成: {allUIItems.Count} 个UI项");
            if (notify) ShowNotification(new GUIContent($"✅ 已加载 {allUIItems.Count} 个 UI"));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载配置失败: {e.Message}");
            if (notify) ShowNotification(new GUIContent("❌ 加载失败，详见 Console"));
        }
    }
    void GenerateUIPathConstants()
    {
        if (allUIItems.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "没有可生成的UI数据，请先扫描UI", "确定");
            return;
        }

        // 重名校验：变量名(=预制体名)重复会让生成的常量类出现同名 const → 编译报错。先拦下。
        var dupGroups = allUIItems
            .Where(i => i.isActive)
            .GroupBy(i => GetVariableName(i.prefabName))
            .Where(g => g.Count() > 1)
            .ToList();
        if (dupGroups.Count > 0)
        {
            var sbDup = new StringBuilder("以下预制体重名，会导致生成的常量类编译报错，请先改名后再生成：\n");
            foreach (var g in dupGroups)
            {
                sbDup.AppendLine($"\n【{g.Key}】×{g.Count()}");
                foreach (var it in g) sbDup.AppendLine($"    {it.prefabPath}");
            }
            Debug.LogError($"[UI管理器] 生成中止：存在重名\n{sbDup}");
            EditorUtility.DisplayDialog("发现重名，已中止生成", sbDup.ToString(), "确定");
            return;
        }

        StringBuilder sb = new StringBuilder();

        // 文件头部
        sb.AppendLine("// ===========================================");
        sb.AppendLine("// 自动生成的UI路径常量类");
        sb.AppendLine("// 生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("// 总数量: " + allUIItems.Count);
        sb.AppendLine("// ===========================================");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("public static class UIPathConstants");
        sb.AppendLine("{");
        sb.AppendLine();

        // 添加UILayer枚举定义在最前面
        sb.AppendLine("    // ===== UI层级定义 ===== //");
        sb.AppendLine("    public enum UILayer");
        sb.AppendLine("    {");
        foreach (var layer in allLayers)
        {
            string layerName = GetLayerName(layer);
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// {layerName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        {layer} = {(int)layer},");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // 按层级分组
        var groupedByLayer = allUIItems
            .Where(item => item.isActive)
            .GroupBy(item => item.uiLayer)
            .OrderBy(group => (int)group.Key);

        int constantIndex = 0;

        // 准备字典数据
        var uiPathDictEntries = new List<string>();
        var uiLayerDictEntries = new List<string>();

        foreach (var layerGroup in groupedByLayer)
        {
            // 层级注释
            string layerName = GetLayerName(layerGroup.Key);
            sb.AppendLine($"    // ===== {layerName} ({layerGroup.Key}) ===== //");
            sb.AppendLine();

            // 该层级下的所有UI
            foreach (var item in layerGroup.OrderBy(i => i.sortOrder).ThenBy(i => i.prefabName))
            {
                // 生成变量名（保持与预制体名称相同，移除文件扩展名）
                string variableName = GetVariableName(item.prefabName);
                string enumName = item.uiLayer.ToString();

                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// {item.prefabName}");
                sb.AppendLine($"    /// 路径: {item.prefabPath}");
                sb.AppendLine($"    /// 层级: {layerName}");
                sb.AppendLine($"    /// 层级值: {(int)item.uiLayer}");
                sb.AppendLine($"    /// </summary>");

                // 路径常量 - 使用 预制体名_UIPanel 格式
                sb.AppendLine($"    public const string {variableName}_UIPanel = \"{item.prefabPath}\";");

                // 层级常量 - 使用 预制体名_UIlayer 格式
                sb.AppendLine($"    public const int {variableName}_UIlayer = {(int)item.uiLayer};");

                sb.AppendLine();

                // 添加字典条目
                uiPathDictEntries.Add($"            {{ \"{variableName}\", {variableName}_UIPanel }}");
                uiLayerDictEntries.Add($"            {{ \"{variableName}\", UILayer.{enumName} }}");

                constantIndex++;
            }

            sb.AppendLine();
        }

        // 生成字典部分
        sb.AppendLine("    // ===== UI路径字典 ===== //");
        sb.AppendLine("    public static readonly Dictionary<string, string> UIPathDictionary = new Dictionary<string, string>()");
        sb.AppendLine("    {");
        sb.AppendLine(string.Join(",\n", uiPathDictEntries));
        sb.AppendLine("    };");
        sb.AppendLine();

        sb.AppendLine("    // ===== UI层级字典 ===== //");
        sb.AppendLine("    public static readonly Dictionary<string, UILayer> UILayerDictionary = new Dictionary<string, UILayer>()");
        sb.AppendLine("    {");
        sb.AppendLine(string.Join(",\n", uiLayerDictEntries));
        sb.AppendLine("    };");
        sb.AppendLine();

        sb.AppendLine("}");

        // 写入文件
        string directory = Path.GetDirectoryName(UI_PATH_CONSTANTS_FILE);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(UI_PATH_CONSTANTS_FILE, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"UI路径常量生成完成！");
        Debug.Log($"文件路径: {UI_PATH_CONSTANTS_FILE}");
        Debug.Log($"生成常量数量: {constantIndex}");
        Debug.Log($" 生成字典: UIPathDictionary (数量: {uiPathDictEntries.Count}), UILayerDictionary (数量: {uiLayerDictEntries.Count})");

        EditorUtility.DisplayDialog("生成成功",
            $"UI路径常量生成完成！\n\n" +
            $"文件: {UI_PATH_CONSTANTS_FILE}\n" +
            $"生成常量数量: {constantIndex}\n" +
            $"生成字典: UIPathDictionary ({uiPathDictEntries.Count}项)\n" +
            $"        UILayerDictionary ({uiLayerDictEntries.Count}项)\n" +
            $"生成时间: {System.DateTime.Now:HH:mm:ss}",
            "确定");
    }

    // 辅助函数：获取变量名（保持与预制体名称相同，移除文件扩展名）
    string GetVariableName(string prefabName)
    {
        // 移除 .prefab 扩展名
        if (prefabName.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        {
            prefabName = prefabName.Substring(0, prefabName.Length - 7);
        }

        // 保持原样，不大写化
        return prefabName;
    }

}
#endif
