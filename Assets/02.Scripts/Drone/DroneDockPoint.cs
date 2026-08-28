using System;
using System.Collections.Generic;
using UnityEngine;

public class DroneDockPoint : MonoBehaviour
{
    public event Action OnAttached;

    [Header("붙을 칸")]
    [Tooltip("0이면 기관차, 1부터는 뒤쪽 객차 순서")]
    [SerializeField, Min(0)] private int _carIndex = 0;

    [Header("기차 위 위치")]
    // TODO: 정명진님 기차에 드론칸이 생기면 _localOffset 대신 그 Transform에 붙일 것
    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 2f, -3f);
    [SerializeField, Min(0f)] private float _topGap = 1f;

    [Header("배회")]
    [SerializeField, Min(0f)] private float _orbitRadius = 2.5f;
    [SerializeField] private float _orbitSpeed = 25f;

    public bool IsAttached { get { return _anchor != null; } }
    public int CarIndex { get { return _carIndex; } }

    private Transform _anchor;
    private Vector3 _offset;
    private float _phaseDegrees;
    private bool _isOrbiting;

    private void OnEnable()
    {
        TrainManager.OnTrainSpawn += HandleTrainSpawn;

        AttachToCurrentTrain();
    }

    private void OnDisable()
    {
        TrainManager.OnTrainSpawn -= HandleTrainSpawn;
    }

    public void SetCar(int carIndex)
    {
        if (carIndex < 0)
        {
            return;
        }

        if (_carIndex == carIndex)
        {
            return;
        }

        _carIndex = carIndex;
        _anchor = null;

        AttachToCurrentTrain();
    }

    private void LateUpdate()
    {
        if (_anchor == null)
        {
            AttachToCurrentTrain();

            if (_anchor == null)
            {
                return;
            }
        }

        Apply();
    }

    private void HandleTrainSpawn(Transform head)
    {
        if (head == null)
        {
            Debug.LogWarning("[DroneDockPoint] 기차 Transform이 null이라 드론칸을 붙이지 못했습니다.");

            return;
        }

        AttachTo(ResolveCar(head));
    }

    private void AttachToCurrentTrain()
    {
        if (TryResolveCurrentCar(out Transform car) == false)
        {
            return;
        }

        AttachTo(car);
    }

    // TODO: TrainManager에 HeadTrain getter가 열리면 _carIndex 0도 여기서 처리할 것
    private bool TryResolveCurrentCar(out Transform car)
    {
        car = null;

        if (_carIndex <= 0)
        {
            return false;
        }

        if (TrainManager.Instance == null)
        {
            return false;
        }

        List<GameObject> cars = TrainManager.Instance.carList;
        int index = _carIndex - 1;

        if (index >= cars.Count || cars[index] == null)
        {
            return false;
        }

        car = cars[index].transform;

        return true;
    }

    private void AttachTo(Transform car)
    {
        _anchor = car;
        _offset = ResolveOffset(_anchor);

        Apply();

        Debug.Log($"[DroneDockPoint] {_anchor.name}에 드론칸을 붙였습니다. 높이 {_offset.y:F2}");

        OnAttached?.Invoke();
    }

    public void SetOrbitSlot(int slot, int total)
    {
        if (total <= 0)
        {
            return;
        }

        _phaseDegrees = 360f * slot / total;
        _isOrbiting = true;
    }

    private void Apply()
    {
        Vector3 center = _anchor.TransformPoint(_offset);

        transform.SetPositionAndRotation(center + ResolveOrbitOffset(), _anchor.rotation);
    }

    private Vector3 ResolveOrbitOffset()
    {
        if (_isOrbiting == false || _orbitRadius <= 0f)
        {
            return Vector3.zero;
        }

        float angle = (_phaseDegrees + _orbitSpeed * Time.time) * Mathf.Deg2Rad;

        return new Vector3(Mathf.Cos(angle) * _orbitRadius, 0f, Mathf.Sin(angle) * _orbitRadius);
    }

    private Transform ResolveCar(Transform head)
    {
        if (_carIndex <= 0)
        {
            return head;
        }

        if (TrainManager.Instance == null)
        {
            Debug.LogWarning("[DroneDockPoint] TrainManager가 없어 기관차에 붙입니다.");

            return head;
        }

        List<GameObject> cars = TrainManager.Instance.carList;
        int index = _carIndex - 1;

        if (index >= cars.Count || cars[index] == null)
        {
            Debug.LogWarning($"[DroneDockPoint] {_carIndex}번 칸이 없어 기관차에 붙입니다. 현재 객차 {cars.Count}칸");

            return head;
        }

        return cars[index].transform;
    }

    private Vector3 ResolveOffset(Transform car)
    {
        Vector3 offset = _localOffset;

        Renderer[] renderers = car.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return offset;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float topOffsetY = bounds.max.y - car.position.y + _topGap;

        if (topOffsetY > offset.y)
        {
            offset.y = topOffsetY;
        }

        return offset;
    }
}
