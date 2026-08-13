using System;
using System.Collections.Generic;
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance { get; private set; }
    public bool IsStation { get; private set; } = false;


    [Header("Rail Detect Setting")]
    private string _railTag = "Rail";
    private float _detectRadius = 1.0f;

    [Header("Station Detect Setting")]
    private string _stationTag = "Station";
    private float _stationDetectRadius = 1.0f;

    [Header("Train Carriage Setting")]
    [SerializeField] private Transform _headTrain;
    [SerializeField] private float _followDistance = 1.2f;

    [Header("Total Rail Path Data")]
    public List<Transform> pathList = new List<Transform>();

    [Header("Connected Train Carriages")]
    public List<GameObject> carList = new List<GameObject>();

    [Header("Test Settings")]
    [SerializeField] private GameObject _headPrefab;        
    [SerializeField] private GameObject _testCarPrefab;     
    [SerializeField] private GameObject _testTurretPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private int _defaultCarriageCount = 3;

    private HashSet<Transform> visitedNode = new HashSet<Transform>();
    private HashSet<Transform> visitedStation = new HashSet<Transform>();

    public static event Action<Transform> OnTrainSpawn;
    public static event Action<bool> OnStationState;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
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

    public void DetectRail(Vector3 currentPos)
    {
        Collider[] hits = Physics.OverlapSphere(currentPos, _detectRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag(_railTag))
            {
                Transform railTransform = hits[i].transform;

                if (!visitedNode.Contains(railTransform))
                {
                    visitedNode.Add(railTransform);
                    pathList.Add(railTransform);
                }
            }
        }
    }

    [ContextMenu("Test / Spawn Carriage")]
    public void TestSpawnCarriage()
    {
        if (_testCarPrefab != null)
        {
            SpawnCarriage(_testCarPrefab);
        }
        else
        {
            Debug.LogWarning("[TrainManager] Test Car Prefab이 등록되지 않았습니다.");
        }
    }
    public void SpawnFullTrain(int carriageCount)
    {
        if (_headPrefab == null)
        {
            Debug.LogWarning("[TrainManager] Head Prefab이 할당되지 않았습니다.");
            return;
        }

        // 1) 스폰 포인트 지정 여부 확인 후 위치/회전 세팅
        Vector3 spawnPos = (_spawnPoint != null) ? _spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = (_spawnPoint != null) ? _spawnPoint.rotation : Quaternion.identity;

        // 2) 기관차(Head) 소환 및 메인 Head로 등록
        GameObject newHead = Instantiate(_headPrefab, spawnPos, spawnRot);
        _headTrain = newHead.transform;

        // 3) 입력한 개수만큼 객차 순차적 추가
        if (_testCarPrefab != null)
        {
            for (int i = 0; i < carriageCount; i++)
            {
                SpawnCarriage(_testCarPrefab);
            }
        }

        OnTrainSpawn?.Invoke(_headTrain);

        Debug.Log($"[TrainManager] 기관차 1대와 객차 {carriageCount}대 전체 소환 완료!");
    }
    //

    public void SpawnCarriage(GameObject carPrefab)
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

        TrainFollow followtrain = newCar.GetComponent<TrainFollow>();
        if (followtrain != null)
        {
            followtrain.SetFrontTrain(frontCar);
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

    public void DetectStation(Vector3 currentPos)
    {
        if (IsStation)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(currentPos, _stationDetectRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag(_stationTag))
            {
                Transform stationTransform = hits[i].transform;
                if (!visitedStation.Contains(stationTransform))
                {
                    visitedStation.Add(stationTransform);
                    ArriveStation(stationTransform.gameObject);
                    break;
                }
            }
        }
    }

    public void ArriveStation(GameObject stationObj)
    {
        IsStation = true;
        SetCarriagerActive(false);
        Debug.Log("기차역 도착");

        OnStationState?.Invoke(true);
    }

    public void DepartStation()
    {
        IsStation = false;
        SetCarriagerActive(true);
        Debug.Log("기차역 출발");

        OnStationState?.Invoke(false);
    }

    public void SetCarriagerActive(bool isActive)
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
