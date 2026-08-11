using UnityEngine;

// 테스트 그리드
public class TestGridMap : MonoBehaviour
{
    [Header("맵 크기")]
    [SerializeField, Min(1)] private int _width = 10;
    [SerializeField, Min(1)] private int _height = 10;
    [SerializeField, Min(0.1f)] private float _cellSize = 1f;

    public int Width { get { return _width; } }
    public int Height { get { return _height; } }
    public float CellSize { get { return _cellSize; } }

    public CellPos ConvertWorldToCell(Vector3 world)
    {
        Vector3 local = world - transform.position;

        int x = Mathf.FloorToInt(local.x / _cellSize);
        int y = Mathf.FloorToInt(local.z / _cellSize);

        return new CellPos(x, y);
    }

    public Vector3 ConvertCellToWorld(CellPos cell)
    {
        float x = (cell.X + 0.5f) * _cellSize;
        float z = (cell.Y + 0.5f) * _cellSize;

        return transform.position + new Vector3(x, 0f, z);
    }

    public bool IsInside(CellPos cell)
    {
        if (cell.X < 0 || cell.X >= _width)
        {
            return false;
        }

        if (cell.Y < 0 || cell.Y >= _height)
        {
            return false;
        }

        return true;
    }

    public bool IsWalkable(CellPos cell)
    {
        return IsInside(cell);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);

        Vector3 origin = transform.position;
        float sizeX = _width * _cellSize;
        float sizeZ = _height * _cellSize;

        for (int x = 0; x <= _width; x++)
        {
            Vector3 from = origin + new Vector3(x * _cellSize, 0f, 0f);
            Gizmos.DrawLine(from, from + new Vector3(0f, 0f, sizeZ));
        }

        for (int y = 0; y <= _height; y++)
        {
            Vector3 from = origin + new Vector3(0f, 0f, y * _cellSize);
            Gizmos.DrawLine(from, from + new Vector3(sizeX, 0f, 0f));
        }
    }
}
