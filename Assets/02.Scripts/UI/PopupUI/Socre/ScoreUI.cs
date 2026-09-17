using UnityEngine;
using System;
using System.ComponentModel;
using TMPro;
using UnityEngine.InputSystem;
using Enums;

public class ScoreUI : UIBase
{
    [Header("제목")]
    [SerializeField] private TextMeshProUGUI Text_Title;

    [Header("운행 거리")]
    [SerializeField] private TextMeshProUGUI Text_DistanceScore;

    [Header("구출한 시민 수")]
    [SerializeField] private TextMeshProUGUI Text_RescueScore;

    [Header("획득한 자원")]
    [SerializeField] private TextMeshProUGUI Text_ResourceScore;

    [Header("처치한 몬스터 수")]
    [SerializeField] private TextMeshProUGUI Text_KillScore;

    private readonly ScoreViewModel _viewModel = new ScoreViewModel();
    private Action _onConfirm;

    private void OnEnable()
    {
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        LocalizationEventHub.Instance.OnLanguageChanged += OnLanguageChanged_LocalizationEventHub;

        RefreshAllTexts();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        bool isEnterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (isEnterPressed == true)
        {
            OnConfirm();
        }
    }

    private void OnDisable()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        if (LocalizationEventHub.Instance != null)
        {
            LocalizationEventHub.Instance.OnLanguageChanged -= OnLanguageChanged_LocalizationEventHub;
        }

        _onConfirm = null;
    }

    private void OnLanguageChanged_LocalizationEventHub(LanguageType language)
    {
        SetTitleText();
    }

    public void Init(float totalDistance, int rescuedHumanCount, int collectedResourceCount, int killCount, ScoreResultType resultType, Action onConfirm)
    {
        _viewModel.TotalDistance = totalDistance;
        _viewModel.RescuedHumanCount = rescuedHumanCount;
        _viewModel.CollectedResourceCount = collectedResourceCount;
        _viewModel.KillCount = killCount;
        _viewModel.ResultType = resultType;

        _onConfirm = onConfirm;

        RefreshAllTexts();
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ScoreViewModel.TotalDistance):
                SetDistanceText();
                break;
            case nameof(ScoreViewModel.RescuedHumanCount):
                SetRescueText();
                break;
            case nameof(ScoreViewModel.CollectedResourceCount):
                SetResourceText();
                break;
            case nameof(ScoreViewModel.KillCount):
                SetKillText();
                break;
            case nameof(ScoreViewModel.ResultType):
                SetTitleText();
                break;
        }
    }

    private void RefreshAllTexts()
    {
        SetDistanceText();
        SetRescueText();
        SetResourceText();
        SetKillText();
        SetTitleText();
    }

    private void SetTitleText()
    {
        if (Text_Title == null)
        {
            return;
        }

        switch (_viewModel.ResultType)
        {
            case ScoreResultType.GameClear:
                Text_Title.text = LocalizationManager.Instance.GetText("Score_PopUp_UI_07");
                break;
            case ScoreResultType.GameOver:
                Text_Title.text = LocalizationManager.Instance.GetText("Score_PopUp_UI_08");
                break;
            case ScoreResultType.SimpleClear:
                Text_Title.text = LocalizationManager.Instance.GetText("Score_PopUp_UI_09");
                break;
            case ScoreResultType.BaseArrival:
            default:
                Text_Title.text = LocalizationManager.Instance.GetText("Score_PopUp_UI_06");
                break;
        }
    }

    private void SetDistanceText()
    {
        if (Text_DistanceScore != null)
        {
            int roundedDistance = Mathf.RoundToInt(_viewModel.TotalDistance);
            Text_DistanceScore.text = roundedDistance.ToString("D4");
        }
    }

    private void SetRescueText()
    {
        if (Text_RescueScore != null)
        {
            Text_RescueScore.text = _viewModel.RescuedHumanCount.ToString("D4");
        }
    }

    private void SetResourceText()
    {
        if (Text_ResourceScore != null)
        {
            Text_ResourceScore.text = _viewModel.CollectedResourceCount.ToString("D4");
        }
    }

    private void SetKillText()
    {
        if (Text_KillScore != null)
        {
            Text_KillScore.text = _viewModel.KillCount.ToString("D4");
        }
    }

    private void OnConfirm()
    {
        Action onConfirm = _onConfirm;
        UIManager.Instance.CloseScoreUI();
        onConfirm?.Invoke();
    }
}
