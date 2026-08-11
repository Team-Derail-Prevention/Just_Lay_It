using UnityEngine;
using UnityEngine.InputSystem;

public class DroneClickCommand : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera _camera;
    [SerializeField] private TestGridMap _grid;
    [SerializeField] private Drone _drone;

    private bool _hasHoverCell;
    private CellPos _hoverCell;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        _hasHoverCell = TryGetCellUnderMouse(out _hoverCell);

        if (Mouse.current.leftButton.wasPressedThisFrame == false)
        {
            return;
        }

        if (_hasHoverCell == false)
        {
            return;
        }

        _drone.MoveTo(_hoverCell);
    }

    private bool TryGetCellUnderMouse(out CellPos cell)
    {
        cell = default;

        if (_camera == null || _grid == null)
        {
            return false;
        }

        Vector2 screen = Mouse.current.position.ReadValue();
        Ray ray = _camera.ScreenPointToRay(screen);
        Plane ground = new Plane(Vector3.up, _grid.transform.position);

        if (ground.Raycast(ray, out float distance) == false)
        {
            return false;
        }

        CellPos hit = _grid.ConvertWorldToCell(ray.GetPoint(distance));

        if (_grid.IsWalkable(hit) == false)
        {
            return false;
        }

        cell = hit;

        return true;
    }

    private void OnDrawGizmos()
    {
        if (_hasHoverCell == false || _grid == null)
        {
            return;
        }

        Gizmos.color = Color.green;

        Vector3 center = _grid.ConvertCellToWorld(_hoverCell);
        Vector3 size = new Vector3(_grid.CellSize, 0.05f, _grid.CellSize);

        Gizmos.DrawWireCube(center, size);
    }
}
