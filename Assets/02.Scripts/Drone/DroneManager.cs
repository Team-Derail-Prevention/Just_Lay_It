using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        public Action<GameObject> OnPlaced;
    }

    [Header("스폰")]
    [SerializeField] private List<DroneSpawnEntry> _spawnEntries = new List<DroneSpawnEntry>();
    [SerializeField] private Transform _spawnRoot;

    [Header("채집 명령")]
    [SerializeField, Min(1)] private int _maxMiningOrders = 3;

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
                _markerView = new DroneOrderMarkerView(transform, _orderMarkerMaterial, _miningColor, _reservedColor, _lastOrderAlphaScale, _markerHeightOffset, _markerSize, _maxMiningOrders);
            }

            return _markerView;
        }
    }

    private void Update()
    {
        DispatchPendingDeliveries();
        DispatchPendingMining();
        Preloader.Tick();
        MarkerView.Refresh(_workers, _pendingMining);
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

                ApplyCarSlot(drone, n);

                _spawned.Add(drone);
            }
        }

        Debug.Log($"[DroneManager] 드론 {_spawned.Count}대 스폰 완료");
    }

    private void ApplyCarSlot(GameObject drone, int slot)
    {
        if (slot <= 0)
        {
            return;
        }

        DroneDockPoint dock = drone.GetComponentInChildren<DroneDockPoint>(true);

        if (dock == null)
        {
            return;
        }

        dock.SetCar(dock.CarIndex + slot);
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

    public bool TryAssignMining(MaterialObject target)
    {
        if (target == null || target.IsBroken || target.IsMining)
        {
            return false;
        }

        if (_pendingMining.Contains(target))
        {
            return false;
        }

        DroneStateMachine miner = FindNearestIdleMiner(target.transform.position, target);

        if (miner != null && miner.Assign(target))
        {
            return true;
        }

        if (_pendingMining.Count >= _maxMiningOrders)
        {
            Debug.Log($"[DroneManager] 채집 예약이 가득 찼습니다. 최대 {_maxMiningOrders}개");

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

            if (target == null || target.IsBroken || target.IsMining)
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
        if (target == null || target.IsBroken || target.IsMining)
        {
            return false;
        }

        if (_pendingMining.Contains(target))
        {
            return false;
        }

        if (FindNearestIdleMiner(target.transform.position, target) != null)
        {
            return true;
        }

        return _pendingMining.Count < _maxMiningOrders;
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

        DroneDeliveryWorker carrier = FindNearestIdleCarrier(target);

        if (carrier == null && HasAnyCarrier() == false)
        {
            Debug.Log("[DroneManager] 배달 요청 거절 — 운반 드론이 한 대도 없습니다");

            onPlaced?.Invoke(payload);

            return false;
        }

        GameObject ghost = CreateReservedGhost(payload, target, rotation);

        payload.SetActive(false);

        if (carrier != null)
        {
            Preloader.TopUp(carrier);

            if (carrier.Assign(payload, ghost, target, rotation, onPlaced))
            {
                return true;
            }

            RestorePayload(payload, ghost, target, rotation, onPlaced);

            return false;
        }

        DeliveryOrder order = new DeliveryOrder
        {
            Payload = payload,
            Ghost = ghost,
            Target = target,
            Rotation = rotation,
            OnPlaced = onPlaced,
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

            if (carrier.Assign(order.Payload, order.Ghost, order.Target, order.Rotation, order.OnPlaced) == false)
            {
                Debug.LogWarning("[DroneManager] 대기 중이던 배달을 배정하지 못했습니다. 즉시 설치로 남깁니다.");

                RestorePayload(order.Payload, order.Ghost, order.Target, order.Rotation, order.OnPlaced);
            }
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

        if (payload == null)
        {
            return;
        }

        payload.transform.SetPositionAndRotation(target, rotation);

        SetPayloadGhost(payload, false);
        SetPayloadCollision(payload, true);

        payload.SetActive(true);

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
