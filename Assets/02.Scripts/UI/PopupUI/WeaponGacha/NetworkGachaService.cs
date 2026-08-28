using UnityEngine;
using System.Collections.Generic;

public class NetworkGachaService : SingletonBase<NetworkGachaService>
{
    private const int REROLL_COUNT_DEFAULT = 3; 
    public const int REROLL_COST_SINGLE = 1;
    public const int REROLL_COST_ALL = 3;
    private const int GACHA_BASE_COST = 30;
    private const int GACHA_COST_INCREASE_PER_PULL = 5;
    private int _gachaPullCount = 0;

    private GachaViewModel _localVm;
    private List<WeaponData> _dataPool;

    public int CurrentGachaCost
    {
        get
        {
            return GACHA_BASE_COST + (_gachaPullCount * GACHA_COST_INCREASE_PER_PULL);
        }
    }

    private void OnEnable()
    {
        UpgradeEventHub.Instance.OnLobbyUpgraded += OnLobbyUpgraded;
    }

    private void OnDisable()
    {
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnLobbyUpgraded -= OnLobbyUpgraded;
        }
    }

    private void OnLobbyUpgraded(string slotDataId, int newLevel)
    {
        if (slotDataId == "LOBBY_GACHA_REROLL")
        {
            ApplyRerollCountMaxUpgrade(REROLL_COUNT_DEFAULT + newLevel);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public GachaViewModel GetLocalGachaViewModel()
    {
        if (_localVm == null)
        {
            _localVm = new GachaViewModel();
            _localVm.SetRerollCount(REROLL_COUNT_DEFAULT, REROLL_COUNT_DEFAULT);
        }

        return _localVm;
    }

    public void ResetRun()
    {
        _localVm = null;
        _gachaPullCount = 0;
    }

    private List<WeaponData> GetDataPool()
    {
        if (_dataPool == null)
        {
            if (DataManager.Instance == null || DataManager.Instance.IsLoaded == false)
            {
                Debug.LogWarning("[NetworkGachaService] 데이터가 아직 로드되지 않았습니다.");
                return new List<WeaponData>();
            }

            _dataPool = new List<WeaponData>(DataManager.Instance.GetAllData<WeaponData>());
        }

        return _dataPool;
    }

    public bool OpenGachaBox()
    {
        if (MoneyRequestEventHub.Instance == null)
        {
            return false;
        }

        bool isSpent = false;
        MoneyRequestEventHub.Instance.RequestSpendMoney(CurrentGachaCost, result => isSpent = result);

        if (isSpent == false)
        {
            Debug.LogWarning("[NetworkGachaService] 가챠 비용이 부족합니다.");
            return false;
        }

        _gachaPullCount++;

        var vm = GetLocalGachaViewModel();
        vm.SetRerollCount(vm.RerollCountMax, vm.RerollCountMax);

        for (int i = 0; i < GachaViewModel.CARD_SLOT_COUNT; i++)
        {
            DrawCardIntoSlot(i);
        }

        return true;
    }

    public void ApplyRerollCountMaxUpgrade(int newMax)
    {
        var vm = GetLocalGachaViewModel();
        vm.SetRerollCount(vm.RerollCountCurrent, newMax);
    }

    public bool RequestRerollSingle(int slotIndex)
    {
        var vm = GetLocalGachaViewModel();
        if (vm.RerollCountCurrent < REROLL_COST_SINGLE)
        {
            Debug.LogWarning("[NetworkGachaService] 재굴림 잔여 횟수가 부족합니다.");
            return false;
        }

        vm.SetRerollCount(vm.RerollCountCurrent - REROLL_COST_SINGLE, vm.RerollCountMax);
        DrawCardIntoSlot(slotIndex);
        return true;
    }

    public bool RequestRerollAll()
    {
        var vm = GetLocalGachaViewModel();
        if (vm.RerollCountCurrent < REROLL_COST_ALL)
        {
            Debug.LogWarning("[NetworkGachaService] 전체 재굴림에 필요한 횟수가 부족합니다.");
            return false;
        }

        vm.SetRerollCount(vm.RerollCountCurrent - REROLL_COST_ALL, vm.RerollCountMax);

        for (int i = 0; i < GachaViewModel.CARD_SLOT_COUNT; i++)
        {
            DrawCardIntoSlot(i);
        }

        return true;
    }

    private void DrawCardIntoSlot(int slotIndex)
    {
        var vm = GetLocalGachaViewModel();
        var cardState = vm.GetCard(slotIndex);
        if (cardState == null)
        {
            return;
        }

        var pool = GetDataPool();
        if (pool.Count == 0)
        {
            return;
        }

        var pickedData = pool[Random.Range(0, pool.Count)];
        cardState.FillFromData(pickedData);
    }

    public void RequestSelectCard(int slotIndex)
    {
        var vm = GetLocalGachaViewModel();
        var cardState = vm.GetCard(slotIndex);
        if (cardState == null)
        {
            return;
        }

        // 실제 인벤토리 추가 처리 연동
        Debug.Log($"[NetworkGachaService] {cardState.DisplayName} 인벤토리 추가 요청");

        UIManager.Instance.CloseWeaponGachaUI();
        UIManager.Instance.OpenExitConfirmPopup(null, null, "무기가 인벤토리로 들어갔습니다.");
    }

}
