using UnityEngine;
using TMPro;

public class StationArrivalUI : UIBase
{
    private const int REPAIR_HEAL_PERCENT = 20; 
    private const int REPAIR_STONE_COST = 40;  

    [Header("좌상단 표시")]
    [SerializeField] private TextMeshProUGUI Text_MyStone;
    [SerializeField] private TextMeshProUGUI Text_Boarding;
    [SerializeField] private TextMeshProUGUI Text_CargoLoad;

    [Header("돌")]
    [SerializeField] private TextMeshProUGUI Text_StationStoneAmount;
    [SerializeField] private TextMeshProUGUI Text_TakeStoneAmount;
    [SerializeField] private UIButton Button_InputStone;

    [Header("시민")]
    [SerializeField] private TextMeshProUGUI Text_StationCitizenAmount;
    [SerializeField] private TextMeshProUGUI Text_BoardCitizenAmount;
    [SerializeField] private UIButton Button_InputCitizen;

    [Header("열차 수리")]
    [SerializeField] private TextMeshProUGUI Text_TrainHpPercent;
    [SerializeField] private TextMeshProUGUI Text_RepairCost;
    [SerializeField] private UIButton Button_Repair;

    [Header("출발")]
    [SerializeField] private UIButton Button_Departure;

    private StationObject _currentStation;
    private int _stationAvailableStone;
    private int _stationAvailableCitizen;
    private int _takeStoneAmount;
    private int _boardCitizenAmount;
    private float _currentHpPercent = 100f;

    private void OnEnable()
    {
        if (Button_InputStone != null)
        {
            Button_InputStone.BindOnClickButtonEvent(OnClick_InputStone);
        }

        if (Button_InputCitizen != null)
        {
            Button_InputCitizen.BindOnClickButtonEvent(OnClick_InputCitizen);
        }

        if (Button_Repair != null)
        {
            Button_Repair.BindOnClickButtonEvent(OnClick_Repair);
        }

        if (Button_Departure != null)
        {
            Button_Departure.BindOnClickButtonEvent(OnClick_Departure);
        }

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged += OnHpChanged;
        }

        _takeStoneAmount = 0;
        _boardCitizenAmount = 0;

        RefreshRepairCostText();
        SyncHpFromActiveTrain();
        RefreshTopIndicators();
        RefreshTakeAmountTexts();
    }

    private void OnDisable()
    {
        if (Button_InputStone != null)
        {
            Button_InputStone.UnBindOnClickButtonEvent(OnClick_InputStone);
        }

        if (Button_InputCitizen != null)
        {
            Button_InputCitizen.UnBindOnClickButtonEvent(OnClick_InputCitizen);
        }

        if (Button_Repair != null)
        {
            Button_Repair.UnBindOnClickButtonEvent(OnClick_Repair);
        }

        if (Button_Departure != null)
        {
            Button_Departure.UnBindOnClickButtonEvent(OnClick_Departure);
        }

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged -= OnHpChanged;
        }
    }

    public void Init(StationObject station, int availableStone, int availableCitizen)
    {
        _currentStation = station;
        _stationAvailableStone = availableStone;
        _stationAvailableCitizen = availableCitizen;

        if (Text_StationStoneAmount != null)
        {
            Text_StationStoneAmount.text = _stationAvailableStone.ToString();
        }

        if (Text_StationCitizenAmount != null)
        {
            Text_StationCitizenAmount.text = _stationAvailableCitizen.ToString();
        }
    }

    private void SyncHpFromActiveTrain()
    {
        if (TrainStatusEventHub.Instance == null)
        {
            return;
        }

        OnHpChanged(TrainStatusEventHub.Instance.CurrentHp, TrainStatusEventHub.Instance.CurrentMaxHp);
    }

    private void OnHpChanged(float curHp, float maxHp)
    {
        if (maxHp > 0f)
        {
            _currentHpPercent = curHp / maxHp * 100f;
        }
        else
        {
            _currentHpPercent = 0f;
        }

        RefreshHpText();
    }

    private void RefreshHpText()
    {
        if (Text_TrainHpPercent != null)
        {
            Text_TrainHpPercent.text = $"{Mathf.RoundToInt(_currentHpPercent)}%";
        }
    }

    private void RefreshRepairCostText()
    {
        if (Text_RepairCost == null)
        {
            return;
        }

        bool isFreeAvailable = NetworkResourceService.Instance != null && NetworkResourceService.Instance.IsStationRepairFreeAvailable;
        Text_RepairCost.text = isFreeAvailable ? "무료" : REPAIR_STONE_COST.ToString();
    }

    private void RefreshTopIndicators()
    {
        var resourceVm = NetworkResourceService.Instance.GetLocalResourceViewModel();

        if (Text_MyStone != null)
        {
            Text_MyStone.text = resourceVm.CurrentStone.ToString();
        }

        if (Text_Boarding != null)
        {
            Text_Boarding.text = $"{NetworkTrainCargoService.Instance.BoardedCitizenCount}/{NetworkTrainCargoService.Instance.BoardingLimit}";
        }

        if (Text_CargoLoad != null)
        {
            Text_CargoLoad.text = $"{NetworkResourceService.Instance.CurrentCargoLoad}/{NetworkResourceService.Instance.CargoLimit}";
        }

        RefreshHpText();
    }

    private void RefreshTakeAmountTexts()
    {
        if (Text_TakeStoneAmount != null)
        {
            Text_TakeStoneAmount.text = _takeStoneAmount.ToString();
        }

        if (Text_BoardCitizenAmount != null)
        {
            Text_BoardCitizenAmount.text = _boardCitizenAmount.ToString();
        }
    }

    private void OnClick_InputStone()
    {
        int maxAllowed = Mathf.Min(_stationAvailableStone, NetworkResourceService.Instance.RemainingCargoCapacity);
        if (maxAllowed <= 0)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "더 이상 가져갈 수 없습니다.\n(적재 한도 초과)");
            return;
        }

        UIManager.Instance.OpenTextInputPopup(
            $"가져갈 돌의 양을 입력하세요.\n(최대 {maxAllowed})",
            OnStoneAmountConfirmed,
            OnInvalidInputAlert,
            maxAllowed);
    }

    private void OnStoneAmountConfirmed(int amount)
    {
        _takeStoneAmount = amount;
        RefreshTakeAmountTexts();
    }

    private void OnClick_InputCitizen()
    {
        int maxAllowed = Mathf.Min(_stationAvailableCitizen, NetworkTrainCargoService.Instance.RemainingBoardingCapacity);
        if (maxAllowed <= 0)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "더 이상 탑승시킬 수 없습니다.\n(탑승 정원 초과)");
            return;
        }

        UIManager.Instance.OpenTextInputPopup(
            $"탑승시킬 인원을 입력하세요.\n(최대 {maxAllowed})",
            OnCitizenAmountConfirmed,
            OnInvalidInputAlert,
            maxAllowed);
    }

    private void OnCitizenAmountConfirmed(int amount)
    {
        _boardCitizenAmount = amount;
        RefreshTakeAmountTexts();
    }

    private void OnInvalidInputAlert()
    {
        UIManager.Instance.OpenExitConfirmPopup(null, null, "올바른 숫자를 입력해주세요.");
    }

    private void OnClick_Repair()
    {
        if (GameManager.Train != null && GameManager.Train.IsTrainHpFull())
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "열차 체력이 이미 가득 찼습니다!");
            return;
        }

        bool isFreeAvailable = NetworkResourceService.Instance != null && NetworkResourceService.Instance.IsStationRepairFreeAvailable;

        bool isSpent = NetworkResourceService.Instance.TrySpendStoneForStationRepair(REPAIR_STONE_COST);
        if (isSpent == false)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "돌이 부족합니다.");
            return;
        }

        RefreshTopIndicators();
        RefreshRepairCostText();

        GameManager.Train.HealActiveTrain(REPAIR_HEAL_PERCENT);

        Debug.Log($"[StationArrivalUI] 수리 요청 완료 (최대 체력의 {REPAIR_HEAL_PERCENT}% 회복), 돌 {REPAIR_STONE_COST} 소모");
    }

    private void OnClick_Departure()
    {
        int actualTakenStone = NetworkResourceService.Instance.AddStone(_takeStoneAmount);
        int actualBoarded = NetworkTrainCargoService.Instance.BoardCitizens(_boardCitizenAmount);

        if (_currentStation != null)
        {
            GameManager.Instance.CompleteStation(actualTakenStone, actualBoarded);
        }

        UIManager.Instance.CloseStationArrivalUI();
    }
}
