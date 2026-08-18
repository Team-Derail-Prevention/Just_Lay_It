using System;
using UnityEngine;

public class StationObject : BaseColliderTrigger
{
    public static event Action<StationObject, string> OnStationEntered;

    [Header("기차역 이벤트 데이터")]
    [SerializeField] private string _stationId;

    private int _rewardGold = 0;  
    private int _rescueCount = 0; 

    public string StationId => _stationId;
    private bool _isInteractionCompleted = false;

    // 맵이 생성될 때 MapManager 등에서 이 기차역의 ID를 넣어주며 초기화합니다.
    public void Initialize(string stationId)
    {
        _stationId = stationId;
        _isInteractionCompleted = false;

        if (GameManager.Data != null)
        {
            MapData myData = GameManager.Data.GetData<MapData>(_stationId);
            if (myData != null)
            {
                _rescueCount = myData.GuestCount;
                _rewardGold = myData.Gold; 
            }
        }
    }

    protected override void HandleInteraction(Collider target)
    {
        if (_isInteractionCompleted || !CanInteract(target)) return;

        Time.timeScale = 0f;
        OnStationEntered?.Invoke(this, StationId);
    }

    public void ExitStation(bool isHealed)
    {
        _isInteractionCompleted = true;

        if (isHealed)
        {
            // TODO: TrainManager로 기차 체력 회복 및 자재 소모 로직
        }

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyRescuedHumanChanged(_rescueCount);
        }

        if (NetworkUpgradeService.Instance != null && _rewardGold > 0)
        {
            NetworkUpgradeService.Instance.GainGold(_rewardGold);
        }

        if (ResourceStatusEventHub.Instance != null && _rewardGold > 0)
        {
            ResourceStatusEventHub.Instance.NotifyMoneyChanged(_rewardGold);
        }

        Debug.Log($"[StationObject] '{_stationId}' 완료. 골드 {_rewardGold} 획득, 구출 {_rescueCount}명.");

        Time.timeScale = 1f;
    }
}