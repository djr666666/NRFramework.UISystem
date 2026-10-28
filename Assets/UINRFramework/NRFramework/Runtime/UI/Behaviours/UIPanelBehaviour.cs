// https://github.com/NRatel/NRFramework.UI

using System;
using System.Collections;
using UnityEngine;

namespace NRFramework
{
    // 下面各枚举成员上的 [InspectorName] 只影响 Inspector 里的显示文案，不影响序列化
    //（Unity 枚举按 int 值存盘），改文案不会让已有 prefab 的配置丢失。

    public enum UIPanelType
    {
        [InspectorName("Underlay 底层界面")] Underlay,
        [InspectorName("Overlay 覆盖层")] Overlay,
        [InspectorName("Window 窗口")] Window,
        [InspectorName("ModalWindow 模态窗口")] ModalWindow,
        [InspectorName("Float 浮窗")] Float,
        [InspectorName("System 系统级")] System,
        [InspectorName("Custom 自定义")] Custom,
    }

    public enum UIPanelBgShowType
    {
        [InspectorName("Alpha 全透明(只挡点击)")] Alpha,
        [InspectorName("HalfAlphaBlack 半透明黑")] HalfAlphaBlack,
        [InspectorName("CustomColor 自定义颜色")] CustomColor,
        /* CustomTexture, BlurryScreenshot */
    }

    public enum UIPanelBgClickEventType
    {
        [InspectorName("PassThrough 穿透到下层")] PassThrough,
        [InspectorName("DontRespond 吃掉点击但不响应")] DontRespond,
        [InspectorName("CloseSelf 关闭自身")] CloseSelf,
        [InspectorName("DestroySelf 销毁自身")] DestroySelf,
        [InspectorName("Custom 自定义(回调 OnBackgroundClicked)")] Custom,
    }

    public enum UIPanelGetFocusType
    {
        [InspectorName("DontGet 从不获取焦点")] DontGet,
        [InspectorName("Get 独占焦点(下方面板全部失焦)")] Get,
        [InspectorName("GetWithOthers 与上方面板共享焦点")] GetWithOthers,
    }

    public enum UIPanelEscPressEventType
    {
        [InspectorName("DontCheck 不参与返回键检测")] DontCheck,
        [InspectorName("DontRespond 参与检测但不响应(吃掉返回键)")] DontRespond,
        [InspectorName("CloseSelf 关闭自身")] CloseSelf,
        [InspectorName("DestroySelf 销毁自身")] DestroySelf,
        [InspectorName("Custom 自定义(回调 OnEscButtonPressed)")] Custom,
    }

    public enum UIPanelOpenAnimPlayMode
    {
        [InspectorName("AutoPlay 用 Animator 播放 open")] AutoPlay,
        [InspectorName("ControlBySelf 由界面自己控制(重写方法)")] ControlBySelf,
    }

    public enum UIPanelCloseAnimPlayMode
    {
        [InspectorName("AutoPlay 用 Animator 播放 close")] AutoPlay,
        [InspectorName("ControlBySelf 由界面自己控制(重写方法)")] ControlBySelf,
    }

    public class UIPanelBehaviour : UIViewBehaviour
    {
#pragma warning disable 414
        [SerializeField]
        private UIPanelType m_PanelType;
#pragma warning restore 414

        [SerializeField]
        private bool m_HasBg;

        [SerializeField]
        private UIPanelBgShowType m_BgShowType;

        [SerializeField]
        private Color m_CustomBgColor;

        [SerializeField]
        private UIPanelBgClickEventType m_BgClickEventType;

        [SerializeField]
        private UIPanelGetFocusType m_GetFocusType;

        [SerializeField]
        UIPanelEscPressEventType m_EscPressEventType;

        //层级相关
        [SerializeField]
        private int m_Thickness;

        [SerializeField]
        private UIPanelOpenAnimPlayMode m_OpenAnimPlayMode;

        [SerializeField]
        private UIPanelCloseAnimPlayMode m_CloseAnimPlayMode;

        public bool hasBg { get { return m_HasBg; } }

        static private Color sm_BgColor_Alpha = new Color(0, 0, 0, 0);
        static private Color sm_BgColor_HalfAlphaBlack = new Color(0, 0, 0, 0.7f);

        public Texture bgTexture
        {
            get { return null; }
        }

        public Color bgColor
        {
            get
            {
                Debug.Assert(hasBg);
                return m_BgShowType switch
                {
                    UIPanelBgShowType.Alpha => sm_BgColor_Alpha,
                    UIPanelBgShowType.HalfAlphaBlack => sm_BgColor_HalfAlphaBlack,
                    UIPanelBgShowType.CustomColor => m_CustomBgColor,
                    _ => Color.white,
                };
            }
        }

        public UIPanelBgClickEventType bgClickEventType { get { return m_BgClickEventType; } }

        public UIPanelGetFocusType getFocusType { get { return m_GetFocusType; } }

        public UIPanelEscPressEventType escPressEventType { get { return m_EscPressEventType; } }

        public int thickness { get { return m_Thickness; } }

        public UIPanelOpenAnimPlayMode openAnimPlayMode { get { return m_OpenAnimPlayMode; } }

        public UIPanelCloseAnimPlayMode closeAnimPlayMode { get { return m_CloseAnimPlayMode; } }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();

            //默认显示为 Underlay
            m_PanelType = UIPanelType.Underlay;

            //默认显示为 Underlay 的子项
            m_HasBg = true;
            m_BgShowType = UIPanelBgShowType.Alpha;
            m_CustomBgColor = Color.white;
            m_BgClickEventType = UIPanelBgClickEventType.DontRespond;
            m_GetFocusType = UIPanelGetFocusType.Get;
            m_EscPressEventType = UIPanelEscPressEventType.DontRespond;

            m_Thickness = Config.kDefaultPanelThickness;
            m_OpenAnimPlayMode = UIPanelOpenAnimPlayMode.AutoPlay;
            m_CloseAnimPlayMode = UIPanelCloseAnimPlayMode.AutoPlay;
        }
#endif

        public bool ExistValidAnimator()
        {
            Animator animator;
            bool exsistAimator = TryGetComponent<Animator>(out animator);

            //必须连 Controller 一起检查：只判「组件存在且启用」的话，挂了 Animator 却没给 Controller 时
            //会进到播放分支，而 PlayOpenAnim/PlayCloseAnim 里 animator.runtimeAnimatorController.animationClips 直接空引用崩溃。
            //加上这个判断后，这种情况会安全退回到「无动画」的同步分支。
            return exsistAimator && animator.enabled && animator.runtimeAnimatorController != null;
        }

        internal void PlayOpenAnim(Action onFinish)
        {
            Animator animator = GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) { onFinish(); return; }

            animator.SetTrigger("open");

            StartCoroutine(DoAfterSeconds(GetClipLength(animator, "open"), onFinish));
        }

        internal void PlayCloseAnim(Action onFinish)
        {
            Animator animator = GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) { onFinish(); return; }

            animator.SetTrigger("close");

            StartCoroutine(DoAfterSeconds(GetClipLength(animator, "close"), onFinish));
        }

        // 约定：Controller 里必须有一个和 clipName 同名的 AnimationClip，框架靠它的长度决定等多久。
        // 找不到就按 0 处理（动画会被立刻掐断），所以这里必须报出来，否则表现是「动画莫名其妙不播」且无任何线索。
        private float GetClipLength(Animator animator, string clipName)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName) { return clip.length; }
            }

            Debug.LogWarning($"[NRFramework] 面板 {name} 的 Animator Controller 里找不到名为 \"{clipName}\" 的动画片段，" +
                             $"本次动画会被立即结束。请把动画文件改名为 \"{clipName}\"（片段名，不是状态机里的状态名）。", this);
            return 0f;
        }

        private IEnumerator DoAfterSeconds(float duration, Action onFinish)
        {
            //用 Realtime 而不是 WaitForSeconds：后者受 Time.timeScale 影响，
            //游戏暂停（timeScale=0）时计时会停住，正在播关闭动画的面板就永远走不到销毁那一步、卡死在屏幕上。
            //配套要求：Animator 的 updateMode 也要设成 UnscaledTime，两边计时基准得一致。
            yield return new WaitForSecondsRealtime(duration);
            onFinish();
        }
    }
}
