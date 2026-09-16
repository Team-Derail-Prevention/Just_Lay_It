using UnityEngine;

public class InGameMenuPopup : UIBase
{
    [SerializeField] private UIButton Button_Setting;
    [SerializeField] private UIButton Button_ExitLobby;
    [SerializeField] private UIButton Button_QuitGame;
    [SerializeField] private UIButton Button_Cancel;

    private void OnEnable()
    {
        if (Button_Setting != null)
        {
            Button_Setting.BindOnClickButtonEvent(OnClick_Setting);
        }

        if (Button_ExitLobby != null)
        {
            Button_ExitLobby.BindOnClickButtonEvent(OnClick_ExitLobby);
        }

        if (Button_QuitGame != null)
        {
            Button_QuitGame.BindOnClickButtonEvent(OnClick_QuitGame);
        }

        if (Button_Cancel != null)
        {
            Button_Cancel.BindOnClickButtonEvent(OnClick_Cancel);
        }
    }

    private void OnDisable()
    {
        if (Button_Setting != null)
        {
            Button_Setting.UnBindOnClickButtonEvent(OnClick_Setting);
        }

        if (Button_ExitLobby != null)
        {
            Button_ExitLobby.UnBindOnClickButtonEvent(OnClick_ExitLobby);
        }

        if (Button_QuitGame != null)
        {
            Button_QuitGame.UnBindOnClickButtonEvent(OnClick_QuitGame);
        }

        if (Button_Cancel != null)
        {
            Button_Cancel.UnBindOnClickButtonEvent(OnClick_Cancel);
        }
    }

    private void OnClick_Setting()
    {
        UIManager.Instance.OpenSettingUI();
    }

    private void OnClick_ExitLobby()
    {
        UIManager.Instance.OpenExitConfirmPopup(ConfirmExitToLobby, null, LocalizationManager.Instance.GetText("ExitConfirm_PopUp_UI_03"));
    }

    private void ConfirmExitToLobby()
    {
        UIManager.Instance.CloseInGameMenuPopup();
        GameManager.Instance.ReturnToLobby();
    }

    private void OnClick_QuitGame()
    {
        UIManager.Instance.OpenExitConfirmPopup(ConfirmQuitGame, null, LocalizationManager.Instance.GetText("ExitConfirm_PopUp_UI_03"));
    }

    private void ConfirmQuitGame()
    {
        Application.Quit();
    }

    private void OnClick_Cancel()
    {
        UIManager.Instance.CloseInGameMenuPopup();
    }
}
