using UnityEngine;
using System;
using System.ComponentModel;
using TMPro;

public class ScoreUI : UIBase
{
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
        RefreshAllTexts();
    }

    private void Update()
    {
        bool isEnterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (isEnterPressed == true)
        {
            OnConfirm();
        }
    }

    private void OnDisable()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _onConfirm = null;
    }

    public void Init(float totalDistance, int rescuedHumanCount, int collectedResourceCount, int killCount, Action onConfirm)
    {
        _viewModel.TotalDistance = totalDistance;
        _viewModel.RescuedHumanCount = rescuedHumanCount;
        _viewModel.CollectedResourceCount = collectedResourceCount;
        _viewModel.KillCount = killCount;

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
        }
    }

    private void RefreshAllTexts()
    {
        SetDistanceText();
        SetRescueText();
        SetResourceText();
        SetKillText();
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
