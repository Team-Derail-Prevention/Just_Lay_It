using UnityEngine;

public class NetworkTrainStrengtheningService : SingletonBase<NetworkTrainStrengtheningService>
{
    private TrainStrengtheningViewModel _localVm;

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

        var dataList = DataManager.Instance.GetAllData<InGameUpgradeData>();

        foreach (var data in dataList)
        {
            bool isParsed = System.Enum.TryParse(data.Category, out TrainStatCategory category);
            if (isParsed == false)
            {
                Debug.LogWarning($"[NetworkTrainStrengtheningService] 알 수 없는 카테고리입니다 : {data.Category}");
                continue;
            }

            var slotVm = new TrainStatSlotViewModel(data.Id, category, data.Name, data.IconPath, data.MaxLevel, data.BaseCost, data.CostIncreasePerLevel);
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

        MoneyRequestEventHub.Instance.RequestSpendMoney(slotVm.NextCost, isApproved => OnPurchaseResult(slotDataId, isApproved));
    }

    private void OnPurchaseResult(string slotDataId, bool isApproved)
    {
        if (isApproved == false)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "돌이 부족합니다.");
            return;
        }

        var vm = GetLocalTrainStrengtheningViewModel();
        var slotVm = vm.GetSlot(slotDataId);
        if (slotVm == null)
        {
            return;
        }

        slotVm.LevelUp();
        ApplyEffect(slotVm.SlotDataId, slotVm.CurrentLevel);

        SoundManager.Instance?.PlaySFX(SfxAddress.Ui.Purchase);
    }

    private void ApplyEffect(string slotDataId, int newLevel)
    {
        if (slotDataId == "CARGO_WEAPON_LIMIT")
        {
            NetworkAugmentService.Instance.ApplyWeaponSlotUnlockLevel(newLevel);
        }

        UpgradeEventHub.Instance.NotifyInGameUpgraded(slotDataId, newLevel);
    }


    public void ResetRun()
    {
        _localVm = new TrainStrengtheningViewModel();
        CreateSlotsFromData();
    }
}
