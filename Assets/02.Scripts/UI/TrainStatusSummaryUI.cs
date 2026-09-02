using Enums;
using TMPro;
using UnityEngine;

public class TrainStatusSummaryUI : UIBase
{
    [Header("전투")]
    [SerializeField] private TextMeshProUGUI Text_Hp;
    [SerializeField] private TextMeshProUGUI Text_Defense;

    [Header("적재")]
    [SerializeField] private TextMeshProUGUI Text_Amount;
    [SerializeField] private TextMeshProUGUI Text_Resource;
    [SerializeField] private TextMeshProUGUI Text_Boarding;

    [Header("드론")]
    [SerializeField] private TextMeshProUGUI Text_CollectSpeed;
    [SerializeField] private TextMeshProUGUI Text_CollectEfficiency;
    [SerializeField] private TextMeshProUGUI Text_ActionSpeed;

    private const float POLL_INTERVAL = 0.5f;

    private void OnEnable()
    {
        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged += OnHpChanged;
        }

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged += OnResourceChanged;
            ResourceStatusEventHub.Instance.OnStoneChanged += OnResourceChanged;
        }

        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnInGameUpgraded += OnInGameUpgraded;
        }

        RefreshAll();
        InvokeRepeating(nameof(RefreshAll), POLL_INTERVAL, POLL_INTERVAL);
    }

    private void OnDisable()
    {
        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged -= OnHpChanged;
        }

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged -= OnResourceChanged;
            ResourceStatusEventHub.Instance.OnStoneChanged -= OnResourceChanged;
        }

        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnInGameUpgraded -= OnInGameUpgraded;
        }

        CancelInvoke(nameof(RefreshAll));
    }

    private void OnHpChanged(float curHp, float maxHp)
    {
        if (Text_Hp != null)
        {
            Text_Hp.text = $"체력 : {Mathf.RoundToInt(curHp)} / {Mathf.RoundToInt(maxHp)}";
        }
    }

    private void OnResourceChanged(int amount)
    {
        RefreshCargo();
    }

    private void OnInGameUpgraded(string slotDataId, int newLevel)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshBattle();
        RefreshCargo();
        RefreshDrone();
    }

    private void RefreshBattle()
    {
        if (TrainManager.Instance == null || TrainManager.Instance.ActiveTrain == null)
        {
            return;
        }

        Train activeTrain = TrainManager.Instance.ActiveTrain;

        if (Text_Hp != null)
        {
            Text_Hp.text = $"체력 : {Mathf.RoundToInt(activeTrain.CurrentHp)} / {Mathf.RoundToInt(activeTrain.MaxHp)}";
        }

        if (Text_Defense != null)
        {
            Text_Defense.text = $"방어력 : {Mathf.RoundToInt(activeTrain.Defense)}";
        }
    }

    private void RefreshCargo()
    {
        if (Text_Amount != null && NetworkResourceService.Instance != null)
        {
            Text_Amount.text = $"현재 적제량 : {NetworkResourceService.Instance.CurrentCargoLoad.ToString()}";
        }

        if (Text_Resource != null && NetworkResourceService.Instance != null)
        {
            Text_Resource.text = $"적제 한계량 : {NetworkResourceService.Instance.CargoLimit.ToString()}";
        }

        if (Text_Boarding != null && NetworkTrainCargoService.Instance != null)
        {
            Text_Boarding.text = $"탑승 가능 : {NetworkTrainCargoService.Instance.BoardedCitizenCount} / {NetworkTrainCargoService.Instance.BoardingLimit}";
        }
    }

    private void RefreshDrone()
    {
        if (DroneManager.Instance == null)
        {
            return;
        }

        if (Text_CollectSpeed != null)
        {
            Text_CollectSpeed.text = $"체집 속도 : x{DroneManager.Instance.GatherSpeedMultiplier:0.00}";
        }

        if (Text_CollectEfficiency != null)
        {
            Text_CollectEfficiency.text = $"체집 효율 : +{DroneManager.Instance.YieldBonus}";
        }

        if (Text_ActionSpeed != null)
        {
            Text_ActionSpeed.text = $"행동 속도 : x{DroneManager.Instance.MoveSpeedMultiplier:0.00}";
        }
    }
}
