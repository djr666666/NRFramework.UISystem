using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NRFramework;

public class Ui_TEST_1_Temp : Ui_TEST_1Base
{
    protected override void OnCreating() { }

    protected override void OnCreated() {

  
    }

    public void test()
    {
        Debug.Log("????");
    }

    protected override void OnClicked(Button button) {

        if (button == m_ButtonLegacy_Button)
        {
            m_ButtonLegacy_Button.onClick.AddListener(() =>
         
            {
                //原来的UI框架没有 update 虽然在UI上 每一帧检测是 不好的 但是为了特殊情况 增加监听区别于他的 生命周期。
                //通过主动调用来决定是否真的需要 开启 update
                //后续可以写 计时器代替 
                OnAddUpdate(test);
                //OnAddApplicationPause
                //OnRemoveApplicationPause
                //OnAddApplicationFocus
                //OnRemovedApplicationFocus
                //OnAddUpdate OnRemoveUpdate
                //OnAddFixUpdate
                //OnRemoveFixUpdate
                //同理我也增加了 后台挂机等方法 根据项目需求可自行添加需求


            });
        }
    }

    protected override void OnValueChanged(Toggle toggle, bool value) { }

    protected override void OnValueChanged(Dropdown dropdown, int value) { }

    protected override void OnValueChanged(TMP_Dropdown tmpDropdown, int value) { }

    protected override void OnValueChanged(InputField inputField, string value) { }

    protected override void OnValueChanged(TMP_InputField tmpInputField, string value) { }

    protected override void OnValueChanged(Slider slider, float value) { }

    protected override void OnValueChanged(Scrollbar scrollbar, float value) { }

    protected override void OnValueChanged(ScrollRect scrollRect, Vector2 value) { }
    
    protected override void OnVisibleChanged(bool visible) { }
    
    protected override void OnFocusChanged(bool got) { }

    //protected override void OnBackgroundClicked() { }

    //protected override void OnEscButtonPressed() { }

    protected override void OnDestroying() { }

    protected override void OnDestroyed()
    {
        OnRemoveUpdate(test);


    }
}