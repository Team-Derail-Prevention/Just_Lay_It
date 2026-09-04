using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TrainManager : SingletonBase<TrainManager>
{
    public bool IsStation { get; private set; } = false;
    public Transform HeadTransform => _headTrain;
    public Train ActiveTrain => _activeTrain;

    [Header("Train Carriage Setting")]
    [SerializeField] private float _followDistance = 3f;

    [Header("Spawn Carriage ID List")]
    [SerializeField]
    private List<string> _spawnCarriageIds = new List<string>
    {
        "TRAIN_STANDARD_01",
        "TRAIN_CARGO_01",
        "TRAIN_STANDARD_02"
    };

    public List<GameObject> carList = new List<GameObject>();
    private HashSet<Transform> visitedStation = new HashSet<Transform>();

    public static event Action<Transform> OnTrainSpawn;
    public static event Action<bool> OnStationState;
    public static event Action OnTrainRelocated;

    private Transform _headTrain;
    private Train _activeTrain;
    private Vector3 _lastEnterDirection = Vector3.forward;

    protected override void Init()
    {
        base.Init();
    }

    // 터미널 스폰 + 동,서,남,북 선택 시 이동
    public void SpawnTerminalTrain(int carriageCount)
    {
        SpawnTerminalTrainAsync(carriageCount).Forget();
    }

    public async UniTask SpawnTerminalTrainAsync(int carriageCount)
    {
        CentralTerminal terminal = GameManager.Map.MapRoot.GetComponentInChildren<CentralTerminal>();

        if (terminal == null)
        {
            Debug.LogError("[TrainManager] Terminal 정보가 없어 스폰할 수 없습니다.");
            return;
        }

        CentralTerminal.RailSpawnInfo startInfo = terminal.GetStartPoint(0);
        GameManager.Rail?.InitStartingRailPath(terminal.ExitDirRoots[0]);
        await SpawnFullTrainAsync(startInfo.position, startInfo.rotation, carriageCount);
    }

    public void SpawnStationTrain(StationObject station, int carriageCount)
    {
        SpawnStationTrainAsync(station, carriageCount).Forget();
    }       

    private async UniTaskVoid SpawnStationTrainAsync(StationObject station, int carriageCount)
    {
        if (station == null)
        {
            Debug.LogError("[TrainManager] StationObject 정보가 없어 스폰할 수 없습니다.");
            return;
        }

        //머리 진행 방향 기준 (0: 서쪽, 1: 동쪽)
        Vector3 forward = (_headTrain != null) ? _headTrain.forward : _lastEnterDirection;
        int exitIndex = (forward.x < 0.1f) ? 0 : 1;

        StationObject.RailSpawnInfo exitInfo = station.GetStartPoint(exitIndex);
        Transform exitDirRoot = station._exitDirRoots[exitIndex];

        if (exitDirRoot != null)
        {
            GameManager.Rail?.InitStartingRailPath(exitDirRoot);
        }

        await SpawnFullTrainAsync(exitInfo.position, exitInfo.rotation, carriageCount);
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


    public void SpawnFullTrain(Vector3 spawnPos, Quaternion spawnRot, int carriageCount)
    {
        SpawnFullTrainAsync(spawnPos, spawnRot, carriageCount).Forget();
    }

    public async UniTask SpawnFullTrainAsync(Vector3 spawnPos, Quaternion spawnRot, int carriageCount)
    {
        if (_headTrain != null)
        {
            RelocateExistingTrain(spawnPos, spawnRot);
            return;
        }

        ClearExistingTrain();

        TrainData headData = DataManager.Instance?.GetData<TrainData>("TRAIN_HEAD_01");
        if (headData == null || string.IsNullOrEmpty(headData.PrefabPath))
        {
            Debug.LogError("[TrainManager] TRAIN_HEAD_01 데이터 또는 PrefabPath 가 없습니다..");
            return;
        }

        GameObject headPrefab = await GameManager.Resource.LoadAsset<GameObject>(headData.PrefabPath);
        if (headPrefab == null)
        {
            Debug.LogError($"[TrainManager] 기관차 프리팹 로드 실패: {headData.PrefabPath}");
            return;
        }

        GameObject newHead = Instantiate(headPrefab, spawnPos, spawnRot);
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
            RegisterTrain(trainScript);
            trainScript.TrainInit(headData);
        }

        if (_spawnCarriageIds != null && _spawnCarriageIds.Count > 0)
        {
            for (int i = 0; i < carriageCount; i++)
            {
                string targetId = _spawnCarriageIds[i % _spawnCarriageIds.Count];

                TrainData carData = DataManager.Instance?.GetData<TrainData>(targetId);
                if (carData == null || string.IsNullOrEmpty(carData.PrefabPath))
                {
                    Debug.LogWarning($"[TrainManager] {targetId} 데이터 또는 PrefabPath가 유효하지 않습니다.");
                    continue;
                }

                GameObject carPrefab = await GameManager.Resource.LoadAsset<GameObject>(carData.PrefabPath);
                if (carPrefab != null)
                {
                    SpawnCarriage(carPrefab, carData);
                }
            }
        }

        OnTrainSpawn?.Invoke(_headTrain);
        Debug.Log($"[TrainManager] 기관차 1대와 객차 {carriageCount}대 전체 소환 완료!");
    }



    public float GetHeadTrainDistance()
    {
        if (_headTrain == null) return 0f;

        Train head = _headTrain.GetComponent<Train>();
        return head != null ? head.TotalDistance : 0f;
    }

    private void RelocateExistingTrain(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (_headTrain == null) return;

        _headTrain.position = spawnPos;
        _headTrain.rotation = spawnRot;

        Train head = _headTrain.GetComponent<Train>();
        if (head != null)
        {
            RegisterTrain(head);
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
                    follow.SetTargetIndex(0);
                }

                front = carList[i].transform;
            }
        }

        SetCarriagesActive(true);
        IsStation = false;

        OnTrainRelocated?.Invoke();
        Debug.Log($"[TrainManager] 기존 기차(체력 유지됨)를 새로운 출구 위치({spawnPos})로 재배치 완료!");
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
            trainContainer.ContainerInit();
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

    public bool IsVisitedStation(Transform stationTransform)
    {
        return stationTransform != null && visitedStation.Contains(stationTransform);
    }

    public void MarkStationVisited(Transform stationTransform)
    {
        if (stationTransform == null)
        {
            return;
        }

        visitedStation.Add(stationTransform);
    }

    public void ArriveStation(GameObject stationObj)
    {
        if (IsStation)
        {
            return;
        }
        if (_headTrain != null)
        {
            _lastEnterDirection = _headTrain.forward;
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

    public void RegisterTrain(Train train)
    {
        _activeTrain = train;
    }

    public bool IsTrainHpFull()
    {
        if (_activeTrain != null)
        {
            return _activeTrain.CurrentHp >= _activeTrain.MaxHp;
        }
        return true;
    }

    public void HealActiveTrain(int healAmount)
    {
        if (_activeTrain != null)
        {
            _activeTrain.Heal(healAmount);
        }
        else
        {
            Debug.LogWarning("[TrainManager] 회복할 활성 기관차가 없습니다.");
        }
    }

    public bool EquipWeaponTrain(GameObject weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("[TrainManager] 장착할 무기 프리팹이 유효하지 않습니다.");
            return false;
        }

        if (carList == null || carList.Count == 0)
        {
            Debug.LogWarning("[TrainManager] 연결된 객차가 없습니다.");
            return false;
        }

        for (int i = 0; i < carList.Count; i++)
        {
            if (carList[i] == null)
            {
                continue;
            }

            TrainFollow follow = carList[i].GetComponent<TrainFollow>();

            //if (follow != null && !follow.HasWeapon && follow.WeaponMountPoint != null)
            //{
            //    bool success = follow.MountWeapon(weaponPrefab);
            //    if (success)
            //    {
            //        Debug.Log($"[TrainManager] {i}번 객차에 무기({weaponPrefab.name}) 장착 성공!");
            //        return true;
            //    }
            //}
        }

        Debug.LogWarning("[TrainManager] 무기를 장착할 수 있는 빈 객차가 없습니다.");
        return false;
    }

    public void SetTrainSpeedBoost(bool active)
    {
        if (_activeTrain != null)
        {
            _activeTrain.SetSpeedBoost(active);
        }
    }

}
