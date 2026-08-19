using System.Collections.Generic;
using UnityEngine;

public class DroneDockPoint : MonoBehaviour
{
    [Header("붙을 칸")]
    [Tooltip("0이면 기관차, 1부터는 뒤쪽 객차 순서")]
    [SerializeField, Min(0)] private int _carIndex = 0;

    [Header("기차 위 위치")]
    // TODO: 정명진님 기차에 드론칸이 생기면 _localOffset 대신 그 Transform에 붙일 것
    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 2f, -3f);
    [SerializeField, Min(0f)] private float _topGap = 1f;

    public bool IsAttached { get { return _anchor != null; } }

    private Transform _anchor;
    private Vector3 _offset;

    private void OnEnable()
    {
        TrainManager.OnTrainSpawn += HandleTrainSpawn;
    }

    private void OnDisable()
    {
        TrainManager.OnTrainSpawn -= HandleTrainSpawn;
    }

    private void LateUpdate()
    {
        if (_anchor == null)
        {
            return;
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

        _anchor = ResolveCar(head);
        _offset = ResolveOffset(_anchor);

        Apply();

        Debug.Log($"[DroneDockPoint] {_anchor.name}에 드론칸을 붙였습니다. 높이 {_offset.y:F2}");
    }

    private void Apply()
    {
        transform.SetPositionAndRotation(_anchor.TransformPoint(_offset), _anchor.rotation);
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
