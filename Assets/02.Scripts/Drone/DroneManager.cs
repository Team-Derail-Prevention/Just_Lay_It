using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Enums;
using UnityEngine;

public class DroneManager : SingletonBase<DroneManager>
{
    [Serializable]
    private class DroneSpawnEntry
    {
        public string Address;
        [Min(0)] public int Count = 1;
    }

    private struct DeliveryOrder
    {
        public GameObject Payload;
        public GameObject Ghost;
        public Vector3 Target;
        public Quaternion Rotation;
        public int Sequence;
        public Action<GameObject> OnPlaced;
    }

    private struct HeldPlacement
    {
        public GameObject Payload;
        public Action<GameObject> OnPlaced;
    }

    [Header("스폰")]
    [SerializeField] private List<DroneSpawnEntry> _spawnEntries = new List<DroneSpawnEntry>();
    [SerializeField] private Transform _spawnRoot;
    [SerializeField, Min(0f)] private float _altitudeStepPerSlot = 0.6f;

    [Header("채집 명령")]
    [SerializeField, Min(1)] private int _maxMiningOrdersPerMiner = 3;

    [Header("채집 표시")]
    [SerializeField] private Material _orderMarkerMaterial;
    [SerializeField] private Color _miningColor = new Color(1f, 0.85f, 0.2f, 0.8f);
    [SerializeField] private Color _reservedColor = new Color(0.3f, 1f, 0.45f, 0.8f);

    [SerializeField, Range(0.05f, 1f)] private float _lastOrderAlphaScale = 0.35f;
    [SerializeField] private float _markerHeightOffset = 0.05f;
    [SerializeField, Min(0.1f)] private float _markerSize = 2f;

    [Header("사전 적재")]
    [SerializeField] private string _preloadRailAddress = "Prefab/Rail_Straight";

    private readonly List<IDroneWorker> _workers = new List<IDroneWorker>();
    private readonly List<GameObject> _spawned = new List<GameObject>();
    private readonly Queue<DeliveryOrder> _pendingDeliveries = new Queue<DeliveryOrder>();
    private readonly List<MaterialObject> _pendingMining = new List<MaterialObject>();
    private readonly Dictionary<int, HeldPlacement> _heldPlacements = new Dictionary<int, HeldPlacement>();

    private int _nextPlacementSequence;
    private int _placementTurn;

    private float _gatherSpeedMultiplier = 1f;
    private float _moveSpeedMultiplier = 1f;
    private int _yieldBonus;
    private bool _isUpgradeSubscribed;
    private bool _isMiningSuspendedUntilDeparture;

    private DroneRailPreloader _preloader;
    private DroneOrderMarkerView _markerView;

    private DroneRailPreloader Preloader
    {
        get
        {
            if (_preloader == null)
            {
                _preloader = new DroneRailPreloader(_workers, _preloadRailAddress);
            }

            return _preloader;
        }
    }

    private DroneOrderMarkerView MarkerView
    {
        get
        {
            if (_markerView == null)
            {
                _markerView = new DroneOrderMarkerView(transform, _orderMarkerMaterial, _miningColor, _reservedColor, _lastOrderAlphaScale, _markerHeightOffset, _markerSize, MaxMiningOrders);
            }

            return _markerView;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState gameState)
    {
        if (gameState == GameState.Playing)
        {
            _isMiningSuspendedUntilDeparture = false;
        }
    }

    private void Update()
    {
        DispatchPendingDeliveries();
        DispatchPendingMining();
        Preloader.Tick();
        MarkerView.Refresh(_workers, _pendingMining, MaxMiningOrders);
    }

    public async UniTask SpawnAllAsync()
    {
        if (_spawnEntries.Count == 0)
        {
            return;
        }

        if (ResourceManager.Instance == null)
        {
            Debug.LogError("[DroneManager] ResourceManager가 없어 드론을 스폰하지 못했습니다.");

            return;
        }

        DespawnAll();

        Transform root;

        if (_spawnRoot != null)
        {
            root = _spawnRoot;
        }
        else
        {
            root = transform;
        }

        for (int i = 0; i < _spawnEntries.Count; i++)
        {
            DroneSpawnEntry entry = _spawnEntries[i];

            if (entry == null || string.IsNullOrEmpty(entry.Address) || entry.Count <= 0)
            {
                continue;
            }

            GameObject prefab = await ResourceManager.Instance.LoadAsset<GameObject>(entry.Address);

            if (prefab == null)
            {
                Debug.LogError($"[DroneManager] 드론 프리팹 로드 실패: {entry.Address}");

                continue;
            }

            for (int n = 0; n < entry.Count; n++)
            {
                GameObject drone = Instantiate(prefab, root);

                ApplyDroneSlot(drone, n, entry.Count);

                _spawned.Add(drone);
            }
        }

        Debug.Log($"[DroneManager] 드론 {_spawned.Count}대 스폰 완료");
    }

    private void ApplyDroneSlot(GameObject drone, int slot, int slotCount)
    {
        ApplyCruiseOffset(drone, slot);

        DroneDockPoint dock = drone.GetComponentInChildren<DroneDockPoint>(true);

        if (dock == null)
        {
            return;
        }

        if (drone.GetComponentInChildren<DroneStateMachine>(true) != null)
        {
            dock.SetOrbitSlot(slot, slotCount);

            return;
        }

        if (slot <= 0)
        {
            return;
        }

        dock.SetCar(dock.CarIndex + slot);
    }

    private void ApplyCruiseOffset(GameObject drone, int slot)
    {
        if (_altitudeStepPerSlot <= 0f)
        {
            return;
        }

        DroneAltitude altitude = drone.GetComponentInChildren<DroneAltitude>(true);

        if (altitude == null)
        {
            return;
        }

        altitude.AddCruiseOffset(slot * _altitudeStepPerSlot);
    }

    public void DespawnAll()
    {
        while (_pendingDeliveries.Count > 0)
        {
            DeliveryOrder order = _pendingDeliveries.Dequeue();

            RestorePayload(order.Payload, order.Ghost, order.Target, order.Rotation, order.OnPlaced);
        }

        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] == null)
            {
                continue;
            }

            Destroy(_spawned[i]);
        }

        _spawned.Clear();
    }

    public float GatherSpeedMultiplier { get { return _gatherSpeedMultiplier; } }
    public float MoveSpeedMultiplier { get { return _moveSpeedMultiplier; } }

    // 채집량 파이프라인이 생기면 캘 때마다 이 값을 더해 주세요. 지금은 읽는 곳이 없습니다.
    public int YieldBonus { get { return _yieldBonus; } }

    private void OnEnable()
    {
        TrySubscribeUpgrade();
    }

    private void Start()
    {
        TrySubscribeUpgrade();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        if (_isUpgradeSubscribed == false)
        {
            return;
        }

        _isUpgradeSubscribed = false;

        if (UpgradeEventHub.Instance == null)
        {
            return;
        }

        UpgradeEventHub.Instance.OnInGameUpgraded -= HandleInGameUpgraded;
    }

    private void TrySubscribeUpgrade()
    {
        if (_isUpgradeSubscribed)
        {
            return;
        }

        if (UpgradeEventHub.Instance == null)
        {
            return;
        }

        UpgradeEventHub.Instance.OnInGameUpgraded += HandleInGameUpgraded;

        _isUpgradeSubscribed = true;
    }

    private void HandleInGameUpgraded(string slotDataId, int newLevel)
    {
        if (slotDataId == DroneUpgradeIdConst.GatherSpeed)
        {
            _gatherSpeedMultiplier = BuildMultiplier(slotDataId, newLevel);

            return;
        }

        if (slotDataId == DroneUpgradeIdConst.MoveSpeed)
        {
            _moveSpeedMultiplier = BuildMultiplier(slotDataId, newLevel);

            return;
        }

        if (slotDataId == DroneUpgradeIdConst.GatherEfficiency)
        {
            _yieldBonus = BuildFlatBonus(slotDataId, newLevel);
        }
    }

    private float BuildMultiplier(string slotDataId, int newLevel)
    {
        if (TryGetAppliedValue(slotDataId, newLevel, out float total) == false)
        {
            return 1f;
        }

        return 1f + total;
    }

    private int BuildFlatBonus(string slotDataId, int newLevel)
    {
        if (TryGetAppliedValue(slotDataId, newLevel, out float total) == false)
        {
            return 0;
        }

        return Mathf.RoundToInt(total);
    }

    private bool TryGetAppliedValue(string slotDataId, int newLevel, out float total)
    {
        total = 0f;

        if (newLevel <= 0)
        {
            return true;
        }

        if (DataManager.Instance == null || DataManager.Instance.IsLoaded == false)
        {
            return false;
        }

        DroneUpgradeData data = DataManager.Instance.GetData<DroneUpgradeData>(slotDataId);

        if (data == null)
        {
            return false;
        }

        int applied = Mathf.Clamp(newLevel, 0, data.MaxLevel);

        total = data.Value * applied;

        return true;
    }

    public void Register(IDroneWorker worker)
    {
        if (worker == null)
        {
            return;
        }

        if (_workers.Contains(worker))
        {
            return;
        }

        _workers.Add(worker);
    }

    public void Unregister(IDroneWorker worker)
    {
        if (worker == null)
        {
            return;
        }

        _workers.Remove(worker);
    }

    public int PendingMiningCount { get { return _pendingMining.Count; } }

    public bool IsSuspendedUntilDeparture { get { return _isMiningSuspendedUntilDeparture; } }

    public bool TryAssignMining(MaterialObject target)
    {
        if (_isMiningSuspendedUntilDeparture)
        {
            return false;
        }

        if (target == null || target.IsBroken || target.IsMining)
        {
            return false;
        }

        if (_pendingMining.Contains(target))
        {
            return false;
        }

        if (IsTargetClaimed(target))
        {
            return false;
        }

        DroneStateMachine miner = FindNearestIdleMiner(target.transform.position, target);

        if (miner != null && miner.Assign(target))
        {
            return true;
        }

        if (_pendingMining.Count >= MaxMiningOrders)
        {
            Debug.Log($"[DroneManager] 채집 예약이 가득 찼습니다. 최대 {MaxMiningOrders}개");

            return false;
        }

        _pendingMining.Add(target);

        Debug.Log($"[DroneManager] 채집 예약 {_pendingMining.Count}번째로 넣었습니다: {target.MaterialId}");

        return true;
    }

    private void DispatchPendingMining()
    {
        while (_pendingMining.Count > 0)
        {
            MaterialObject target = _pendingMining[0];

            if (target == null || target.IsBroken || target.IsMining || IsTargetClaimed(target))
            {
                _pendingMining.RemoveAt(0);

                continue;
            }

            DroneStateMachine miner = FindNearestIdleMiner(target.transform.position, target);

            if (miner == null)
            {
                return;
            }

            _pendingMining.RemoveAt(0);

            miner.Assign(target);
        }
    }

    public bool CanAssignMining(MaterialObject target)
    {
        if (_isMiningSuspendedUntilDeparture)
        {
            return false;
        }

        if (target == null || target.IsBroken || target.IsMining)
        {
            return false;
        }

        if (_pendingMining.Contains(target))
        {
            return false;
        }

        if (IsTargetClaimed(target))
        {
            return false;
        }

        if (FindNearestIdleMiner(target.transform.position, target) != null)
        {
            return true;
        }

        return _pendingMining.Count < MaxMiningOrders;
    }

    public static void Deliver(GameObject payload, Vector3 target, Quaternion rotation, Action<GameObject> onPlaced = null)
    {
        if (Instance != null)
        {
            Instance.RequestDelivery(payload, target, rotation, onPlaced);

            return;
        }

        onPlaced?.Invoke(payload);
    }

    public static bool TryReplaceDelivery(GameObject oldPayload, GameObject newPayload)
    {
        if (Instance == null)
        {
            return false;
        }

        return Instance.ReplaceDelivery(oldPayload, newPayload);
    }

    private bool ReplaceDelivery(GameObject oldPayload, GameObject newPayload)
    {
        if (oldPayload == null || newPayload == null)
        {
            return false;
        }

        if (DroneRailDrop.TryHandOff(oldPayload, newPayload))
        {
            Debug.Log($"[순번] 낙하 중이던 레일이 모양 갱신으로 교체돼 순번을 넘겨받았습니다: {newPayload.name}");

            return true;
        }

        for (int i = 0; i < _workers.Count; i++)
        {
            DroneDeliveryWorker carrier = _workers[i] as DroneDeliveryWorker;

            if (carrier == null || carrier.HasPayload(oldPayload) == false)
            {
                continue;
            }

            GameObject newGhost = ReserveForDelivery(newPayload);

            if (carrier.TryReplacePayload(oldPayload, newPayload, newGhost))
            {
                return true;
            }

            Destroy(newGhost);

            return false;
        }

        return ReplacePendingDelivery(oldPayload, newPayload);
    }

    private bool ReplacePendingDelivery(GameObject oldPayload, GameObject newPayload)
    {
        int count = _pendingDeliveries.Count;
        bool isReplaced = false;

        for (int i = 0; i < count; i++)
        {
            DeliveryOrder order = _pendingDeliveries.Dequeue();

            if (order.Payload == oldPayload)
            {
                if (order.Ghost != null)
                {
                    Destroy(order.Ghost);
                }

                order.Target = newPayload.transform.position;
                order.Rotation = newPayload.transform.rotation;
                order.Payload = newPayload;
                order.Ghost = ReserveForDelivery(newPayload);

                isReplaced = true;
            }

            _pendingDeliveries.Enqueue(order);
        }

        return isReplaced;
    }

    private GameObject ReserveForDelivery(GameObject payload)
    {
        GameObject ghost = CreateReservedGhost(payload, payload.transform.position, payload.transform.rotation);

        payload.SetActive(false);

        return ghost;
    }

    public bool RequestDelivery(GameObject payload, Vector3 target, Quaternion rotation, Action<GameObject> onPlaced = null)
    {
        if (payload == null)
        {
            Debug.Log("[DroneManager] 배달 요청 거절 — 옮길 물건이 없습니다");

            return false;
        }

        int sequence = _nextPlacementSequence;

        _nextPlacementSequence++;

        Action<GameObject> sequenced = placed => CompletePlacement(sequence, placed, onPlaced);

        DroneDeliveryWorker carrier = null;

        if (_pendingDeliveries.Count == 0)
        {
            carrier = FindNearestIdleCarrier(target);
        }

        if (carrier == null && HasAnyCarrier() == false)
        {
            Debug.Log("[DroneManager] 배달 요청 거절 — 운반 드론이 한 대도 없습니다");

            sequenced(payload);

            return false;
        }

        GameObject ghost = CreateReservedGhost(payload, target, rotation);

        payload.SetActive(false);

        if (carrier != null)
        {
            Preloader.TopUp(carrier);

            if (carrier.Assign(payload, ghost, target, rotation, sequence, sequenced))
            {
                return true;
            }

            RestorePayload(payload, ghost, target, rotation, sequenced);

            return false;
        }

        DeliveryOrder order = new DeliveryOrder
        {
            Payload = payload,
            Ghost = ghost,
            Target = target,
            Rotation = rotation,
            Sequence = sequence,
            OnPlaced = sequenced,
        };

        _pendingDeliveries.Enqueue(order);

        Debug.Log($"[DroneManager] 배달 대기열에 넣었습니다. 대기 {_pendingDeliveries.Count}건");

        return true;
    }

    private void DispatchPendingDeliveries()
    {
        while (_pendingDeliveries.Count > 0)
        {
            DeliveryOrder order = _pendingDeliveries.Peek();

            if (order.Payload == null)
            {
                _pendingDeliveries.Dequeue();

                RestorePayload(order.Payload, order.Ghost, order.Target, order.Rotation, order.OnPlaced);

                continue;
            }

            DroneDeliveryWorker carrier = FindNearestIdleCarrier(order.Target);

            if (carrier == null)
            {
                return;
            }

            _pendingDeliveries.Dequeue();

            Preloader.TopUp(carrier);

            if (carrier.Assign(order.Payload, order.Ghost, order.Target, order.Rotation, order.Sequence, order.OnPlaced) == false)
            {
                Debug.LogWarning("[DroneManager] 대기 중이던 배달을 배정하지 못했습니다. 즉시 설치로 남깁니다.");

                RestorePayload(order.Payload, order.Ghost, order.Target, order.Rotation, order.OnPlaced);
            }
        }
    }

    public bool IsPlacementTurn(int sequence)
    {
        return sequence <= _placementTurn;
    }

    private void CompletePlacement(int sequence, GameObject placed, Action<GameObject> onPlaced)
    {
        if (sequence < _placementTurn)
        {
            return;
        }

        if (sequence > _placementTurn)
        {
            _heldPlacements[sequence] = new HeldPlacement { Payload = placed, OnPlaced = onPlaced };

            return;
        }

        _placementTurn++;

        onPlaced?.Invoke(placed);

        DrainHeldPlacements();
    }

    private void DrainHeldPlacements()
    {
        while (_heldPlacements.TryGetValue(_placementTurn, out HeldPlacement held))
        {
            _heldPlacements.Remove(_placementTurn);

            _placementTurn++;

            held.OnPlaced?.Invoke(held.Payload);
        }
    }

    private GameObject CreateReservedGhost(GameObject payload, Vector3 target, Quaternion rotation)
    {
        GameObject ghost = Instantiate(payload, target, rotation);

        ghost.name = payload.name + "_Reserved";

        SetPayloadCollision(ghost, false);
        SetPayloadGhost(ghost, true);

        return ghost;
    }

    private void RestorePayload(GameObject payload, GameObject ghost, Vector3 target, Quaternion rotation, Action<GameObject> onPlaced)
    {
        if (ghost != null)
        {
            Destroy(ghost);
        }

        if (payload != null)
        {
            payload.transform.SetPositionAndRotation(target, rotation);

            SetPayloadGhost(payload, false);
            SetPayloadCollision(payload, true);

            payload.SetActive(true);
        }

        onPlaced?.Invoke(payload);
    }

    public static void SetPayloadGhost(GameObject payload, bool isGhost)
    {
        if (payload == null)
        {
            return;
        }

        RailPreviewController preview = payload.GetComponent<RailPreviewController>();

        if (preview == null)
        {
            return;
        }

        if (isGhost)
        {
            preview.SetGhost();
        }
        else
        {
            preview.SetSolid();
        }
    }

    public static void SetPayloadCollision(GameObject payload, bool isEnabled)
    {
        if (payload == null)
        {
            return;
        }

        Collider[] colliders = payload.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = isEnabled;
        }
    }

    private bool HasAnyCarrier()
    {
        for (int i = 0; i < _workers.Count; i++)
        {
            if (_workers[i] is DroneDeliveryWorker)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasIdleCarrier()
    {
        return FindNearestIdleCarrier(Vector3.zero) != null;
    }

    public void RecallAllAndSuspendMining()
    {
        _isMiningSuspendedUntilDeparture = true;
        RecallAll();
    }

    public void RecallAll()
    {
        _pendingMining.Clear();

        for (int i = 0; i < _workers.Count; i++)
        {
            if (_workers[i] == null)
            {
                continue;
            }

            _workers[i].Recall();
        }
    }

    private int MaxMiningOrders
    {
        get
        {
            int minerCount = CountMiners();

            if (minerCount < 1)
            {
                minerCount = 1;
            }

            return _maxMiningOrdersPerMiner * minerCount;
        }
    }

    private int CountMiners()
    {
        int count = 0;

        for (int i = 0; i < _workers.Count; i++)
        {
            if (_workers[i] is DroneStateMachine)
            {
                count++;
            }
        }

        return count;
    }

    private bool IsTargetClaimed(MaterialObject target)
    {
        for (int i = 0; i < _workers.Count; i++)
        {
            DroneStateMachine miner = _workers[i] as DroneStateMachine;

            if (miner == null)
            {
                continue;
            }

            if (miner.CurrentTarget == target)
            {
                return true;
            }
        }

        return false;
    }

    private DroneStateMachine FindNearestIdleMiner(Vector3 near, MaterialObject target)
    {
        DroneStateMachine best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < _workers.Count; i++)
        {
            DroneStateMachine miner = _workers[i] as DroneStateMachine;

            if (miner == null)
            {
                continue;
            }

            if (miner.CanAssign(target) == false)
            {
                continue;
            }

            float distance = (miner.Transform.position - near).sqrMagnitude;

            if (distance >= bestDistance)
            {
                continue;
            }

            best = miner;
            bestDistance = distance;
        }

        return best;
    }

    private DroneDeliveryWorker FindNearestIdleCarrier(Vector3 near)
    {
        DroneDeliveryWorker best = null;
        int bestPreload = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < _workers.Count; i++)
        {
            DroneDeliveryWorker carrier = _workers[i] as DroneDeliveryWorker;

            if (carrier == null)
            {
                continue;
            }

            if (carrier.CanAcceptWork == false)
            {
                continue;
            }

            if (carrier.PreloadCount < bestPreload)
            {
                continue;
            }

            float distance = (carrier.Transform.position - near).sqrMagnitude;

            if (carrier.PreloadCount == bestPreload && distance >= bestDistance)
            {
                continue;
            }

            best = carrier;
            bestPreload = carrier.PreloadCount;
            bestDistance = distance;
        }

        return best;
    }
}
