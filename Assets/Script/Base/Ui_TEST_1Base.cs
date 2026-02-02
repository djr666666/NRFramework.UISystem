
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NRFramework;

public class Ui_TEST_1Base : UIPanel
{
	protected Button m_ButtonLegacy_Button;
	protected Image m_Image_Image;
	protected Text m_TextLegacy_Text;
    protected override void OnBindCompsAndEvents() 
    {
		m_ButtonLegacy_Button = (Button)viewBehaviour.GetComponentByIndexs(0, 0);
		m_Image_Image = (Image)viewBehaviour.GetComponentByIndexs(1, 0);
		m_TextLegacy_Text = (Text)viewBehaviour.GetComponentByIndexs(2, 0);

		BindEvent(m_ButtonLegacy_Button);
	}

    protected override void OnUnbindCompsAndEvents() 
    {
		UnbindEvent(m_ButtonLegacy_Button);

		m_ButtonLegacy_Button = null;
		m_Image_Image = null;
		m_TextLegacy_Text = null;
	}
}