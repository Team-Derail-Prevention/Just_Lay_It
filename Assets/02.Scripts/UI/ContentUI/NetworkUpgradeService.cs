using UnityEngine;

public class NetworkUpgradeService : SingletonBase<NetworkUpgradeService>
{
    private UpgradeViewModel _localUpgradeViewModel;

    [Header("보상 설정")]
    [SerializeField] private int _cashPerRescuedCitizen = 100;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public UpgradeViewModel GetLocalUpgradeViewModel()
    {
        if (_localUpgradeViewModel == null)
        {
            CreateLocalUpgradeViewModel();
        }

        return _localUpgradeViewModel;
    }

    private void CreateLocalUpgradeViewModel()
    {
        _localUpgradeViewModel = new UpgradeViewModel();
        CreateSlotsFromData();
    }

    private void CreateSlotsFromData()
    {
        if (DataManager.Instance == null || DataManager.Instance.IsLoaded == false)
        {
            Debug.LogWarning("[NetworkUpgradeService] 데이터가 아직 로드되지 않았습니다.");
            return;
        }

        var dataList = DataManager.Instance.GetAllData<LobbyUpgradeData>();

        foreach (var data in dataList)
        {
            var slotVm = new UpgradeSlotViewModel(data.Id, data.Name, data.IconPath, data.MaxLevel, data.BaseCost, data.CostIncreasePerLevel);
            _localUpgradeViewModel.AddSlot(slotVm);
        }
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

        // 저장 관련 정해지면 추후 수정

        return true;
    }

    private void ApplyEffect(string slotDataId, int newLevel)
    {
        if (slotDataId == "LOBBY_RESCUE_REWARD")
        {
            _cashPerRescuedCitizen += 5;
        }

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

        // 저장 관련 정해지면 추후 수정

        return true;
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

        // 저장 관련 정해지면 추후 수정
    }

    private int CalcRefundAmount(UpgradeSlotViewModel slotVm)
    {
        return slotVm.CurrentLevelCost;
    }

    public void GainCash(int amount)
    {
        var vm = GetLocalUpgradeViewModel();
        vm.GainCash(amount);

        // 저장 관련 정해지면 추후 수정
    }

    public void GrantRescueReward(int rescuedHumanCount)
    {
        if (rescuedHumanCount <= 0)
        {
            return;
        }

        int rewardCash = rescuedHumanCount * _cashPerRescuedCitizen;
        GainCash(rewardCash);

        Debug.Log($"[NetworkUpgradeService] 구출한 시민 {rescuedHumanCount}명 → 보상 캐쉬 {rewardCash} 지급");
    }

    public object GetSaveData()
    {
        // 저장 관련 정해지면 추후 수정
        return null;
    }

    public void LoadSaveData(object saveData)
    {
        // 저장 관련 정해지면 추후 수정
    }
}
