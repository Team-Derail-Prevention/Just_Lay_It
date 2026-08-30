using UnityEngine;
using UnityEngine.InputSystem;

public class DroneTargetCursor : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera _camera;

    [Header("레이")]
    [SerializeField, Min(1f)] private float _rayDistance = 500f;
    [SerializeField] private LayerMask _groundMask;

    [Header("칸 탐색")]
    [SerializeField] private LayerMask _materialMask;
    [SerializeField, Min(0.1f)] private float _cellCheckRadius = 1f;
    [SerializeField, Min(0.1f)] private float _cellCheckHeight = 3f;

    private readonly Collider[] _cellBuffer = new Collider[16];

    private MaterialObject _cellCacheMaterial;

    private int _cachedFrame = -1;
    private bool _cachedResult;
    private Vector3 _cachedCellCenter;
    private MaterialObject _cachedTarget;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_groundMask == 0)
        {
            _groundMask = LayerMask.GetMask("Ground");
        }

        if (_materialMask == 0)
        {
            _materialMask = LayerMask.GetMask("OnGround");
        }
    }

    public bool TryGetTarget(out MaterialObject target)
    {
        return TryGetCell(out _, out target);
    }

    public bool TryGetCell(out Vector3 cellCenter, out MaterialObject target)
    {
        if (_cachedFrame == Time.frameCount)
        {
            cellCenter = _cachedCellCenter;
            target = _cachedTarget;

            return _cachedResult;
        }

        _cachedFrame = Time.frameCount;
        _cachedResult = Resolve(out _cachedCellCenter, out _cachedTarget);

        cellCenter = _cachedCellCenter;
        target = _cachedTarget;

        return _cachedResult;
    }

    private bool Resolve(out Vector3 cellCenter, out MaterialObject target)
    {
        cellCenter = Vector3.zero;
        target = null;

        if (_camera == null || Mouse.current == null)
        {
            return false;
        }

        Vector2 screen = Mouse.current.position.ReadValue();
        Ray ray = _camera.ScreenPointToRay(screen);

        if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _groundMask) == false)
        {
            return false;
        }

        target = GetMaterialAt(hit.point);

        if (target == null)
        {
            return false;
        }

        Vector3 materialPosition = target.transform.position;

        cellCenter = new Vector3(materialPosition.x, hit.point.y, materialPosition.z);

        return true;
    }

    private MaterialObject GetMaterialAt(Vector3 groundPoint)
    {
        if (IsCacheUsable(groundPoint))
        {
            return _cellCacheMaterial;
        }

        _cellCacheMaterial = FindNearestMaterial(groundPoint);

        return _cellCacheMaterial;
    }

    private bool IsCacheUsable(Vector3 groundPoint)
    {
        if (_cellCacheMaterial == null)
        {
            return false;
        }

        if (_cellCacheMaterial.IsBroken)
        {
            return false;
        }

        return IsInsideCell(_cellCacheMaterial.transform.position, groundPoint);
    }

    private bool IsInsideCell(Vector3 materialPosition, Vector3 groundPoint)
    {
        float distanceX = Mathf.Abs(materialPosition.x - groundPoint.x);
        float distanceZ = Mathf.Abs(materialPosition.z - groundPoint.z);

        return distanceX <= _cellCheckRadius && distanceZ <= _cellCheckRadius;
    }

    private MaterialObject FindNearestMaterial(Vector3 groundPoint)
    {
        Vector3 center = groundPoint + Vector3.up * (_cellCheckHeight * 0.5f);
        Vector3 halfExtents = new Vector3(_cellCheckRadius, _cellCheckHeight * 0.5f, _cellCheckRadius);

        int count = Physics.OverlapBoxNonAlloc(
            center,
            halfExtents,
            _cellBuffer,
            Quaternion.identity,
            _materialMask,
            QueryTriggerInteraction.Collide);

        MaterialObject nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (_cellBuffer[i] == null)
            {
                continue;
            }

            MaterialObject material = _cellBuffer[i].GetComponentInParent<MaterialObject>();

            if (material == null)
            {
                continue;
            }

            if (material.IsBroken)
            {
                continue;
            }

            float distance = SqrDistanceXZ(material.transform.position, groundPoint);

            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearest = material;
        }

        return nearest;
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float deltaX = a.x - b.x;
        float deltaZ = a.z - b.z;

        return (deltaX * deltaX) + (deltaZ * deltaZ);
    }
}
