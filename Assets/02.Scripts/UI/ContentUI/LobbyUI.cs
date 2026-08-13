using UnityEngine;

public class LobbyUI : UIBase
{
    [SerializeField] private UIButton _btnGameStart;
    [SerializeField] private UIButton _btnUpgrade;
    [SerializeField] private UIButton _btnDictionary;
    [SerializeField] private UIButton _btnSetting;
    [SerializeField] private UIButton _btnExit;

    private void OnEnable()
    {
        _btnGameStart.BindOnClickButtonEvent(OnClick_GameStart);
        _btnUpgrade.BindOnClickButtonEvent(OnClick_Upgrade);
        _btnDictionary.BindOnClickButtonEvent(OnClick_Dictionary);
        _btnSetting.BindOnClickButtonEvent(OnClick_Setting);
        _btnExit.BindOnClickButtonEvent(OnClick_Exit);
    }

    private void OnDisable()
    {
        _btnGameStart.UnBindOnClickButtonEvent(OnClick_GameStart);
        _btnUpgrade.UnBindOnClickButtonEvent(OnClick_Upgrade);
        _btnDictionary.UnBindOnClickButtonEvent(OnClick_Dictionary);
        _btnSetting.UnBindOnClickButtonEvent(OnClick_Setting);
        _btnExit.UnBindOnClickButtonEvent(OnClick_Exit);
    }

    private void OnClick_GameStart()
    {
        UIManager.Instance.StartGameFromLobby();
    }

    private void OnClick_Upgrade()
    {
        UIManager.Instance.OpenLobbyUpgradeUI();
    }

    private void OnClick_Dictionary()
    {
        UIManager.Instance.OpenGameBookUI();
    }

    private void OnClick_Setting()
    {
        UIManager.Instance.OpenSettingUI();
    }

    private void OnClick_Exit()
    {
        // 게임매니저 작성 후 추가 작성
        UIManager.Instance.OpenExitConfirmPopup(OnConfirmExit);
    }

    private void OnConfirmExit()
    {
        // 게임 매니저 작성 후 추가 작성
        Application.Quit();
    }
}
