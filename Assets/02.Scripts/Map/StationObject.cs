using System;
using UnityEngine;

public class StationObject : BaseColliderTrigger
{
    public static event Action<StationObject, string> OnStationEntered;

    [Header("기차역 이벤트 데이터")]
    [SerializeField] private string _stationId;

    [System.Serializable]
    public struct RailSpawnInfo
    {
        [Tooltip("레일의 월드 좌표 위치")]
        public Vector3 position;
        [Tooltip("레일의 회전값")]
        public Quaternion rotation;
    }

    [Header("스폰 포인트 (인게임 자동 등록 및 확인용)")]
    [SerializeField] private RailSpawnInfo[] _startPoints = new RailSpawnInfo[2];

    [Header("방향별 레일 루트 (인게임 자동 등록)")]
    [SerializeField] public Transform[] _exitDirRoots = new Transform[2]; 

    private int _rewardGold = 0;
    private int _rescueCount = 0;

    public int AvailableStone => _rewardGold;
    public int AvailableCitizen => _rescueCount;

    public string StationId => _stationId;
    private bool _isInteractionCompleted = false;

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

    public void RegisterStartPoint(int index, Vector3 pos, Quaternion rot)
    {
        if (index >= 0 && index < _startPoints.Length)
        {
            _startPoints[index].position = pos;
            _startPoints[index].rotation = rot;
        }
    }

    public RailSpawnInfo GetStartPoint(int index)
    {
        return _startPoints[index];
    }

    protected override void HandleInteraction(Collider target)
    {
        if (_isInteractionCompleted || !CanInteract(target)) return;

        OnStationEntered?.Invoke(this, StationId);
    }

    public void ExitStation(int stoneTaken, int citizenBoarded)
    {
        _isInteractionCompleted = true;

        if (NetworkResourceService.Instance != null)
        {
            NetworkResourceService.Instance.AddRescuedHuman(citizenBoarded);
        }

        Debug.Log($"[StationObject] '{_stationId}' 완료. 골드 {_rewardGold} 획득, 구출 {_rescueCount}명.");
    }

    public void RegisterExitDirRoot(int dirIndex, Transform dirRoot)
    {
        if (dirIndex >= 0 && dirIndex < _exitDirRoots.Length)
        {
            _exitDirRoots[dirIndex] = dirRoot;
        }
    }

    public void RemoveStationAndRails()
    {
        foreach (Transform railRoot in _exitDirRoots)
        {
            if (railRoot != null)
            {
                Destroy(railRoot.gameObject);
            }
        }

        Destroy(gameObject);
    }
}