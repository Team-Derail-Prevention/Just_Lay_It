using System;
using System.Collections.Generic;
using UnityEngine;

public class TrainManager : SingletonBase<TrainManager>
{
    public bool IsStation { get; private set; } = false;

    [Header("Train Carriage Setting")]
    [SerializeField] private Transform _headTrain;
    [SerializeField] private float _followDistance = 3f;

    [Header("Connected Train Carriages")]
    public List<GameObject> carList = new List<GameObject>();

    [Header("Test Settings")]
    [SerializeField] private GameObject _headPrefab;
    [SerializeField] private GameObject[] _testCarPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private int _defaultCarriageCount = 3;

    private HashSet<Transform> visitedStation = new HashSet<Transform>();

    public static event Action<Transform> OnTrainSpawn;
    public static event Action<bool> OnStationState;
    public static event Action OnTrainRelocated;

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

        visitedStation.Clear();
        IsStation = false;
    }




    // 기본 테스트용 소환 (인스펙터에 등록된 스폰스팟 기준)
    [ContextMenu("Test / Spawn Carriage")]
    public void SpawnFullTrain(int carriageCount)
    {
        Vector3 spawnPos = (_spawnPoint != null) ? _spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = (_spawnPoint != null) ? _spawnPoint.rotation : Quaternion.identity;

        SpawnFullTrain(spawnPos, spawnRot, carriageCount);
    }
    //


    public void SpawnFullTrain(Vector3 spawnPos, Quaternion spawnRot, int carriageCount)
    {
        if (_headPrefab == null)
        {
            Debug.LogWarning("[TrainManager] Head Prefab이 할당되지 않았습니다.");
            return;
        }

        ClearExistingTrain();

        //  기차 헤드 소환 및 회전값 등록
        GameObject newHead = Instantiate(_headPrefab, spawnPos, spawnRot);
        if (newHead == null)
        {
            Debug.LogError("[TrainManager] 기관차 Instantiate 생성에 실패했습니다.");
            return;
        }

        _headTrain = newHead.transform;
        _headTrain.position = spawnPos;
        _headTrain.rotation = spawnRot;

        Train trainScript = newHead.GetComponent<Train>();
        if (trainScript != null && DataManager.Instance != null)
        {
            TrainData headData = DataManager.Instance.GetData<TrainData>("TRAIN_HEAD_01");
            trainScript.TrainInit(headData);
        }

        if (_testCarPrefab != null && _testCarPrefab.Length > 0)
        {
            int count = Mathf.Min(carriageCount, _testCarPrefab.Length);

            for (int i = 0; i < count; i++)
            {
                TrainData carData = null;
                if (DataManager.Instance != null)
                {
                    carData = DataManager.Instance.GetData<TrainData>("TRAIN_CARGO_01");
                }

                SpawnCarriage(_testCarPrefab[i], carData);
            }
        }

        OnTrainSpawn?.Invoke(_headTrain);

        Debug.Log($"[TrainManager] 기관차 1대와 객차 {carriageCount}대 전체 소환 완료!");
    }

    public void RelocateTrain(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (_headTrain == null)
        {
            Debug.Log("[TrainManager] 재배치할 기차 헤드가 없습니다.");
            return;
        }

        _headTrain.position = spawnPos;
        _headTrain.rotation = spawnRot;

        Train head = _headTrain.GetComponent<Train>();
        if (head != null)
        {
            head.SetTargetIndex(0);
        }

        Vector3 backspawn = -(spawnRot * Vector3.forward);
        Transform front = _headTrain;

        for (int i = 0; i < carList.Count; i++)
        {
            if (carList[i] != null && front != null)
            {
                Vector3 carPos = front.position + (backspawn * _followDistance);
                carList[i].transform.position = carPos;
                carList[i].transform.rotation = spawnRot;

                TrainFollow follow = carList[i].GetComponent<TrainFollow>();
                if (follow != null)
                {
                    follow.SetFrontTrain(front);
                }

                front = carList[i].transform;
            }
        }

        OnTrainRelocated?.Invoke();

        Debug.Log($"[TrainManager] 출구 위치({spawnPos})로 기차 재배치 완료!");
    }



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
        if (RailManager.Instance != null)
        {
            return RailManager.Instance.GetRailNode(index);
        }
        return null;
    }

    public void ArriveStation(GameObject stationObj)
    {
        if (IsStation)
        {
            return;
        }

            visitedStation.Add(stationObj.transform);
            IsStation = true;
            SetCarriagesActive(false);
            Debug.Log("[TrainManager] 기차역 도착 : 정차 상태");
            OnStationState?.Invoke(true);
    }

    public void DepartStation()
    {
        IsStation = false;

        SetCarriagesActive(true);
        OnStationState?.Invoke(false);
        Debug.Log("[TrainManager] 기차역 출발 : 이동 상태");

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
