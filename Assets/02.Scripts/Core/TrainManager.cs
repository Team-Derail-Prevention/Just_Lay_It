using System;
using System.Collections.Generic;
using UnityEngine;

public class TrainManager : SingletonBase<TrainManager>
{
    public bool IsStation { get; private set; } = false;

    [Header("Train Carriage Setting")]
    [SerializeField] private Transform _headTrain;
    [SerializeField] private float _followDistance = 3f;

    [Header("Total Rail Path Data")]
    public List<Transform> pathList = new List<Transform>();

    [Header("Connected Train Carriages")]
    public List<GameObject> carList = new List<GameObject>();

    [Header("Test Settings")]
    [SerializeField] private GameObject _headPrefab;        
    [SerializeField] private GameObject[] _testCarPrefab;     
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private int _defaultCarriageCount = 3;

    private HashSet<Transform> visitedNode = new HashSet<Transform>();
    private HashSet<Transform> visitedStation = new HashSet<Transform>();

    public static event Action<Transform> OnTrainSpawn;
    public static event Action<bool> OnStationState;

    protected override void Init()
    {
        base.Init();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            DepartStation();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SpawnFullTrain(_defaultCarriageCount);
        }

    }

    public void ClearExistingTrain()
    {
        for (int i = 0; i < carList.Count; i++)
        {
            if (carList[i] != null)
            {
                Destroy(carList[i]);
            }
        }
        carList.Clear();

        if (_headTrain != null)
        {
            Destroy(_headTrain.gameObject);
            _headTrain = null;
        }
    }

    public void DetectRail(Transform railTransform)
    {
        if (railTransform == null)
        {
            return;
        }

        if (!visitedNode.Contains(railTransform))
        {
            visitedNode.Add(railTransform);
            pathList.Add(railTransform);
        }
      
    }

    [ContextMenu("Test / Spawn Carriage")]
    public void SpawnFullTrain(int carriageCount)
    {
        if (_headPrefab == null)
        {
            Debug.LogWarning("[TrainManager] Head Prefab이 할당되지 않았습니다.");
            return;
        }


        ClearExistingTrain();

        // 1) 스폰 포인트 지정 여부 확인 후 위치/회전 세팅
        Vector3 spawnPos = (_spawnPoint != null) ? _spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = (_spawnPoint != null) ? _spawnPoint.rotation : Quaternion.identity;

        // 2) 기관차(Head) 소환 및 메인 Head로 등록
        GameObject newHead = Instantiate(_headPrefab, spawnPos, spawnRot);
        if (newHead == null)
        {
            Debug.LogError("[TrainManager] 기관차 Instantiate 생성에 실패했습니다.");
            return;
        }

        _headTrain = newHead.transform;
        _headTrain.rotation = spawnRot;

        // 3) 입력한 개수만큼 객차 순차적 추가
        if (_testCarPrefab != null && _testCarPrefab.Length > 0)
        {
            int count = Mathf.Min(carriageCount, _testCarPrefab.Length);

            for (int i = 0; i < count; i++)
            {
                SpawnCarriage(_testCarPrefab[i]);
            }
        }

        OnTrainSpawn?.Invoke(_headTrain);

        Debug.Log($"[TrainManager] 기관차 1대와 객차 {carriageCount}대 전체 소환 완료!");
    }
    //

    public void SpawnCarriage(GameObject carPrefab, TrainData data = null)
    {
        if (carPrefab == null)
        {
            return;
        }

        Transform frontCar = null;

        if (carList.Count == 0)
        {
            frontCar = _headTrain;
        }
        else
        {
            frontCar = carList[carList.Count - 1].transform;
        }

        if (frontCar == null)
        {
            Debug.LogWarning("[TrainManager] 앞 차(frontCar) 정보가 없어 생성할 수 없습니다.");
            return;
        }

        Vector3 spawnPos = frontCar.position - (frontCar.forward * _followDistance);
        Quaternion spawnRot = frontCar.rotation;

        GameObject newCar = Instantiate(carPrefab, spawnPos, spawnRot);

        TrainFollow followTrain = newCar.GetComponent<TrainFollow>();
        if (followTrain != null)
        {
            followTrain.FollowInit(data, frontCar);
        }

        TrainContainer trainContainer = newCar.GetComponent<TrainContainer>();
        if (trainContainer != null && data != null)
        {
            trainContainer.ContainerInit(data);
        }



            carList.Add(newCar);
    }


    public Transform GetWaypoint(int index)
    {
        if (index >= 0 && index < pathList.Count)
        {
            return pathList[index];
        }
        return null;
    }

    public void ArriveStation(GameObject stationObj)
    {
        if (IsStation)
        {
            return;
        }

        if (!visitedStation.Contains(stationObj.transform))
        {
            visitedStation.Add(stationObj.transform);
            IsStation = true;
            SetCarriagesActive(false);
            Debug.Log("[TrainManager] 기차역 도착 : 정차 상태");

            OnStationState?.Invoke(true);
        }
    }

    public void DepartStation()
    {
        IsStation = false;
        SetCarriagesActive(true);
        Debug.Log("[TrainManager] 기차역 출발 : 이동 상태");

        OnStationState?.Invoke(false);
    }

    public void SetCarriagesActive(bool isActive)
    {
        for (int i = 0; i < carList.Count; i++)
        {
            if (carList[i] != null)
            {
                carList[i].SetActive(isActive);
            }
        }
    }

}
