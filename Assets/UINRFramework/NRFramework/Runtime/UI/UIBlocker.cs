// https://github.com/NRatel/NRFramework.UI

using System;
using UnityEngine;
using UnityEngine.UI;

namespace NRFramework
{
    [MonoSingletonSetting(HideFlags.NotEditable, "#UIBlocker#")]
    internal class UIBlocker : MonoSingletonNRT<UIBlocker>
    {
        private RectTransform m_RectTrransform;
        private RawImage m_RawImage;
        private Button m_Btn;

        private void Awake()
        {
            m_RectTrransform = gameObject.AddComponent<RectTransform>();
            m_RawImage = gameObject.AddComponent<RawImage>();
            m_Btn = gameObject.AddComponent<Button>();

            m_RawImage.color = new Color(0, 0, 0, 0);
            m_Btn.transition = Selectable.Transition.None;

            gameObject.SetActive(false);
        }

        internal void Bind(RectTransform parent, Texture texture, Color color, bool passThrough, Action onClick = null)
        {
            if (transform.parent == parent) { return; }

            gameObject.SetActive(true);

            m_RectTrransform.SetParent(parent);
            m_RectTrransform.SetAsFirstSibling();

            m_RectTrransform.localPosition = Vector3.zero;
            m_RectTrransform.localRotation = Quaternion.Euler(Vector3.zero);
            m_RectTrransform.localScale = Vector3.one;

            m_RectTrransform.anchorMin = Vector2.zero;
            m_RectTrransform.anchorMax = Vector2.one;
            m_RectTrransform.sizeDelta = Vector2.zero;

            m_RawImage.texture = texture;
            m_RawImage.color = color;
            m_RawImage.raycastTarget = !passThrough;

            m_Btn.onClick.RemoveAllListeners();
            if (!passThrough && onClick != null) { m_Btn.onClick.AddListener(() => { onClick(); }); }
        }

        /// <summary>
        /// 仅当遮罩当前确实挂在 parent 下时才解绑。
        /// 面板销毁必须走这个、不能直接 Unbind()：带关闭动画的面板是在 SetBackgroundAndFocus 之后才真正销毁的，
        /// 那时遮罩早已重新绑到别的面板上，无条件 Unbind 会把它从那个面板身上扯走、导致背景凭空消失。
        /// </summary>
        internal void UnbindFrom(Transform parent)
        {
            if (m_RectTrransform.parent != parent) { return; }
            Unbind();
        }

        internal void Unbind()
        {
            if (transform.parent == UIBlocker.commonRoot) { return; }

            m_Btn.onClick.RemoveAllListeners();
            m_RectTrransform.SetParent(UIBlocker.commonRoot);
            gameObject.SetActive(false);
        }
    }
}