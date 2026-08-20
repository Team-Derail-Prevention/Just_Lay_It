using UnityEngine;

[RequireComponent(typeof(Drone))]
public class DroneStateMachine : MonoBehaviour, IDroneWorker
{
    [Header("참조")]
    [SerializeField] private Transform _dock;

    // 이동 중에는 꺼둡니다. 켜져 있으면 지나치는 자원까지 채굴이 시작됩니다.
    [SerializeField] private Collider _workTrigger;

    [Header("작업")]
    [SerializeField, Min(0f)] private float _workDuration = 2f;

    public DroneState State { get { return _state; } }
    public bool CanAcceptWork { get { return _state == DroneState.Docked || _isReturning; } }
    public Transform Transform { get { return transform; } }

    private Drone _drone;
    private IAgentMover _mover;
    private DroneDockPoint _dockPoint;

    private DroneState _state = DroneState.Docked;
    private MaterialObject _workTarget;
    private bool _isReturning;
    private float _workTimer;

    private void Awake()
    {
        _drone = GetComponent<Drone>();
        _mover = GetComponent<IAgentMover>();

        if (_dock != null)
        {
            _dockPoint = _dock.GetComponent<DroneDockPoint>();
        }

        if (_workTrigger == null)
        {
            _workTrigger = GetComponent<Collider>();
        }

        UpdateWorkTrigger();
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

    public bool TryGetWorkTopY(out float topY)
    {
        topY = 0f;

        if (_workTarget == null)
        {
            return TryGetDockY(out topY);
        }

        if (_workTarget.TryGetComponent(out Collider targetCollider) == false)
        {
            return TryGetDockY(out topY);
        }

        topY = targetCollider.bounds.max.y;

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

    private void Start()
    {
        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.Register(this);
        }

        SnapToDock();
    }

    public bool CanAssign(MaterialObject target)
    {
        if (CanAcceptWork == false)
        {
            return false;
        }

        if (target == null)
        {
            return false;
        }

        return target.IsBroken == false && target.IsMining == false;
    }

    public bool Assign(MaterialObject target)
    {
        if (CanAssign(target) == false)
        {
            return false;
        }

        _workTarget = target;
        _isReturning = false;

        _drone.MoveTo(target.transform.position);

        SetState(DroneState.Moving);

        return true;
    }

    public void Recall()
    {
        if (_state == DroneState.Docked)
        {
            return;
        }

        BeginReturn();
    }

    private void Update()
    {
        if (_state == DroneState.Working)
        {
            UpdateWork();

            return;
        }

        if (_state == DroneState.Docked)
        {
            return;
        }

        if (_state == DroneState.Moving && _isReturning)
        {
            TrackDock();
        }
    }

    private void LateUpdate()
    {
        if (_state != DroneState.Docked)
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

        BeginReturn();
    }

    private void BeginReturn()
    {
        _isReturning = true;
        _workTarget = null;

        SetState(DroneState.Moving);

        TrackDock();
    }

    // 기차가 움직이면 드론칸도 따라 움직이므로 매 프레임 목표를 갱신합니다.
    private void TrackDock()
    {
        if (_dock == null)
        {
            return;
        }

        _drone.MoveTo(_dock.position);
    }

    private void HandleArrived()
    {
        if (_state != DroneState.Moving)
        {
            return;
        }

        if (_isReturning)
        {
            _isReturning = false;

            SetState(DroneState.Docked);

            return;
        }

        _workTimer = _workDuration;

        SetState(DroneState.Working);
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

    private void SetState(DroneState next)
    {
        if (_state == next)
        {
            return;
        }

        _state = next;

        UpdateWorkTrigger();
    }

    private void UpdateWorkTrigger()
    {
        if (_workTrigger == null)
        {
            return;
        }

        _workTrigger.enabled = _state == DroneState.Working;
    }
}
