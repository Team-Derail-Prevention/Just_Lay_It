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
    public bool CanAcceptWork { get { return _order == null; } }

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

    private Phase _phase = Phase.Idle;
    private DeliveryOrder _order;
    private float _workTimer;

    private void Awake()
    {
        _drone = GetComponent<Drone>();
        _mover = GetComponent<IAgentMover>();
    }

    private void OnEnable()
    {
        _drone.OnArrived += HandleArrived;

        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {
        _drone.OnArrived -= HandleArrived;

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

    public bool Assign(DeliveryOrder order)
    {
        if (order == null || CanAcceptWork == false)
        {
            return false;
        }

        _order = order;
        _phase = Phase.ToRack;

        TrackPickUpPoint();

        return true;
    }

    public void Recall()
    {
        if (_phase == Phase.Idle)
        {
            return;
        }

        if (_order != null)
        {
            DeliveryOrder cancelled = _order;

            _order = null;

            cancelled.Cancel();
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
                return false;
            }

            topY = _rack.position.y;

            return true;
        }

        if (_phase == Phase.ToTarget || _phase == Phase.Dropping)
        {
            if (_order == null)
            {
                return false;
            }

            topY = _order.Target.y;

            return true;
        }

        return false;
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

        if (_phase == Phase.Idle || _phase == Phase.Returning)
        {
            TrackDock();
        }
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

            _drone.MoveTo(_order.Target);

            return;
        }

        Drop();
    }

    private void Drop()
    {
        DeliveryOrder finished = _order;

        _order = null;

        PutDown(finished);

        if (finished != null)
        {
            finished.Complete();
        }

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
        if (_order == null || _order.Payload == null)
        {
            return;
        }

        Transform socket = _carrySocket != null ? _carrySocket : transform;

        _order.Payload.transform.SetParent(socket, true);
        _order.Payload.transform.SetPositionAndRotation(socket.position, socket.rotation);
        _order.Payload.SetActive(true);
    }

    private void PutDown(DeliveryOrder order)
    {
        if (order == null || order.Payload == null)
        {
            return;
        }

        order.Payload.transform.SetParent(null, true);
        order.Payload.transform.SetPositionAndRotation(order.Target, order.Rotation);
        order.Payload.SetActive(true);
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
