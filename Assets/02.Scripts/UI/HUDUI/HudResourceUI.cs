using System.ComponentModel;
using TMPro;
using UnityEngine;

public class HudResourceUI : UIBase
{
    [Header("로비 재화")]
    [SerializeField] private TextMeshProUGUI Text_Cash;

    [Header("나무")]
    [SerializeField] private TextMeshProUGUI Text_Wood;

    [Header("적재 제한량")]
    [SerializeField] private TextMeshProUGUI Text_StoneLimit;

    [Header("돌")]
    [SerializeField] private TextMeshProUGUI Text_Stone;

    [Header("돌 수량 경고 색상")]
    [SerializeField] private Color _stoneNormalColor = Color.white;
    [SerializeField] private Color _stoneWarningColor = new Color(1f, 0.6f, 0f);
    [SerializeField] private Color _stoneLimitColor = Color.red;
    [SerializeField, Range(0f, 1f)] private float _stoneWarningThreshold = 0.8f;

    [Header("탑승 인원")]
    [SerializeField] private TextMeshProUGUI Text_RescuedHuman;
    [SerializeField] private Color _boardingNormalColor = Color.white;
    [SerializeField] private Color _boardingWarningColor = new Color(1f, 0.6f, 0f);
    [SerializeField] private Color _boardingLimitColor = Color.red;
    [SerializeField, Range(0f, 1f)] private float _boardingWarningThreshold = 0.8f;

    private UpgradeViewModel _upgradeVm;
    private int _currentStoneCount;
    private int _currentCargoLimit;
    private int _currentBoardedCount;
    private int _currentBoardingLimit;

    private void OnEnable()
    {
        if (ResourceStatusEventHub.Instance == null)
        {
            Debug.LogError("[HudResourceUI] ResourceStatusEventHub.Instance가 null입니다. 씬에 배치했는지 확인하세요.");
            return;
        }
        else
        {
            ResourceStatusEventHub.Instance.OnWoodChanged += SetWood;
            ResourceStatusEventHub.Instance.OnStoneChanged += SetStone;
            ResourceStatusEventHub.Instance.OnBoardingChanged += SetBoardingInfo;
            ResourceStatusEventHub.Instance.OnCargoLimitChanged += SetStoneLimit;
        }

        if (NetworkResourceService.Instance != null)
        {
            var resourceVm = NetworkResourceService.Instance.GetLocalResourceViewModel();
            SetWood(resourceVm.CurrentWood);
            SetStone(resourceVm.CurrentStone);
            SetStoneLimit(NetworkResourceService.Instance.CargoLimit);
        }

        if (NetworkTrainCargoService.Instance != null)
        {
            SetBoardingInfo(NetworkTrainCargoService.Instance.BoardedCitizenCount, NetworkTrainCargoService.Instance.BoardingLimit);
        }

        if (NetworkUpgradeService.Instance != null)
        {
            _upgradeVm = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
            _upgradeVm.PropertyChanged += OnUpgradeViewModelPropertyChanged;
            SetCash(_upgradeVm.CurrentCash);
        }
    }

    private void OnDisable()
    {
        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged -= SetWood;
            ResourceStatusEventHub.Instance.OnStoneChanged -= SetStone;
            ResourceStatusEventHub.Instance.OnBoardingChanged -= SetBoardingInfo;
            ResourceStatusEventHub.Instance.OnCargoLimitChanged -= SetStoneLimit;
        }

        if (_upgradeVm != null)
        {
            _upgradeVm.PropertyChanged -= OnUpgradeViewModelPropertyChanged;
        }
    }

    private void OnUpgradeViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UpgradeViewModel.CurrentCash))
        {
            SetCash(_upgradeVm.CurrentCash);
        }
    }

    public void SetCash(int curCashCount)
    {
        if (Text_Cash != null)
        {
            Text_Cash.text = curCashCount.ToString();
        }
    }

    public void SetWood(int curWoodCount)
    {
        if (Text_Wood != null)
        {
            Text_Wood.text = curWoodCount.ToString();
        }
    }

    public void SetStone(int curStoneCount)
    {
        _currentStoneCount = curStoneCount;

        if (Text_Stone != null)
        {
            Text_Stone.text = curStoneCount.ToString();
        }

        RefreshStoneColor();
    }


    public void SetBoardingInfo(int curBoardedCount, int boardingLimit)
    {
        _currentBoardedCount = curBoardedCount;
        _currentBoardingLimit = boardingLimit;

        if (Text_RescuedHuman != null)
        {
            Text_RescuedHuman.text = $"{curBoardedCount:D2} / {boardingLimit:D2}";
            RefreshBoardingColor();
        }
    }

    private void RefreshBoardingColor()
    {
        if (Text_RescuedHuman == null || _currentBoardingLimit <= 0)
        {
            return;
        }

        float ratio = (float)_currentBoardedCount / _currentBoardingLimit;

        if (ratio >= 1f)
        {
            Text_RescuedHuman.color = _boardingLimitColor;
        }
        else if (ratio >= _boardingWarningThreshold)
        {
            Text_RescuedHuman.color = _boardingWarningColor;
        }
        else
        {
            Text_RescuedHuman.color = _boardingNormalColor;
        }
    }

    public void SetStoneLimit(int curCargoLimit)
    {
        _currentCargoLimit = curCargoLimit;

        if (Text_StoneLimit != null)
        {
            Text_StoneLimit.text = curCargoLimit.ToString();
        }

        RefreshStoneColor();
    }

    private void RefreshStoneColor()
    {
        if (Text_Stone == null || _currentCargoLimit <= 0)
        {
            return;
        }

        float ratio = (float)_currentStoneCount / _currentCargoLimit;

        if (ratio >= 1f)
        {
            Text_Stone.color = _stoneLimitColor;
        }
        else if (ratio >= _stoneWarningThreshold)
        {
            Text_Stone.color = _stoneWarningColor;
        }
        else
        {
            Text_Stone.color = _stoneNormalColor;
        }
    }
}
