using System;
using UnityEngine;

[RequireComponent(typeof(Drone))]
public class DroneStateMachine : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GridMapBase _grid;
    [SerializeField] private Transform _dock;

    [Header("작업")]
    [SerializeField, Min(0f)] private float _workDuration = 2f;

    public DroneState State { get { return _state; } }
    public bool CanAcceptWork { get { return _state == DroneState.Docked || _isReturning; } }
    public CellPos WorkCell { get { return _workCell; } }
    public float WorkProgress { get { return GetWorkProgress(); } }

    public event Action<CellPos> OnWorkCompleted;
    public event Action<DroneState> OnStateChanged;

    private Drone _drone;
    private IAgentMover _mover;

    private DroneState _state = DroneState.Docked;
    private CellPos _workCell;
    private CellPos _dockCell;
    private bool _hasDockCell;
    private bool _isReturning;
    private float _workTimer;

    private void Awake()
    {
        _drone = GetComponent<Drone>();
        _mover = GetComponent<IAgentMover>();
    }

    private void OnEnable()
    {
        _drone.OnArrived += HandleArrived;
    }

    private void OnDisable()
    {
        _drone.OnArrived -= HandleArrived;
    }

    private void Start()
    {
        SnapToDock();
    }

    public bool CanAssign(CellPos cell)
    {
        if (CanAcceptWork == false)
        {
            return false;
        }

        if (_drone == null)
        {
            return false;
        }

        return _drone.CanMoveTo(cell);
    }

    public bool Assign(CellPos cell)
    {
        if (CanAssign(cell) == false)
        {
            return false;
        }

        if (_drone.MoveTo(cell) == false)
        {
            return false;
        }

        _workCell = cell;
        _isReturning = false;

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

        if (_state == DroneState.Moving && _isReturning)
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

        OnWorkCompleted?.Invoke(_workCell);

        BeginReturn();
    }

    private void BeginReturn()
    {
        _isReturning = true;
        _hasDockCell = false;

        SetState(DroneState.Moving);

        TrackDock();
    }

    private void TrackDock()
    {
        if (TryGetDockCell(out CellPos cell) == false)
        {
            return;
        }

        if (_hasDockCell && cell == _dockCell)
        {
            return;
        }

        if (_drone.MoveTo(cell) == false)
        {
            return;
        }

        _dockCell = cell;
        _hasDockCell = true;
    }

    private void HandleArrived(CellPos cell)
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
        if (_mover == null)
        {
            return;
        }

        if (TryGetDockCell(out CellPos cell) == false)
        {
            return;
        }

        Vector3 world = _grid.ConvertCellToWorld(cell);

        world.y = transform.position.y;

        _mover.Warp(world);

        _dockCell = cell;
        _hasDockCell = true;
    }

    private bool TryGetDockCell(out CellPos cell)
    {
        cell = default;

        if (_grid == null || _dock == null)
        {
            return false;
        }

        cell = _grid.ConvertWorldToCell(_dock.position);

        return true;
    }

    private void SetState(DroneState next)
    {
        if (_state == next)
        {
            return;
        }

        _state = next;

        OnStateChanged?.Invoke(_state);
    }

    private float GetWorkProgress()
    {
        if (_state != DroneState.Working || _workDuration <= 0f)
        {
            return 0f;
        }

        return 1f - Mathf.Clamp01(_workTimer / _workDuration);
    }
}
