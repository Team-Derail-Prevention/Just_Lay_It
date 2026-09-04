using UnityEngine;

public class NetworkTrainCargoService : SingletonBase<NetworkTrainCargoService>
{
    private const int BASE_BOARDING_LIMIT = 5;
    private int _boardedCitizenCount;

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
            return BASE_BOARDING_LIMIT + GetInGameBoardingLimitBonus();
        }
    }

    public int RemainingBoardingCapacity
    {
        get
        {
            int remaining = BoardingLimit - _boardedCitizenCount;
            return Mathf.Max(0, remaining);
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

    public void UnloadAllCitizens()
    {
        _boardedCitizenCount = 0;
    }

    private int GetInGameBoardingLimitBonus()
    {
        if (NetworkTrainStrengtheningService.Instance == null)
        {
            return 0;
        }

        TrainStrengtheningViewModel strengtheningVm = NetworkTrainStrengtheningService.Instance.GetLocalTrainStrengtheningViewModel();
        TrainStatSlotViewModel slotVm = strengtheningVm?.GetSlot("CARGO_CREW_LIMIT");
        int level = slotVm != null ? slotVm.CurrentLevel : 0;

        return level * 2;
    }

    public void ResetRun()
    {
        _boardedCitizenCount = 0;
    }
}
