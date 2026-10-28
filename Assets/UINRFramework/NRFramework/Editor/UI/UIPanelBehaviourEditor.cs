// https://github.com/NRatel/NRFramework.UI

using System;
using UnityEditor;
using UnityEngine;

namespace NRFramework
{
    [CustomEditor(typeof(UIPanelBehaviour))]
    public class UIPanelBehaviourEditor : UIViewBehaviourEditor
    {
        // Inspector 标签：保留英文（方便对着 Inspector 直接去代码里搜同名字段），后接中文，
        // 鼠标悬停的 tooltip 里写清每个选项到底会发生什么。static readonly 避免每帧重建 GUIContent。
        static private readonly GUIContent kPanelTypeLabel = new GUIContent("UIPanelType 面板类型",
            "选一种预设类型，下面各项会自动按预设填好；想逐项手调请选 Custom");
        static private readonly GUIContent kHasBgLabel = new GUIContent("HasBg 是否有背景遮罩",
            "勾选后，本面板会成为背景遮罩(UIBlocker)的宿主。遮罩全局只有一个，挂在最上方勾选了本项的面板下");
        static private readonly GUIContent kBgShowTypeLabel = new GUIContent("BgShowType 背景显示方式",
            "遮罩的外观。Alpha=完全透明、只用来挡住下层点击；HalfAlphaBlack=半透明黑，常用于弹窗压暗背景");
        static private readonly GUIContent kCustomBgColorLabel = new GUIContent("CustomBgColor 自定义背景色",
            "BgShowType 选 CustomColor 时生效");
        static private readonly GUIContent kBgClickEventTypeLabel = new GUIContent("BgClickEventType 点击背景时",
            "点到背景遮罩(面板以外的区域)时的行为。PassThrough=点击穿到下层面板；DontRespond=挡住点击但什么都不做；CloseSelf/DestroySelf=关闭或销毁本面板；Custom=回调 OnBackgroundClicked");
        static private readonly GUIContent kGetFocusTypeLabel = new GUIContent("GetFocusType 焦点获取方式",
            "Get=独占焦点，其下方所有面板一律失焦；GetWithOthers=与上方面板共享焦点，继续往下找；DontGet=本面板从不获取焦点。焦点变化会回调 OnFocusChanged");
        static private readonly GUIContent kEscPressEventTypeLabel = new GUIContent("EscPressEventType 按返回键时",
            "DontCheck=本面板完全不参与返回键检测(返回键会落到下方面板)；DontRespond=参与检测、吃掉返回键但不做事；CloseSelf/DestroySelf=关闭或销毁自身；Custom=回调 OnEscButtonPressed");
        static private readonly GUIContent kThicknessLabel = new GUIContent("Thickness 层级厚度",
            "本面板预留的 sortingOrder 区间宽度。下一个面板的 order = 本面板 order + thickness + 1，留给界面内部的子层级使用");
        static private readonly GUIContent kOpenAnimPlayModeLabel = new GUIContent("OpenAnimPlayMode 打开动画播放方式",
            "AutoPlay=框架在打开时自动触发 Animator 的 open；ControlBySelf=框架不管，由界面自己播。需要物体上有 Animator 且已配置 Controller 才可选");
        static private readonly GUIContent kCloseAnimPlayModeLabel = new GUIContent("CloseAnimPlayMode 关闭动画播放方式",
            "AutoPlay=框架在关闭时自动触发 Animator 的 close，并等动画播完再销毁；ControlBySelf=框架不管，由界面自己播。需要物体上有 Animator 且已配置 Controller 才可选");

        private SerializedProperty m_PanelTypeSP;
        private SerializedProperty m_HasBgSP;
        private SerializedProperty m_BgShowTypeSP;
        private SerializedProperty m_CustomBgColorSP;
        private SerializedProperty m_BgClickEventTypeSP;
        private SerializedProperty m_GetFocusTypeSP;
        private SerializedProperty m_EscPressEventTypeSP;
        private SerializedProperty m_ThicknessSP;
        private SerializedProperty m_OpenAnimPlayModeSP;
        private SerializedProperty m_CloseAnimPlayModeSP;

        private int m_LastPanelTypeIndex = -1;       //记录上次选择，仅变化时重置其他子项

        protected override void OnEnable()
        {
            base.OnEnable();

            m_PanelTypeSP = serializedObject.FindProperty("m_PanelType");
            m_HasBgSP = serializedObject.FindProperty("m_HasBg");
            m_BgShowTypeSP = serializedObject.FindProperty("m_BgShowType");
            m_CustomBgColorSP = serializedObject.FindProperty("m_CustomBgColor");
            m_BgClickEventTypeSP = serializedObject.FindProperty("m_BgClickEventType");
            m_GetFocusTypeSP = serializedObject.FindProperty("m_GetFocusType");
            m_EscPressEventTypeSP = serializedObject.FindProperty("m_EscPressEventType");
            m_ThicknessSP = serializedObject.FindProperty("m_Thickness");
            m_OpenAnimPlayModeSP = serializedObject.FindProperty("m_OpenAnimPlayMode");
            m_CloseAnimPlayModeSP = serializedObject.FindProperty("m_CloseAnimPlayMode");

            m_LastPanelTypeIndex = m_PanelTypeSP.enumValueIndex;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawUIPanelSetting();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight / 4);
            DrawOpElementList();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight / 2);
            DrawExpoertButton();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawUIPanelSetting()
        {
            Enum panelTypeEnum = EditorGUILayout.EnumPopup(kPanelTypeLabel, (UIPanelType)m_PanelTypeSP.enumValueIndex);
            UIPanelType panelType = (UIPanelType)panelTypeEnum;
            m_PanelTypeSP.enumValueIndex = (int)panelType;

            EditorGUI.indentLevel++;
            {
                if (m_PanelTypeSP.enumValueIndex != m_LastPanelTypeIndex)
                {
                    SetWithDefault(panelType);
                    m_LastPanelTypeIndex = m_PanelTypeSP.enumValueIndex;
                }

                using (new EditorGUI.DisabledScope(panelType != UIPanelType.Custom))
                {
                    m_HasBgSP.boolValue = EditorGUILayout.Toggle(kHasBgLabel, m_HasBgSP.boolValue);
                    if (m_HasBgSP.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        Enum bgShowTypeEnum = EditorGUILayout.EnumPopup(kBgShowTypeLabel, (UIPanelBgShowType)m_BgShowTypeSP.enumValueIndex);
                        m_BgShowTypeSP.enumValueIndex = (int)(UIPanelBgShowType)bgShowTypeEnum;
                        if (m_BgShowTypeSP.enumValueIndex == (int)UIPanelBgShowType.CustomColor)
                        {
                            EditorGUI.indentLevel++;
                            m_CustomBgColorSP.colorValue = EditorGUILayout.ColorField(kCustomBgColorLabel, m_CustomBgColorSP.colorValue);
                            EditorGUI.indentLevel--;
                        }
                        Enum bgClickEventTypeEnum = EditorGUILayout.EnumPopup(kBgClickEventTypeLabel, (UIPanelBgClickEventType)m_BgClickEventTypeSP.enumValueIndex);
                        m_BgClickEventTypeSP.enumValueIndex = (int)(UIPanelBgClickEventType)bgClickEventTypeEnum;
                        EditorGUI.indentLevel--;
                    }

                    Enum getFocusTypeEnum = EditorGUILayout.EnumPopup(kGetFocusTypeLabel, (UIPanelGetFocusType)m_GetFocusTypeSP.enumValueIndex);
                    m_GetFocusTypeSP.enumValueIndex = (int)(UIPanelGetFocusType)getFocusTypeEnum;
                }

                //Debug.Log("m_EscPressEventTypeSP.enumValueIndex: " + m_EscPressEventTypeSP.enumValueIndex);

                Enum escPressEventTypeEnum = EditorGUILayout.EnumPopup(kEscPressEventTypeLabel, (UIPanelEscPressEventType)m_EscPressEventTypeSP.enumValueIndex);
                m_EscPressEventTypeSP.enumValueIndex = (int)(UIPanelEscPressEventType)escPressEventTypeEnum;

            }
            EditorGUI.indentLevel--;

            m_ThicknessSP.intValue = EditorGUILayout.IntField(kThicknessLabel, m_ThicknessSP.intValue);

            //这两个下拉框【始终可选】。以前在没有 Animator 时整个置灰成 NoValidAnimator，
            //会让人误以为「没挂 Animator 就不能有动画」——实际上重写 PlayOpenAnim/PlayCloseAnim
            //走 DOTween 之类的代码动画是完全可以的，那条路根本不读这个枚举。
            bool existValidAnimator = ((UIPanelBehaviour)target).ExistValidAnimator();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Animator 状态",
                    existValidAnimator ? "有效（可用 AutoPlay）" : "无有效 Animator（需组件存在+启用+Controller 不为空）");
            }

            Enum openAnimPlayModeEnum = EditorGUILayout.EnumPopup(kOpenAnimPlayModeLabel, (UIPanelOpenAnimPlayMode)m_OpenAnimPlayModeSP.enumValueIndex);
            m_OpenAnimPlayModeSP.enumValueIndex = (int)(UIPanelOpenAnimPlayMode)openAnimPlayModeEnum;
            Enum closeAnimPlayModeEnum = EditorGUILayout.EnumPopup(kCloseAnimPlayModeLabel, (UIPanelCloseAnimPlayMode)m_CloseAnimPlayModeSP.enumValueIndex);
            m_CloseAnimPlayModeSP.enumValueIndex = (int)(UIPanelCloseAnimPlayMode)closeAnimPlayModeEnum;

            bool anyAutoPlay = m_OpenAnimPlayModeSP.enumValueIndex == (int)UIPanelOpenAnimPlayMode.AutoPlay
                            || m_CloseAnimPlayModeSP.enumValueIndex == (int)UIPanelCloseAnimPlayMode.AutoPlay;
            bool anyControlBySelf = m_OpenAnimPlayModeSP.enumValueIndex == (int)UIPanelOpenAnimPlayMode.ControlBySelf
                                 || m_CloseAnimPlayModeSP.enumValueIndex == (int)UIPanelCloseAnimPlayMode.ControlBySelf;

            if (anyAutoPlay && !existValidAnimator)
            {
                EditorGUILayout.HelpBox(
                    "选了 AutoPlay，但当前没有可用的 Animator，框架会按「无动画」立即完成。\n" +
                    "· 想用 Animator：给本物体挂 Animator 并配好 Controller，状态机里需要有 open / close 两个 Trigger\n" +
                    "· 想用代码动画（DOTween 等）：改选 ControlBySelf，并重写 PlayOpenAnim / PlayCloseAnim",
                    MessageType.Warning);
            }

            if (anyControlBySelf)
            {
                EditorGUILayout.HelpBox(
                    "ControlBySelf：动画交由代码控制，需在面板脚本里重写 PlayOpenAnim / PlayCloseAnim。\n" +
                    "重写 PlayCloseAnim 时务必在动画结束时调用 onFinish —— 关闭流程的销毁动作就挂在这个回调上，漏调面板永远不会被销毁。",
                    MessageType.Info);
            }
        }

        private void SetWithDefault(UIPanelType panelType)
        {
            switch (panelType)
            {
                case UIPanelType.Underlay:
                    m_HasBgSP.boolValue = true;
                    m_BgShowTypeSP.enumValueIndex = (int)UIPanelBgShowType.Alpha;
                    m_BgClickEventTypeSP.enumValueIndex = (int)UIPanelBgClickEventType.DontRespond;
                    m_GetFocusTypeSP.enumValueIndex = (int)UIPanelGetFocusType.Get;
                    m_EscPressEventTypeSP.enumValueIndex = (int)UIPanelEscPressEventType.DontRespond;  //默认不响应
                    break;

                case UIPanelType.Overlay:
                    m_HasBgSP.boolValue = false;
                    m_GetFocusTypeSP.enumValueIndex = (int)UIPanelGetFocusType.GetWithOthers;
                    m_EscPressEventTypeSP.enumValueIndex = (int)UIPanelEscPressEventType.DontCheck;     //默认不检测
                    break;

                case UIPanelType.Window:
                    m_HasBgSP.boolValue = true;
                    m_BgShowTypeSP.enumValueIndex = (int)UIPanelBgShowType.HalfAlphaBlack;
                    m_BgClickEventTypeSP.enumValueIndex = (int)UIPanelBgClickEventType.CloseSelf;
                    m_GetFocusTypeSP.enumValueIndex = (int)UIPanelGetFocusType.Get;
                    m_EscPressEventTypeSP.enumValueIndex = (int)UIPanelEscPressEventType.CloseSelf;     //默认关闭自身
                    break;

                case UIPanelType.ModalWindow:
                    m_HasBgSP.boolValue = true;
                    m_BgShowTypeSP.enumValueIndex = (int)UIPanelBgShowType.HalfAlphaBlack;
                    m_BgClickEventTypeSP.enumValueIndex = (int)UIPanelBgClickEventType.DontRespond;
                    m_GetFocusTypeSP.enumValueIndex = (int)UIPanelGetFocusType.Get;
                    m_EscPressEventTypeSP.enumValueIndex = (int)UIPanelEscPressEventType.DontRespond;   //默认不响应
                    break;

                case UIPanelType.Float:
                    m_HasBgSP.boolValue = false;
                    m_GetFocusTypeSP.enumValueIndex = (int)UIPanelGetFocusType.DontGet;
                    m_EscPressEventTypeSP.enumValueIndex = (int)UIPanelEscPressEventType.DontCheck;     //默认不检测
                    break;

                case UIPanelType.System:
                    m_HasBgSP.boolValue = true;
                    m_BgShowTypeSP.enumValueIndex = (int)UIPanelBgShowType.HalfAlphaBlack;
                    m_BgClickEventTypeSP.enumValueIndex = (int)UIPanelBgClickEventType.DontRespond;
                    m_GetFocusTypeSP.enumValueIndex = (int)UIPanelGetFocusType.DontGet;
                    m_EscPressEventTypeSP.enumValueIndex = (int)UIPanelEscPressEventType.DontRespond;   //默认不响应
                    break;

                case UIPanelType.Custom:
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}