using UnityEngine;

public class NetworkTrainCargoService : SingletonBase<NetworkTrainCargoService>
{
    private const int BASE_BOARDING_LIMIT = 20;
    private int _boardedCitizenCount;
    private int _boardingLimit = BASE_BOARDING_LIMIT;

    public int BoardedCitizenCount
    {
        get
        {
            return _boardedCitizenCount;
        }
    }

    public int BoardingLimit
    {
        get
        {
            return _boardingLimit;
        }
    }

    public int RemainingBoardingCapacity
    {
        get
        {
            int remaining = _boardingLimit - _boardedCitizenCount;
            return Mathf.Max(0, remaining);
        }
    }
    private void OnEnable()
    {
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnInGameUpgraded += OnInGameUpgraded;
        }
    }

    private void OnDisable()
    {
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnInGameUpgraded -= OnInGameUpgraded;
        }
    }

    private void OnInGameUpgraded(string slotDataId, int newLevel)
    {
        if (slotDataId == "CARGO_CREW_LIMIT")
        {
            IncreaseBoardingLimit(5); // 임시 값
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public int BoardCitizens(int amount)
    {
        int boardable = Mathf.Min(amount, RemainingBoardingCapacity);
        if (boardable <= 0)
        {
            Debug.LogWarning("[NetworkTrainCargoService] 탑승 정원이 가득 찼습니다.");
            return 0;
        }

        _boardedCitizenCount += boardable;
        return boardable;
    }

    public void IncreaseBoardingLimit(int amount)
    {
        _boardingLimit += amount;
    }

    public void ResetRun()
    {
        _boardedCitizenCount = 0;
        _boardingLimit = BASE_BOARDING_LIMIT;
    }
}
