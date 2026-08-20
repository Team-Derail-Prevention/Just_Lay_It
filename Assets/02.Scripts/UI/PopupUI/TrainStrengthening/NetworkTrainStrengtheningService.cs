using UnityEngine;

public class NetworkTrainStrengtheningService : SingletonBase<NetworkTrainStrengtheningService>
{
    // 적제 업글에 따른 해금 칸 정해지면 추후 수정
    private const int CARGO_BASE_UNLOCKED_COUNT = 3;

    private TrainStrengtheningViewModel _localVm;
    private string _pendingSlotDataId;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public TrainStrengtheningViewModel GetLocalTrainStrengtheningViewModel()
    {
        if (_localVm == null)
        {
            _localVm = new TrainStrengtheningViewModel();
            CreateSlotsFromData();
        }

        return _localVm;
    }

    private void CreateSlotsFromData()
    {
        if (DataManager.Instance == null || DataManager.Instance.IsLoaded == false)
        {
            Debug.LogWarning("[NetworkTrainStrengtheningService] 데이터가 아직 로드되지 않았습니다.");
            return;
        }

        var dataList = DataManager.Instance.GetAllData<TrainStrengtheningData>();

        foreach (var data in dataList)
        {
            bool isParsed = System.Enum.TryParse(data.Category, out TrainStatCategory category);
            if (isParsed == false)
            {
                Debug.LogWarning($"[NetworkTrainStrengtheningService] 알 수 없는 카테고리입니다 : {data.Category}");
                continue;
            }

            var slotVm = new TrainStatSlotViewModel(data.Id, category, data.DisplayName, data.IconPath, data.MaxLevel, data.BaseCost, data.CostIncreasePerLevel);
            _localVm.AddSlot(slotVm);
        }
    }

    public void RequestPurchase(string slotDataId)
    {
        var vm = GetLocalTrainStrengtheningViewModel();
        var slotVm = vm.GetSlot(slotDataId);
        if (slotVm == null || slotVm.IsMaxLevel == true)
        {
            return;
        }

        _pendingSlotDataId = slotDataId;
        MoneyRequestEventHub.Instance.RequestSpendMoney(slotVm.NextCost, OnPurchaseResult);
    }

    private void OnPurchaseResult(bool isApproved)
    {
        if (isApproved == false)
        {
            Debug.LogWarning("[NetworkTrainStrengtheningService] 재화가 부족합니다.");
            return;
        }

        var vm = GetLocalTrainStrengtheningViewModel();
        var slotVm = vm.GetSlot(_pendingSlotDataId);
        if (slotVm == null)
        {
            return;
        }

        slotVm.LevelUp();
        ApplyEffect(slotVm.SlotDataId, slotVm.Category, slotVm.CurrentLevel);
    }

    // 열차 상태 스탯 시스템이 정해지면 추후 수정
    private void ApplyEffect(string slotDataId, TrainStatCategory category, int newLevel)
    {
        if (category == TrainStatCategory.Cargo)
        {
            int unlockedCount = CARGO_BASE_UNLOCKED_COUNT + newLevel;
            NetworkAugmentService.Instance.SetEquipUnlockedCount(unlockedCount);
        }


        // 추후 열차 스텟과 연동 추후 수정
    }

    public void ResetRun()
    {
        _localVm = new TrainStrengtheningViewModel();
        CreateSlotsFromData();
    }
}
