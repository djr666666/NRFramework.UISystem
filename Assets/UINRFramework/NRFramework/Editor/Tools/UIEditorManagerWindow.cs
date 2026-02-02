#if UNITY_EDITOR
using NRFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class UIEditorManagerWindow : EditorWindow
{

    // 预览相关
    private Dictionary<string, Texture2D> prefabPreviews = new Dictionary<string, Texture2D>();
    private string previewPrefabPath = null;
    private bool showPreviewWindow = false;
    private Vector2 previewScrollPosition;
    private Vector2 previewWindowSize = new Vector2(400, 600);
    private float previewScale = 1.0f;


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
    private bool showSettings = false;

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
    private const string UI_PREFABS_PATH = "Assets/Prefabs";

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
            // TODDO 配置默认路径（修改为GUI文件夹）
            customScanPaths = new List<string>
            {
                UI_PREFABS_PATH,
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
            LoadConfig();
        }

        // 设置按钮"⚙️ 设置"
        showSettings = GUILayout.Toggle(showSettings, UnityIconsEx.SettingsContent("设置"), EditorStyles.toolbarButton, GUILayout.Width(100), GUILayout.Height(50));

        // 搜索框
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
        string newSearchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
        if (newSearchText != searchText)
        {
            searchText = newSearchText;
            FilterUIItems();
        }

        // 层级过滤
        EditorGUILayout.LabelField("层级:", GUILayout.Width(30));
        filterLayer = (UILayer)EditorGUILayout.EnumPopup(filterLayer, EditorStyles.toolbarDropDown, GUILayout.Width(100));

        // 显示已激活
        showOnlyActive = GUILayout.Toggle(showOnlyActive, "仅激活", EditorStyles.toolbarButton);

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

            //"🗑️"
            if (GUILayout.Button(UnityIconsEx.Delete, GUILayout.Width(30)))
            {
                customScanPaths.RemoveAt(i);
                break;
            }
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
            //TODO
            customScanPaths = new List<string> { UI_PREFABS_PATH };  //自己配置
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

        // 统计每个层级的数量
        var layerStats = new List<string>();
        foreach (var layer in allLayers)
        {
            int count = 0;
            if (layerItems.ContainsKey(layer))
            {
                count = layerItems[layer].Count;
            }
            layerStats.Add($"{GetLayerName(layer)}:{count}");
        }

        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("📊 统计:", EditorStyles.boldLabel);
        GUILayout.Label($"总数: {total}", GUILayout.Width(70));
        GUILayout.Label($"激活: {active}", GUILayout.Width(70));
        GUILayout.Label($"层级: {string.Join(" ", layerStats)}", GUILayout.ExpandWidth(true));
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

                // 层级标题 - 显示中文名称、层级值和数量
                int itemCount = items.Count;
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                layerFoldoutStates[layerKey] = EditorGUILayout.Foldout(
                    layerFoldoutStates[layerKey],
                    $" Layer  =  {layer}  --- {itemCount}个UI",
                    true
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
                    if (items.Count == 0)
                    {
                        // 如果该层级下没有UI，显示提示信息
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        EditorGUILayout.HelpBox($"该层级下暂无UI预制体", MessageType.Info);
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        // 按排序值和名称排序
                        var sortedItems = items
                            .OrderBy(i => i.sortOrder)
                            .ThenBy(i => i.prefabName)
                            .ToList();

                        foreach (var item in sortedItems)
                        {
                            DrawUIItem(item);
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawUIItem(UIItem item)
    {
        EditorGUILayout.BeginHorizontal("box");

        // 激活开关
        bool newActive = EditorGUILayout.Toggle(item.isActive, GUILayout.Width(20));
        if (newActive != item.isActive)
        {
            item.isActive = newActive;
        }

        // 预制体名称（可点击）
        GUIContent nameContent = new GUIContent(item.prefabName,
            $"\n     层级: {item.uiLayer}\n     排序: {item.sortOrder} \n     完整路径:      {item.prefabPath}");

        if (GUILayout.Button(nameContent, EditorStyles.label, GUILayout.Width(200)))
        {
            OpenPrefab(item.prefabPath);
        }

        // 层级选择 - 这里会触发重新分组
        EditorGUILayout.LabelField("层级:", GUILayout.Width(35));
        UILayer oldLayer = item.uiLayer;
        UILayer newLayer = (UILayer)EditorGUILayout.EnumPopup(item.uiLayer, GUILayout.Width(100));
        if (newLayer != oldLayer)
        {
            item.uiLayer = newLayer;
            // 层级改变，重新分组
            UpdateItemLayer(item, oldLayer, newLayer);
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

        // 完整路径显示（包含预制体本身）
        EditorGUILayout.LabelField(item.prefabPath, EditorStyles.label, GUILayout.ExpandWidth(true));

        // 操作按钮
        EditorGUILayout.BeginHorizontal(GUILayout.Width(90));



        // 打开文件夹路径 "📁"
        if (GUILayout.Button(UnityIconsEx.Folder, GUILayout.Width(25), GUILayout.Height(25)))
        {
            EditorUtility.RevealInFinder(item.prefabPath);
        }

        // 编辑预制体 "🔍"
        if (GUILayout.Button(UnityIconsEx.Prefab, GUILayout.Width(25), GUILayout.Height(25)))
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabPath));
        }

        // 打开预制体"📦"
        if (GUILayout.Button(UnityIconsEx.Info, GUILayout.Width(25), GUILayout.Height(25)))
        {
            OpenPrefab(item.prefabPath);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndHorizontal();
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

        // 保存按钮"💾 保存配置"
        if (GUILayout.Button(UnityIconsEx.SaveContent("保存配置"), GUILayout.Width(100), GUILayout.Height(30)))
        {
            SaveConfig();
            EditorUtility.DisplayDialog("保存成功", "UI配置已保存", "确定");
        }

        // 生成路径常量按钮 "🚀 生成路径常量"
        if (GUILayout.Button(UnityIconsEx.StarContent("生成路径常量"), GUILayout.Width(120), GUILayout.Height(30)))
        {
            GenerateUIPathConstants();
        }

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

        // 3. 处理每个预制体
        foreach (string path in allPrefabPaths)
        {
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
                sortOrder = 0
            };

            // 如果没有检测到明确层级，使用MainLayer作为默认
            if (item.uiLayer == UILayer.MainLayer)
            {
                // 可以在这里添加更多默认层级判断逻辑
            }

            scannedItems.Add(item);
            AddItemToLayer(item);
        }

        // 4. 更新数据
        allUIItems = scannedItems;

        FilterUIItems();
        Debug.Log($"扫描完成，找到 {allUIItems.Count} 个UI预制体（新发现 {allUIItems.Count - existingItems.Count} 个）");

        // 5. 自动保存配置（可选）
        SaveConfig();
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

        // 根据名称判断 - 更新为新的枚举名称
        if (name.Contains("worldscene") || folder.Contains("worldscene")) return UILayer.WorldScene;
        if (name.Contains("worldobject") || folder.Contains("worldobject")) return UILayer.WorldObject;
        if (name.Contains("worldeffect") || folder.Contains("worldeffect")) return UILayer.WorldEffect;
        if (name.Contains("draglayer") || folder.Contains("draglayer")) return UILayer.DragLayer;
        if (name.Contains("mainlayer") || folder.Contains("mainlayer")) return UILayer.MainLayer;
        if (name.Contains("screenlayer") || folder.Contains("screenlayer")) return UILayer.ScreenLayer;
        if (name.Contains("modallayer") || folder.Contains("modallayer")) return UILayer.ModalLayer;
        if (name.Contains("poplayer") || folder.Contains("poplayer")) return UILayer.PopLayer;
        if (name.Contains("guidelayer") || folder.Contains("guidelayer")) return UILayer.GuideLayer;
        if (name.Contains("toplayer") || folder.Contains("toplayer")) return UILayer.TopLayer;
        if (name.Contains("loadinglayer") || folder.Contains("loadinglayer")) return UILayer.LoadingLayer;
        if (name.Contains("cursorlayer") || folder.Contains("cursorlayer")) return UILayer.CursorLayer;

        // 根据文件夹路径判断
        if (folder.Contains("world") || folder.Contains("scene")) return UILayer.WorldScene;
        if (folder.Contains("object") || folder.Contains("character")) return UILayer.WorldObject;
        if (folder.Contains("effect") || folder.Contains("damage")) return UILayer.WorldEffect;
        if (folder.Contains("drag") || folder.Contains("follow")) return UILayer.DragLayer;
        if (folder.Contains("pop") || folder.Contains("dialog")) return UILayer.PopLayer;
        if (folder.Contains("modal") || folder.Contains("mask")) return UILayer.ModalLayer;
        if (folder.Contains("guide") || folder.Contains("tutorial")) return UILayer.GuideLayer;
        if (folder.Contains("top") || folder.Contains("notice")) return UILayer.TopLayer;
        if (folder.Contains("loading") || folder.Contains("load")) return UILayer.LoadingLayer;
        if (folder.Contains("cursor") || folder.Contains("mouse")) return UILayer.CursorLayer;

        return UILayer.MainLayer; // 默认使用主界面层
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
           (filterLayer == UILayer.MainLayer || item.uiLayer == filterLayer) //新的默认层级
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

        Debug.Log($"配置已保存: {UI_CONFIG_FILE}");
    }

    void LoadConfig()
    {
        if (!File.Exists(UI_CONFIG_FILE))
        {
            Debug.LogWarning("配置文件不存在，请先扫描并保存配置");
            return;
        }

        try
        {
            string json = File.ReadAllText(UI_CONFIG_FILE);
            UIConfigData config = JsonUtility.FromJson<UIConfigData>(json);

            // 更新数据
            allUIItems = config.uiItems ?? new List<UIItem>();
            customScanPaths = config.scanPaths ?? new List<string>();

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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载配置失败: {e.Message}");
        }
    }
    void GenerateUIPathConstants()
    {
        if (allUIItems.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "没有可生成的UI数据，请先扫描UI", "确定");
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