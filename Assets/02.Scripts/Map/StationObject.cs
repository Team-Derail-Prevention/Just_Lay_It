using System;
using UnityEngine;

public class StationObject : BaseColliderTrigger
{
    public static event Action<StationObject, string> OnStationEntered;

    [Header("기차역 이벤트 데이터")]
    [SerializeField] private string _stationId;

    public string StationId => _stationId;
    private bool _isInteractionComplted = false;

    public void Initailze(string stationId)
    {
        _stationId = stationId;
        _isInteractionComplted = false;

        // TODO: DataManager에서 기차역 데이터를 가져와서 초기화
    }

    protected override void HandleInteraction(Collider target)
    {
        if (_isInteractionComplted || !CanInteract(target))
        {
            return;
        }

        Time.timeScale = 0f;

        OnStationEntered?.Invoke(this, StationId);
    }

    public void ExitStation(bool isHealed)
    {
        _isInteractionComplted = true;

        if (isHealed)
        {
            // TODO: 기차체력 회복 및 자재 소모
        }

        Time.timeScale = 1f;
    }
}