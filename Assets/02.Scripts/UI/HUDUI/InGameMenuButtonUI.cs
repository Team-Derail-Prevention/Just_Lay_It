using UnityEngine;
using UnityEngine.InputSystem;

public class InGameMenuButtonUI : UIBase
{
    [SerializeField] private UIButton Button_OpenMenu;

    private void OnEnable()
    {
        if (Button_OpenMenu != null)
        {
            Button_OpenMenu.BindOnClickButtonEvent(OnClick_OpenMenu);
        }
    }

    private void OnDisable()
    {
        if (Button_OpenMenu != null)
        {
            Button_OpenMenu.UnBindOnClickButtonEvent(OnClick_OpenMenu);
        }
    }

    private void Update()
    {
        CheckEscInput();
    }

    private void CheckEscInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame == true)
        {
            ToggleMenuPopup();
        }
    }

    private void OnClick_OpenMenu()
    {
        UIManager.Instance.OpenInGameMenuPopup();
    }

    private void ToggleMenuPopup()
    {
        bool isOpen = UIManager.Instance.IsOpenUI(UIType.InGameMenuPopup);
        if (isOpen == true)
        {
            UIManager.Instance.CloseInGameMenuPopup();
        }
        else
        {
            UIManager.Instance.OpenInGameMenuPopup();
        }
    }
}
