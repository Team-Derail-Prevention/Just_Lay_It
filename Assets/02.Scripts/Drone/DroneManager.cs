using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DroneManager : MonoBehaviour
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
    }

    public static DroneManager Instance { get; private set; }

    [Header("스폰")]
    [Tooltip("비워두면 아무것도 스폰하지 않는다. 씬에 직접 배치한 드론으로 동작")]
    [SerializeField] private List<DroneSpawnEntry> _spawnEntries = new List<DroneSpawnEntry>();
    [SerializeField] private Transform _spawnRoot;

    [Header("배달 예약 표시")]
    [SerializeField, Range(0f, 1f)] private float _reservedAlpha = 0.4f;

    private readonly List<IDroneWorker> _workers = new List<IDroneWorker>();
    private readonly List<GameObject> _spawned = new List<GameObject>();
    private readonly Queue<DeliveryOrder> _pendingDeliveries = new Queue<DeliveryOrder>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DroneManager] 이미 다른 DroneManager가 있어 이 컴포넌트를 제거합니다.");

            Destroy(this);

            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
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
                _spawned.Add(Instantiate(prefab, root));
            }
        }

        Debug.Log($"[DroneManager] 드론 {_spawned.Count}대 스폰 완료");
    }

    public void DespawnAll()
    {
        while (_pendingDeliveries.Count > 0)
        {
            DeliveryOrder order = _pendingDeliveries.Dequeue();

            RestorePayload(order.Payload, order.Ghost, order.Target, order.Rotation);
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

    public bool TryAssignMining(MaterialObject target)
    {
        if (target == null)
        {
            return false;
        }

        DroneStateMachine miner = FindNearestIdleMiner(target.transform.position, target);

        if (miner == null)
        {
            Debug.Log("[DroneManager] 명령을 받을 수 있는 채집 드론이 없습니다.");

            return false;
        }

        return miner.Assign(target);
    }

    public bool CanAssignMining(MaterialObject target)
    {
        if (target == null)
        {
            return false;
        }

        return FindNearestIdleMiner(target.transform.position, target) != null;
    }

    public bool RequestDelivery(GameObject payload, Vector3 target, Quaternion rotation)
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

            return false;
        }

        GameObject ghost = CreateReservedGhost(payload, target, rotation);

        payload.SetActive(false);

        if (carrier != null)
        {
            if (carrier.Assign(payload, ghost, target, rotation))
            {
                return true;
            }

            RestorePayload(payload, ghost, target, rotation);

            return false;
        }

        DeliveryOrder order = new DeliveryOrder
        {
            Payload = payload,
            Ghost = ghost,
            Target = target,
            Rotation = rotation,
        };

        _pendingDeliveries.Enqueue(order);

        Debug.Log($"[DroneManager] 배달 대기열에 넣었습니다. 대기 {_pendingDeliveries.Count}건");

        return true;
    }

    private GameObject CreateReservedGhost(GameObject payload, Vector3 target, Quaternion rotation)
    {
        GameObject ghost = Instantiate(payload, target, rotation);

        ghost.name = payload.name + "_Reserved";

        SetPayloadCollision(ghost, false);
        SetPayloadGhost(ghost, true);

        return ghost;
    }

    private void RestorePayload(GameObject payload, GameObject ghost, Vector3 target, Quaternion rotation)
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
    }

    public void SetPayloadGhost(GameObject payload, bool isGhost)
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
            preview.SetGhostAlpha(_reservedAlpha);
        }
        else
        {
            preview.SetGhostAlpha(1f);
        }
    }

    public void SetPayloadCollision(GameObject payload, bool isEnabled)
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

    private void Update()
    {
        DispatchPendingDeliveries();
    }

    private void DispatchPendingDeliveries()
    {
        while (_pendingDeliveries.Count > 0)
        {
            DeliveryOrder order = _pendingDeliveries.Peek();

            if (order.Payload == null)
            {
                _pendingDeliveries.Dequeue();

                continue;
            }

            DroneDeliveryWorker carrier = FindNearestIdleCarrier(order.Target);

            if (carrier == null)
            {
                return;
            }

            _pendingDeliveries.Dequeue();

            if (carrier.Assign(order.Payload, order.Ghost, order.Target, order.Rotation) == false)
            {
                Debug.LogWarning("[DroneManager] 대기 중이던 배달을 배정하지 못했습니다. 즉시 설치로 남깁니다.");

                RestorePayload(order.Payload, order.Ghost, order.Target, order.Rotation);
            }
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

            float distance = (carrier.Transform.position - near).sqrMagnitude;

            if (distance >= bestDistance)
            {
                continue;
            }

            best = carrier;
            bestDistance = distance;
        }

        return best;
    }
}
