using UnityEngine;

public class NetworkUpgradeService : SingletonBase<NetworkUpgradeService>
{
    private UpgradeViewModel _localUpgradeViewModel;

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

        // 업글 내용이 정해지면 추후 수정
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

        if (vm.TrySpendGold(slotVm.NextCost) == false)
        {
            Debug.LogWarning("[NetworkUpgradeService] 골드가 부족합니다.");
            return false;
        }
        
        slotVm.LevelUp();

        // 저장 관련 정해지면 추후 수정

        return true;
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
        vm.GainGold(refundAmount);

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
                vm.GainGold(refundAmount);
            }
        }

        // 저장 관련 정해지면 추후 수정
    }

    private int CalcRefundAmount(UpgradeSlotViewModel slotVm)
    {
        return slotVm.NextCost;
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
