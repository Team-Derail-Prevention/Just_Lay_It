using UnityEngine;
using UnityEngine.InputSystem;

public class GridCursor : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera _camera;
    [SerializeField] private GridMapBase _grid;

    public float CellSize
    {
        get
        {
            if (_grid == null)
            {
                return 1f;
            }

            return _grid.CellSize;
        }
    }

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    public bool TryGetCell(out CellPos cell)
    {
        cell = default;

        if (_camera == null || _grid == null || Mouse.current == null)
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

        if (_grid.IsInside(hit) == false)
        {
            return false;
        }

        cell = hit;

        return true;
    }

    public Vector3 GetCellCenter(CellPos cell)
    {
        if (_grid == null)
        {
            return Vector3.zero;
        }

        return _grid.ConvertCellToWorld(cell);
    }
}
