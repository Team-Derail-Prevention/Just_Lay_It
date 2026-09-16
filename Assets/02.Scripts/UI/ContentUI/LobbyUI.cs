using Cysharp.Threading.Tasks;
using Enums;
using TMPro;
using UnityEngine;

public class LobbyUI : UIBase
{
    [SerializeField] private UIButton _btnGameStart;
    [SerializeField] private UIButton _btnUpgrade;
    [SerializeField] private UIButton _btnDictionary;
    [SerializeField] private UIButton _btnSetting;
    [SerializeField] private UIButton _btnExit;
    [SerializeField] private UIButton _btnTip;

    [Header("스테이지 표시")]
    [SerializeField] private TextMeshProUGUI _textStage;

    private bool _isStartingGame;

    private void OnEnable()
    {
        _isStartingGame = false;

        _btnGameStart.BindOnClickButtonEvent(OnClick_GameStart);
        _btnUpgrade.BindOnClickButtonEvent(OnClick_Upgrade);
        _btnDictionary.BindOnClickButtonEvent(OnClick_Dictionary);
        _btnSetting.BindOnClickButtonEvent(OnClick_Setting);
        _btnExit.BindOnClickButtonEvent(OnClick_Exit);
        _btnTip.BindOnClickButtonEvent(OnClick_Tip);

        LocalizationEventHub.Instance.OnLanguageChanged += OnLanguageChanged_LocalizationEventHub;

        RefreshStageText();
    }

    private void OnDisable()
    {
        _btnGameStart.UnBindOnClickButtonEvent(OnClick_GameStart);
        _btnUpgrade.UnBindOnClickButtonEvent(OnClick_Upgrade);
        _btnDictionary.UnBindOnClickButtonEvent(OnClick_Dictionary);
        _btnSetting.UnBindOnClickButtonEvent(OnClick_Setting);
        _btnExit.UnBindOnClickButtonEvent(OnClick_Exit);
        _btnTip.UnBindOnClickButtonEvent(OnClick_Tip);

        if (LocalizationEventHub.Instance != null)
        {
            LocalizationEventHub.Instance.OnLanguageChanged -= OnLanguageChanged_LocalizationEventHub;
        }
    }

    private void OnLanguageChanged_LocalizationEventHub(LanguageType language)
    {
        RefreshStageText();
    }

    private void RefreshStageText()
    {
        if (_textStage == null || GameManager.Instance == null)
        {
            return;
        }

        string stageTextId = GetStageLocalizationId(GameManager.Instance.CurrentGameStage);
        _textStage.text = $"( {LocalizationManager.Instance.GetText(stageTextId)} )";
    }

    private string GetStageLocalizationId(GameStage stage)
    {
        switch (stage)
        {
            case GameStage.Stage1:
                return "StageSelect_PopUp_UI_01";
            case GameStage.Stage2:
                return "StageSelect_PopUp_UI_02";
            case GameStage.Stage3:
                return "StageSelect_PopUp_UI_03";
            default:
                return "StageSelect_PopUp_UI_01";
        }
    }

    private async void OnClick_GameStart()
    {
        if (_isStartingGame == true)
        {
            return;
        }

        _isStartingGame = true;

        try
        {
            await UIManager.Instance.StartGameFromLobby();
        }
        finally
        {
            _isStartingGame = false;
        }
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
        UIManager.Instance.OpenExitConfirmPopup(OnConfirmExit);
    }

    private void OnConfirmExit()
    {
        Application.Quit();
    }

    private void OnClick_Tip()
    {
        UIManager.Instance.OpenNoticePopup(null);
    }
}
