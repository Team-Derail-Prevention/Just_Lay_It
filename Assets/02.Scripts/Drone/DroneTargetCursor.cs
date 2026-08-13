using UnityEngine;
using UnityEngine.InputSystem;

public class DroneTargetCursor : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera _camera;

    [Header("레이")]
    [SerializeField, Min(1f)] private float _rayDistance = 500f;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    public bool TryGetTarget(out MaterialObject target)
    {
        target = null;

        if (_camera == null || Mouse.current == null)
        {
            return false;
        }

        Vector2 screen = Mouse.current.position.ReadValue();
        Ray ray = _camera.ScreenPointToRay(screen);

        if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance) == false)
        {
            return false;
        }

        MaterialObject material = hit.collider.GetComponentInParent<MaterialObject>();

        if (material == null)
        {
            return false;
        }

        target = material;

        return true;
    }
}
