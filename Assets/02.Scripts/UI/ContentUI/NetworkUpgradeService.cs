using UnityEngine;

public class NetworkUpgradeService : SingletonBase<NetworkUpgradeService>
{
    private UpgradeViewModel _localUpgradeViewModel;
    private bool _isSlotsCreated;

    [Header("보상 설정")]
    private const int RESCUE_REWARD_BASE = 50;
    private const int RESCUE_REWARD_STEP = 5;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnDataLoadCompleted += OnDataLoadCompleted;
        }
    }

    private void OnDestroy()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnDataLoadCompleted -= OnDataLoadCompleted;
        }
    }

    private void OnDataLoadCompleted()
    {
        CreateSlotsFromData();
    }


    public UpgradeViewModel GetLocalUpgradeViewModel()
    {
        if (_localUpgradeViewModel == null)
        {
            _localUpgradeViewModel = new UpgradeViewModel();
        }

        if (_isSlotsCreated == false)
        {
            CreateSlotsFromData();
        }

        return _localUpgradeViewModel;
    }

    private void CreateSlotsFromData()
    {
        if (_isSlotsCreated == true)
        {
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.IsLoaded == false)
        {
            Debug.LogWarning("[NetworkUpgradeService] 데이터가 아직 로드되지 않았습니다.");
            return;
        }

        var dataList = DataManager.Instance.GetAllData<LobbyUpgradeData>();

        foreach (var data in dataList)
        {
            var slotVm = new UpgradeSlotViewModel(data.Id, data.Name, data.IconPath, data.Description, data.MaxLevel, data.BaseCost, data.CostIncreasePerLevel);
            _localUpgradeViewModel.AddSlot(slotVm);
        }

        _isSlotsCreated = true;
    }

    public bool RequestPurchase(string slotDataId)
    {
        var vm = GetLocalUpgradeViewModel();
        var slotVm = vm.GetSlot(slotDataId);
        if (slotVm == null)
        {
            Debug.LogWarning($"[NetworkUpgradeService] 슬롯을 찾을 수 없습니다 : {slotDataId}");
            return false;
        }

        if (slotVm.IsMaxLevel == true)
        {
            Debug.LogWarning("[NetworkUpgradeService] 이미 최대 레벨입니다.");
            return false;
        }

        if (vm.TrySpendCash(slotVm.NextCost) == false)
        {
            Debug.LogWarning("[NetworkUpgradeService] 캐쉬가 부족합니다.");
            return false;
        }
        
        slotVm.LevelUp();
        ApplyEffect(slotDataId, slotVm.CurrentLevel);

        return true;
    }

    private void ApplyEffect(string slotDataId, int newLevel)
    {
        UpgradeEventHub.Instance.NotifyLobbyUpgraded(slotDataId, newLevel);
    }

    public bool RequestRefund(string slotDataId)
    {
        var vm = GetLocalUpgradeViewModel();
        var slotVm = vm.GetSlot(slotDataId);
        if (slotVm == null || slotVm.CurrentLevel <= 0)
        {
            return false;
        }

        int refundAmount = CalcRefundAmount(slotVm);
        slotVm.LevelDown();
        vm.GainCash(refundAmount);

        return true;
    }

    public bool HasAnyRefundableSlot()
    {
        var vm = GetLocalUpgradeViewModel();

        foreach (var slotKv in vm.SlotDic)
        {
            if (slotKv.Value.CurrentLevel > 0)
            {
                return true;
            }
        }

        return false;
    }

    public void RequestRefundAll()
    {
        var vm = GetLocalUpgradeViewModel();

        foreach (var slotKv in vm.SlotDic)
        {
            var slotVm = slotKv.Value;
            while (slotVm.CurrentLevel > 0)
            {
                int refundAmount = CalcRefundAmount(slotVm);
                slotVm.LevelDown();
                vm.GainCash(refundAmount);
            }
        }
    }

    private int CalcRefundAmount(UpgradeSlotViewModel slotVm)
    {
        return slotVm.CurrentLevelCost;
    }

    public void GainCash(int amount)
    {
        var vm = GetLocalUpgradeViewModel();
        vm.GainCash(amount);
    }

    public int GrantRescueReward(int rescuedHumanCount)
    {
        if (rescuedHumanCount <= 0)
        {
            return 0;
        }

        int rewardCash = rescuedHumanCount * GetCashPerRescuedCitizen();
        GainCash(rewardCash);

        Debug.Log($"[NetworkUpgradeService] 구출한 시민 {rescuedHumanCount}명 → 보상 캐쉬 {rewardCash} 지급");

        return rewardCash;
    }

    private int GetCashPerRescuedCitizen()
    {
        int level = GetLocalUpgradeViewModel().GetSlot("LOBBY_RESCUE_REWARD")?.CurrentLevel ?? 0;
        return RESCUE_REWARD_BASE + (level * RESCUE_REWARD_STEP);
    }

#if UNITY_EDITOR
    public void Debug_ResetUpgradeState()
    {
        UpgradeViewModel vm = GetLocalUpgradeViewModel();

        foreach (var slotKv in vm.SlotDic)
        {
            UpgradeSlotViewModel slotVm = slotKv.Value;
            while (slotVm.CurrentLevel > 0)
            {
                slotVm.LevelDown();
            }
        }

        vm.CurrentCash = 0;

        Debug.Log("[NetworkUpgradeService] 업그레이드 상태(캐시, 슬롯 레벨)를 초기화했습니다.");
    }
#endif
}
