using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class NetworkGachaService : SingletonBase<NetworkGachaService>
{
    private const int REROLL_COUNT_DEFAULT = 3; 
    public const int REROLL_COST_SINGLE = 1;
    public const int REROLL_COST_ALL = 3;
    private const int GACHA_BASE_COST = 50;
    private const int GACHA_COST_INCREASE_PER_PULL = 5;
    private int _gachaPullCount = 0;

    private static readonly Dictionary<string, int> GRADE_WEIGHT_TABLE = new Dictionary<string, int>
    {
        { "Common", 4 },
        { "Rare", 3 },
        { "Epic", 2 },
        { "Legendary", 1 }
    };

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
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnLobbyUpgraded += OnLobbyUpgraded;
        }
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

        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnLobbyUpgraded -= OnLobbyUpgraded;
            UpgradeEventHub.Instance.OnLobbyUpgraded += OnLobbyUpgraded;
        }
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

    public IReadOnlyList<WeaponData> GetGachaWeaponPool()
    {
        return GetDataPool();
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

        string pickedGrade = PickWeightedGrade(pool);
        List<WeaponData> gradePool = new List<WeaponData>();

        foreach (WeaponData data in pool)
        {
            if (data.GradeName == pickedGrade)
            {
                gradePool.Add(data);
            }
        }

        var pickedData = gradePool[Random.Range(0, gradePool.Count)];
        cardState.FillFromData(pickedData);
    }

    private string PickWeightedGrade(List<WeaponData> pool)
    {
        List<string> gradeNameList = new List<string>();
        List<int> weightList = new List<int>();
        int totalWeight = 0;

        foreach (WeaponData data in pool)
        {
            if (gradeNameList.Contains(data.GradeName))
            {
                continue;
            }

            int weight = 1;
            if (GRADE_WEIGHT_TABLE.ContainsKey(data.GradeName))
            {
                weight = GRADE_WEIGHT_TABLE[data.GradeName];
            }

            gradeNameList.Add(data.GradeName);
            weightList.Add(weight);
            totalWeight += weight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        for (int i = 0; i < gradeNameList.Count; i++)
        {
            cumulativeWeight += weightList[i];
            if (randomValue < cumulativeWeight)
            {
                return gradeNameList[i];
            }
        }

        return gradeNameList[gradeNameList.Count - 1];
    }

    public void RequestSelectCard(int slotIndex)
    {
        var vm = GetLocalGachaViewModel();
        var cardState = vm.GetCard(slotIndex);
        if (cardState == null)
        {
            return;
        }

        bool isAdded = NetworkAugmentService.Instance.AddAugment(cardState.WeaponDataId);
        if (isAdded == false)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "인벤토리에 빈 칸이 없습니다.");
            return;
        }

        UIManager.Instance.CloseWeaponGachaUI();
        UIManager.Instance.OpenExitConfirmPopup(null, null, "무기가 인벤토리로 들어갔습니다.");
    }

    public void PreloadGachaIcons()
    {
        PreloadGachaIconsAsync().Forget();
    }

    private async UniTaskVoid PreloadGachaIconsAsync()
    {
        var pool = GetDataPool();
        if (pool.Count == 0)
        {
            return;
        }

        var loadTaskList = new List<UniTask<Sprite>>(pool.Count);
        for (int i = 0; i < pool.Count; i++)
        {
            loadTaskList.Add(ResourceManager.Instance.LoadAssetWithRetry<Sprite>(pool[i].IconPath));
        }

        await UniTask.WhenAll(loadTaskList);
    }

    public void Debug_SimulateGachaProbability(int sampleCount)
    {
        var pool = GetDataPool();
        if (pool.Count == 0)
        {
            Debug.LogWarning("[NetworkGachaService] 데이터 풀이 비어있어 시뮬레이션할 수 없습니다.");
            return;
        }

        Dictionary<string, int> resultCountByGrade = new Dictionary<string, int>();

        for (int i = 0; i < sampleCount; i++)
        {
            string pickedGrade = PickWeightedGrade(pool);

            if (resultCountByGrade.ContainsKey(pickedGrade) == false)
            {
                resultCountByGrade[pickedGrade] = 0;
            }

            resultCountByGrade[pickedGrade]++;
        }

        int totalWeight = 0;
        foreach (KeyValuePair<string, int> weightPair in GRADE_WEIGHT_TABLE)
        {
            totalWeight += weightPair.Value;
        }

        Debug.Log($"[GachaProbability] 샘플 {sampleCount}회 시뮬레이션 결과");

        foreach (KeyValuePair<string, int> resultPair in resultCountByGrade)
        {
            string gradeName = resultPair.Key;
            int actualCount = resultPair.Value;
            float actualPercent = (float)actualCount / sampleCount * 100f;

            float expectedPercent = 0f;
            if (GRADE_WEIGHT_TABLE.ContainsKey(gradeName))
            {
                expectedPercent = (float)GRADE_WEIGHT_TABLE[gradeName] / totalWeight * 100f;
            }

            Debug.Log($"  {gradeName} : {actualCount}회 ({actualPercent:F2}% / 기대치 {expectedPercent:F2}%)");
        }
    }
}
