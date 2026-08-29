using UnityEngine;
using System;
using System.ComponentModel;
using TMPro;
using UnityEngine.UI;

public class GameClearResultUI : UIBase
{
    [Header("제목")]
    [SerializeField] private TextMeshProUGUI Text_Title;

    [Header("운행 거리")]
    [SerializeField] private TextMeshProUGUI Text_DistanceScore;

    [Header("플레이 시간")]
    [SerializeField] private TextMeshProUGUI Text_TimeScore;

    [Header("시민 구출 수")]
    [SerializeField] private TextMeshProUGUI Text_RescueScore;

    [Header("획득 나무 / 획득 돌")]
    [SerializeField] private TextMeshProUGUI Text_ResourceTreeScore;
    [SerializeField] private TextMeshProUGUI Text_ResourceStoneScore;

    [Header("몬스터 킬 수")]
    [SerializeField] private TextMeshProUGUI Text_KillScore;

    [Header("선로 제작 / 설치 수")]
    [SerializeField] private TextMeshProUGUI Text_RailMakeScore;
    [SerializeField] private TextMeshProUGUI Text_RailInstallScore;

    [Header("획득 캐쉬")]
    [SerializeField] private TextMeshProUGUI Text_CashScore;

    [Header("누적 게임 플레이 횟수")]
    [SerializeField] private TextMeshProUGUI Text_GameNumberScore;

    [Header("다음 난이도 응원 문구 (최종 난이도가 아닐 때)")]
    [SerializeField] private GameObject _nextStageNoticeRoot;
    [SerializeField] private TextMeshProUGUI Text_NextStageNotice;

    [Header("제작진 크레딧 (모든 난이도 특수 클리어 시)")]
    [SerializeField] private GameObject _creditsRoot;

    [Header("엔딩 문구 (모든 난이도 특수 클리어 시)")]
    [SerializeField] private GameObject _endingRoot;

    [Header("스크롤 콘텐츠 (제목 ~ 엔딩 전체)")]
    [SerializeField] private RectTransform _viewportRect;
    [SerializeField] private RectTransform _scrollContent;
    [SerializeField, Min(0f)] private float _creditsScrollSpeed = 60f;

    private readonly GameClearResultViewModel _viewModel = new GameClearResultViewModel();
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
            return;
        }

        ScrollContentIfNeeded();
    }

    private void ScrollContentIfNeeded()
    {
        if (_viewModel.IsFinalStage == false || _scrollContent == null || _viewportRect == null)
        {
            return;
        }

        float maxScrollY = Mathf.Max(0f, _scrollContent.rect.height - _viewportRect.rect.height);
        if (maxScrollY <= 0f)
        {
            return;
        }

        float targetY = -maxScrollY;

        Vector2 anchoredPosition = _scrollContent.anchoredPosition;
        anchoredPosition.y = Mathf.MoveTowards(anchoredPosition.y, targetY, _creditsScrollSpeed * Time.deltaTime);
        _scrollContent.anchoredPosition = anchoredPosition;
    }

    private void ResetScrollContentPosition()
    {
        if (_scrollContent == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);

        Vector2 anchoredPosition = _scrollContent.anchoredPosition;
        anchoredPosition.y = 0f;
        _scrollContent.anchoredPosition = anchoredPosition;
    }

    private void OnDisable()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _onConfirm = null;
    }

    public void Init(GameClearResultData resultData, Action onConfirm)
    {
        if (resultData == null)
        {
            Debug.LogWarning("[GameClearResultUI] resultData가 null입니다.");
            return;
        }

        _viewModel.TotalDistance = resultData.TotalDistance;
        _viewModel.PlayTimeSeconds = resultData.PlayTimeSeconds;
        _viewModel.RescuedHumanCount = resultData.RescuedHumanCount;
        _viewModel.CollectedWoodCount = resultData.CollectedWoodCount;
        _viewModel.CollectedStoneCount = resultData.CollectedStoneCount;
        _viewModel.KillCount = resultData.KillCount;
        _viewModel.RailCraftedCount = resultData.RailCraftedCount;
        _viewModel.RailInstalledCount = resultData.RailInstalledCount;
        _viewModel.EarnedCashCount = resultData.EarnedCashCount;
        _viewModel.TotalPlayCount = resultData.TotalPlayCount;
        _viewModel.TitleMessage = resultData.TitleMessage;
        _viewModel.NextStageNoticeMessage = resultData.NextStageNoticeMessage;
        _viewModel.IsFinalStage = resultData.IsFinalStage;

        _onConfirm = onConfirm;

        RefreshAllTexts();
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(GameClearResultViewModel.TitleMessage):
                SetTitleText();
                break;
            case nameof(GameClearResultViewModel.TotalDistance):
                SetDistanceText();
                break;
            case nameof(GameClearResultViewModel.PlayTimeSeconds):
                SetTimeText();
                break;
            case nameof(GameClearResultViewModel.RescuedHumanCount):
                SetRescueText();
                break;
            case nameof(GameClearResultViewModel.CollectedWoodCount):
                SetResourceTreeText();
                break;
            case nameof(GameClearResultViewModel.CollectedStoneCount):
                SetResourceStoneText();
                break;
            case nameof(GameClearResultViewModel.KillCount):
                SetKillText();
                break;
            case nameof(GameClearResultViewModel.RailCraftedCount):
                SetRailMakeText();
                break;
            case nameof(GameClearResultViewModel.RailInstalledCount):
                SetRailInstallText();
                break;
            case nameof(GameClearResultViewModel.EarnedCashCount):
                SetCashText();
                break;
            case nameof(GameClearResultViewModel.TotalPlayCount):
                SetGameNumberText();
                break;
            case nameof(GameClearResultViewModel.NextStageNoticeMessage):
                SetNextStageNoticeText();
                break;
            case nameof(GameClearResultViewModel.IsFinalStage):
                SetFooterVisibility();
                break;
        }
    }

    private void RefreshAllTexts()
    {
        SetTitleText();
        SetDistanceText();
        SetTimeText();
        SetRescueText();
        SetResourceTreeText();
        SetResourceStoneText();
        SetKillText();
        SetRailMakeText();
        SetRailInstallText();
        SetCashText();
        SetGameNumberText();
        SetNextStageNoticeText();
        SetFooterVisibility();
    }

    private void SetTitleText()
    {
        if (Text_Title != null)
        {
            Text_Title.text = _viewModel.TitleMessage;
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

    private void SetTimeText()
    {
        if (Text_TimeScore != null)
        {
            int totalSeconds = Mathf.RoundToInt(_viewModel.PlayTimeSeconds);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            Text_TimeScore.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
    }

    private void SetRescueText()
    {
        if (Text_RescueScore != null)
        {
            Text_RescueScore.text = _viewModel.RescuedHumanCount.ToString("D4");
        }
    }

    private void SetResourceTreeText()
    {
        if (Text_ResourceTreeScore != null)
        {
            Text_ResourceTreeScore.text = _viewModel.CollectedWoodCount.ToString("D4");
        }
    }

    private void SetResourceStoneText()
    {
        if (Text_ResourceStoneScore != null)
        {
            Text_ResourceStoneScore.text = _viewModel.CollectedStoneCount.ToString("D4");
        }
    }

    private void SetKillText()
    {
        if (Text_KillScore != null)
        {
            Text_KillScore.text = _viewModel.KillCount.ToString("D4");
        }
    }

    private void SetRailMakeText()
    {
        if (Text_RailMakeScore != null)
        {
            Text_RailMakeScore.text = _viewModel.RailCraftedCount.ToString("D4");
        }
    }

    private void SetRailInstallText()
    {
        if (Text_RailInstallScore != null)
        {
            Text_RailInstallScore.text = _viewModel.RailInstalledCount.ToString("D4");
        }
    }

    private void SetCashText()
    {
        if (Text_CashScore != null)
        {
            Text_CashScore.text = _viewModel.EarnedCashCount.ToString("D4");
        }
    }

    private void SetGameNumberText()
    {
        if (Text_GameNumberScore != null)
        {
            Text_GameNumberScore.text = _viewModel.TotalPlayCount.ToString("D4");
        }
    }

    private void SetNextStageNoticeText()
    {
        if (Text_NextStageNotice != null)
        {
            Text_NextStageNotice.text = _viewModel.NextStageNoticeMessage;
        }
    }

    private void SetFooterVisibility()
    {
        if (_nextStageNoticeRoot != null)
        {
            _nextStageNoticeRoot.SetActive(!_viewModel.IsFinalStage);
        }

        if (_creditsRoot != null)
        {
            _creditsRoot.SetActive(_viewModel.IsFinalStage);
        }

        if (_endingRoot != null)
        {
            _endingRoot.SetActive(_viewModel.IsFinalStage);
        }

        ResetScrollContentPosition();
    }

    private void OnConfirm()
    {
        Action onConfirm = _onConfirm;
        UIManager.Instance.CloseGameClearResultUI();
        onConfirm?.Invoke();
    }
}
