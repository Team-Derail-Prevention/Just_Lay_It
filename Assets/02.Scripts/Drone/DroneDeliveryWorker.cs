using UnityEngine;

[RequireComponent(typeof(Drone))]
public class DroneDeliveryWorker : MonoBehaviour, IDroneWorker
{
    private enum Phase
    {
        Idle,
        ToRack,
        PickingUp,
        ToTarget,
        Dropping,
        Returning,
    }

    [Header("참조")]
    [SerializeField] private Transform _dock;

    // TODO: 정명진님 기차에 적재칸이 생기면 그 Transform으로 교체
    [SerializeField] private Transform _rack;
    [SerializeField] private Transform _carrySocket;

    [Header("작업")]
    [SerializeField, Min(0f)] private float _pickUpDuration = 0.4f;
    [SerializeField, Min(0f)] private float _dropDuration = 1f;

    public Transform Transform { get { return transform; } }
    public bool CanAcceptWork { get { return _hasOrder == false; } }

    public DroneState State
    {
        get
        {
            if (_phase == Phase.PickingUp || _phase == Phase.Dropping)
            {
                return DroneState.Working;
            }

            return _phase == Phase.Idle ? DroneState.Docked : DroneState.Moving;
        }
    }

    private Drone _drone;
    private IAgentMover _mover;
    private DroneDockPoint _dockPoint;

    private Phase _phase = Phase.Idle;

    private bool _hasOrder;
    private GameObject _payload;
    private GameObject _ghost;
    private Vector3 _target;
    private Quaternion _rotation;

    private float _workTimer;

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

    public bool Assign(GameObject payload, GameObject ghost, Vector3 target, Quaternion rotation)
    {
        if (payload == null || CanAcceptWork == false)
        {
            return false;
        }

        if (_rack == null && _dock == null)
        {
            Debug.LogWarning($"[DroneDeliveryWorker] {name}의 _rack과 _dock이 모두 비어 있어 배달을 받지 않습니다. 프리팹 참조를 확인하세요.", this);

            return false;
        }

        _payload = payload;
        _ghost = ghost;
        _target = target;
        _rotation = rotation;
        _hasOrder = true;

        _phase = Phase.ToRack;

        TrackPickUpPoint();

        return true;
    }

    public void Recall()
    {
        if (_phase == Phase.Idle || _hasOrder)
        {
            return;
        }

        BeginReturn();
    }

    public bool TryGetWorkTopY(out float topY)
    {
        topY = 0f;

        if (_phase == Phase.ToRack || _phase == Phase.PickingUp)
        {
            if (_rack == null)
            {
                return TryGetDockY(out topY);
            }

            topY = _rack.position.y;

            return true;
        }

        if (_phase == Phase.ToTarget || _phase == Phase.Dropping)
        {
            topY = _target.y;

            return true;
        }

        return TryGetDockY(out topY);
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

    private void Update()
    {
        if (_phase == Phase.PickingUp || _phase == Phase.Dropping)
        {
            UpdateWork();

            return;
        }

        if (_phase == Phase.ToRack)
        {
            TrackPickUpPoint();

            return;
        }

        if (_phase == Phase.Returning)
        {
            TrackDock();
        }
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
        if (_mover == null || _dock == null)
        {
            return;
        }

        _mover.Warp(_dock.position);
    }

    private void UpdateWork()
    {
        _workTimer -= Time.deltaTime;

        if (_workTimer > 0f)
        {
            return;
        }

        if (_phase == Phase.PickingUp)
        {
            PickUp();

            _phase = Phase.ToTarget;

            _drone.MoveTo(_target);

            return;
        }

        PutDown();

        _hasOrder = false;
        _payload = null;

        BeginReturn();
    }

    private void HandleArrived()
    {
        if (_phase == Phase.ToRack)
        {
            _workTimer = _pickUpDuration;
            _phase = Phase.PickingUp;

            return;
        }

        if (_phase == Phase.ToTarget)
        {
            _workTimer = _dropDuration;
            _phase = Phase.Dropping;

            return;
        }

        if (_phase == Phase.Returning)
        {
            _phase = Phase.Idle;
        }
    }

    private void PickUp()
    {
        if (_payload == null)
        {
            return;
        }

        Transform socket = _carrySocket != null ? _carrySocket : transform;

        _payload.transform.SetParent(socket, true);
        _payload.transform.SetPositionAndRotation(socket.position, socket.rotation);
        _payload.SetActive(true);
    }

    private void PutDown()
    {
        if (_payload == null)
        {
            DestroyGhost();

            return;
        }

        _payload.transform.SetParent(null, true);
        _payload.transform.SetPositionAndRotation(_target, _rotation);
        _payload.SetActive(true);

        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.SetPayloadCollision(_payload, true);
        }

        DestroyGhost();
    }

    private void DestroyGhost()
    {
        if (_ghost == null)
        {
            return;
        }

        Destroy(_ghost);

        _ghost = null;
    }

    private void BeginReturn()
    {
        _phase = Phase.Returning;

        TrackDock();
    }

    private void TrackPickUpPoint()
    {
        Transform point = _rack != null ? _rack : _dock;

        if (point == null)
        {
            return;
        }

        _drone.MoveTo(point.position);
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
