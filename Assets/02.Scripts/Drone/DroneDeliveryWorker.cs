using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Drone))]
public class DroneDeliveryWorker : MonoBehaviour, IDroneWorker
{
    private enum Phase
    {
        Idle,
        ToTarget,
        Waiting,
        Returning,
    }

    private struct Cargo
    {
        public GameObject Payload;
        public Transform Parent;
        public GameObject Ghost;
        public Vector3 Target;
        public Quaternion Rotation;
        public int Sequence;
        public Action<GameObject> OnPlaced;
    }

    [Header("참조")]
    [SerializeField] private Transform _dock;
    [SerializeField] private Transform _carrySocket;

    [Header("적재")]
    [SerializeField, Min(1)] private int _capacity = 3;
    [SerializeField, Min(0f)] private float _stackGap = 0.4f;
    [Tooltip("드론 몸체와 첫 레일 사이 간격. 레일이 드론에 파묻히면 이 값을 키운다")]
    [SerializeField, Min(0f)] private float _carryGap = 0.35f;

    [Header("복귀")]
    [Tooltip("복귀 중 도크와 이 거리 안이면 즉시 도킹한다. 움직이는 기차를 감속 구간에서 영영 못 따라잡는 것 방지")]
    [SerializeField, Min(0.1f)] private float _dockCatchDistance = 0.8f;

    public Transform Transform { get { return transform; } }

    public bool CanAcceptWork
    {
        get
        {
            if (_cargo.Count >= _capacity)
            {
                return false;
            }

            if (_phase == Phase.Idle)
            {
                return true;
            }

            return _preloadRails.Count > 0;
        }
    }

    public bool IsPreloaded { get { return _preloadRails.Count > 0; } }

    public int PreloadCount { get { return _preloadRails.Count; } }

    public bool CanPreload
    {
        get
        {
            if (_phase != Phase.Idle)
            {
                return false;
            }

            return _cargo.Count + _preloadRails.Count < _capacity;
        }
    }

    public DroneState State
    {
        get
        {
            if (_phase == Phase.Idle)
            {
                return DroneState.Docked;
            }

            return DroneState.Moving;
        }
    }

    private Drone _drone;
    private IAgentMover _mover;
    private DroneDockPoint _dockPoint;

    private Phase _phase = Phase.Idle;

    private readonly List<Cargo> _cargo = new List<Cargo>();

    private readonly List<GameObject> _preloadRails = new List<GameObject>();

    private void Awake()
    {
        _drone = GetComponent<Drone>();
        _mover = GetComponent<IAgentMover>();

        if (_dock != null)
        {
            _dockPoint = _dock.GetComponent<DroneDockPoint>();
        }
    }

    private void OnEnable()
    {
        _drone.OnArrived += HandleArrived;

        if (_dockPoint != null)
        {
            _dockPoint.OnAttached += HandleDockAttached;
        }

        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {
        AbortDelivery();
        ClearPreloads();

        _drone.OnArrived -= HandleArrived;

        if (_dockPoint != null)
        {
            _dockPoint.OnAttached -= HandleDockAttached;
        }

        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.Unregister(this);
        }
    }

    private void Start()
    {
        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.Register(this);
        }

        SnapToDock();
    }

    public bool HasPayload(GameObject payload)
    {
        return FindCargoIndex(payload) >= 0;
    }

    public bool TryReplacePayload(GameObject oldPayload, GameObject newPayload, GameObject newGhost)
    {
        if (oldPayload == null || newPayload == null)
        {
            return false;
        }

        int index = FindCargoIndex(oldPayload);

        if (index < 0)
        {
            return false;
        }

        Cargo cargo = _cargo[index];

        DestroyGhost(cargo.Ghost);

        cargo.Payload = newPayload;
        cargo.Parent = newPayload.transform.parent;
        cargo.Ghost = newGhost;
        cargo.Target = newPayload.transform.position;
        cargo.Rotation = newPayload.transform.rotation;

        _cargo[index] = cargo;

        HoldPayload(newPayload);
        RefreshStackLayout();

        return true;
    }

    public bool Assign(GameObject payload, GameObject ghost, Vector3 target, Quaternion rotation, int sequence, Action<GameObject> onPlaced = null)
    {
        if (payload == null || CanAcceptWork == false)
        {
            return false;
        }

        if (_dock == null)
        {
            Debug.LogWarning($"[DroneDeliveryWorker] {name}의 _dock이 비어 있어 배달을 받지 않습니다. 프리팹 참조를 확인하세요.", this);

            return false;
        }

        Cargo cargo = new Cargo
        {
            Payload = payload,
            Parent = payload.transform.parent,
            Ghost = ghost,
            Target = target,
            Rotation = rotation,
            Sequence = sequence,
            OnPlaced = onPlaced,
        };

        _cargo.Add(cargo);

        HoldPayload(payload);
        ConsumeOnePreload();
        RefreshStackLayout();

        if (_phase == Phase.Idle)
        {
            Depart();
        }
        else if (_phase == Phase.Returning)
        {
            _phase = Phase.ToTarget;

            _drone.MoveTo(_cargo[0].Target);
        }

        return true;
    }

    public bool TryPreload(GameObject rail)
    {
        if (rail == null || CanPreload == false)
        {
            return false;
        }

        rail.transform.SetParent(GetSocket(), true);

        _preloadRails.Add(rail);

        RefreshStackLayout();

        return true;
    }

    public GameObject TakePreload()
    {
        if (_preloadRails.Count == 0)
        {
            return null;
        }

        int last = _preloadRails.Count - 1;
        GameObject rail = _preloadRails[last];

        _preloadRails.RemoveAt(last);

        if (rail != null)
        {
            rail.transform.SetParent(null, true);
        }

        return rail;
    }

    private void ConsumeOnePreload()
    {
        if (_preloadRails.Count == 0)
        {
            return;
        }

        int last = _preloadRails.Count - 1;
        GameObject rail = _preloadRails[last];

        _preloadRails.RemoveAt(last);

        if (rail != null)
        {
            Destroy(rail);
        }
    }

    private void ClearPreloads()
    {
        for (int i = 0; i < _preloadRails.Count; i++)
        {
            if (_preloadRails[i] != null)
            {
                Destroy(_preloadRails[i]);
            }
        }

        _preloadRails.Clear();
    }

    public void Recall()
    {
        if (_phase == Phase.Idle)
        {
            return;
        }

        if (_cargo.Count > 0)
        {
            return;
        }

        BeginReturn();
    }

    public bool TryGetWorkTopY(out float topY)
    {
        topY = 0f;

        if (_phase == Phase.ToTarget || _phase == Phase.Waiting)
        {
            if (_cargo.Count == 0)
            {
                return TryGetDockY(out topY);
            }

            topY = _cargo[0].Target.y;

            return true;
        }

        if (TryGetDockY(out float dockY) == false)
        {
            return false;
        }

        topY = dockY + GetStackHeight();

        return true;
    }

    private bool TryGetDockY(out float dockY)
    {
        dockY = 0f;

        if (_dock == null)
        {
            return false;
        }

        if (_dockPoint != null && _dockPoint.IsAttached == false)
        {
            return false;
        }

        dockY = _dock.position.y;

        return true;
    }

    private float GetStackHeight()
    {
        int itemCount = _cargo.Count + _preloadRails.Count;

        if (itemCount == 0)
        {
            return 0f;
        }

        return _carryGap + (itemCount - 1) * _stackGap;
    }

    private void Update()
    {
        if (_phase == Phase.Waiting)
        {
            if (IsPlacementTurn())
            {
                PlaceOrWait();
            }

            return;
        }

        if (_phase != Phase.Returning)
        {
            return;
        }

        if (TryDockNearby())
        {
            return;
        }

        TrackDock();
    }

    private bool TryDockNearby()
    {
        if (_dock == null)
        {
            return false;
        }

        Vector3 offset = _dock.position - transform.position;

        offset.y = 0f;

        if (offset.sqrMagnitude > _dockCatchDistance * _dockCatchDistance)
        {
            return false;
        }

        _drone.Stop();

        _phase = Phase.Idle;

        return true;
    }

    private void LateUpdate()
    {
        if (_phase != Phase.Idle)
        {
            return;
        }

        HoldAtDock();
    }

    private void HoldAtDock()
    {
        if (_mover == null || _dock == null)
        {
            return;
        }

        if (_dockPoint != null && _dockPoint.IsAttached == false)
        {
            return;
        }

        Vector3 world = _dock.position;

        world.y = transform.position.y;

        _mover.Warp(world);
    }

    private void HandleDockAttached()
    {
        if (_phase != Phase.Idle)
        {
            return;
        }

        if (_mover == null || _dock == null)
        {
            return;
        }

        _mover.Warp(_dock.position);
    }

    private void Depart()
    {
        if (_phase != Phase.Idle)
        {
            return;
        }

        if (_cargo.Count == 0)
        {
            return;
        }

        _phase = Phase.ToTarget;

        _drone.MoveTo(_cargo[0].Target);
    }

    private void HandleArrived()
    {
        if (_phase == Phase.ToTarget)
        {
            PlaceOrWait();

            return;
        }

        if (_phase == Phase.Returning)
        {
            _phase = Phase.Idle;
        }
    }

    private void PlaceOrWait()
    {
        if (IsPlacementTurn() == false)
        {
            _phase = Phase.Waiting;

            return;
        }

        _phase = Phase.ToTarget;

        DropFront();

        if (_cargo.Count > 0)
        {
            _drone.MoveTo(_cargo[0].Target);

            return;
        }

        BeginReturn();
    }

    private bool IsPlacementTurn()
    {
        if (_cargo.Count == 0)
        {
            return true;
        }

        if (DroneManager.Instance == null)
        {
            return true;
        }

        return DroneManager.Instance.IsPlacementTurn(_cargo[0].Sequence);
    }

    private void DropFront()
    {
        if (_cargo.Count == 0)
        {
            return;
        }

        Cargo cargo = _cargo[0];

        _cargo.RemoveAt(0);
        RefreshStackLayout();

        if (cargo.Payload == null)
        {
            DestroyGhost(cargo.Ghost);

            cargo.OnPlaced?.Invoke(cargo.Payload);

            return;
        }

        cargo.Payload.transform.SetParent(cargo.Parent, true);

        DroneRailDrop.Begin(cargo.Payload, cargo.Target, cargo.Rotation, cargo.Ghost, placed => FinishDrop(cargo, placed));
    }

    private void FinishDrop(Cargo cargo, GameObject placed)
    {
        DroneManager.SetPayloadCollision(placed, true);

        cargo.OnPlaced?.Invoke(placed);
    }

    private int FindCargoIndex(GameObject payload)
    {
        if (payload == null)
        {
            return -1;
        }

        for (int i = 0; i < _cargo.Count; i++)
        {
            if (_cargo[i].Payload == payload)
            {
                return i;
            }
        }

        return -1;
    }

    private Transform GetSocket()
    {
        if (_carrySocket != null)
        {
            return _carrySocket;
        }

        return transform;
    }

    private void HoldPayload(GameObject payload)
    {
        if (payload == null)
        {
            return;
        }

        payload.transform.SetParent(GetSocket(), true);
        payload.SetActive(true);

        DroneManager.SetPayloadCollision(payload, false);
    }

    private void RefreshStackLayout()
    {
        Transform socket = GetSocket();
        int slot = 0;

        for (int i = 0; i < _cargo.Count; i++)
        {
            GameObject payload = _cargo[i].Payload;

            if (payload == null)
            {
                continue;
            }

            PlaceInStack(payload, socket, slot);

            slot++;
        }

        for (int i = 0; i < _preloadRails.Count; i++)
        {
            GameObject rail = _preloadRails[i];

            if (rail == null)
            {
                continue;
            }

            PlaceInStack(rail, socket, slot);

            slot++;
        }
    }

    private void PlaceInStack(GameObject item, Transform socket, int slot)
    {
        Vector3 position = socket.position - Vector3.up * (_carryGap + slot * _stackGap);

        item.transform.SetPositionAndRotation(position, socket.rotation);
    }

    private void PutDown(int index)
    {
        Cargo cargo = _cargo[index];

        DestroyGhost(cargo.Ghost);

        if (cargo.Payload != null)
        {
            cargo.Payload.transform.SetParent(cargo.Parent, true);
            cargo.Payload.transform.SetPositionAndRotation(cargo.Target, cargo.Rotation);
            cargo.Payload.SetActive(true);

            DroneManager.SetPayloadCollision(cargo.Payload, true);
        }

        cargo.OnPlaced?.Invoke(cargo.Payload);
    }

    private void AbortDelivery()
    {
        for (int i = 0; i < _cargo.Count; i++)
        {
            PutDown(i);
        }

        _cargo.Clear();

        _phase = Phase.Idle;
    }

    private void DestroyGhost(GameObject ghost)
    {
        if (ghost == null)
        {
            return;
        }

        Destroy(ghost);
    }

    private void BeginReturn()
    {
        _phase = Phase.Returning;

        TrackDock();
    }

    private void TrackDock()
    {
        if (_dock == null)
        {
            return;
        }

        _drone.MoveTo(_dock.position);
    }

    private void SnapToDock()
    {
        if (_mover == null || _dock == null)
        {
            return;
        }

        Vector3 world = _dock.position;

        world.y = transform.position.y;

        _mover.Warp(world);
    }
}
