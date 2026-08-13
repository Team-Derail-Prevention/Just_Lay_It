using UnityEngine;

public class GeneratedGridMap : GridMapBase
{
    [Header("맵 크기 (MapMaker와 동일)")]
    [SerializeField, Min(1)] private int _gridSizeX = 14;
    [SerializeField, Min(1)] private int _gridSizeZ = 14;
    [SerializeField, Min(0.1f)] private float _spacing = 2f;

    public override float CellSize { get { return _spacing; } }

    public override CellPos ConvertWorldToCell(Vector3 world)
    {
        Vector3 local = world - transform.position;

        int x = Mathf.RoundToInt(local.x / _spacing + _gridSizeX / 2f);
        int z = Mathf.RoundToInt(local.z / _spacing + _gridSizeZ / 2f);

        return new CellPos(x, z);
    }

    public override Vector3 ConvertCellToWorld(CellPos cell)
    {
        float x = (cell.X - _gridSizeX / 2f) * _spacing;
        float z = (cell.Y - _gridSizeZ / 2f) * _spacing;

        return transform.position + new Vector3(x, 0f, z);
    }

    public override bool IsInside(CellPos cell)
    {
        if (cell.X < 0 || cell.X >= _gridSizeX)
        {
            return false;
        }

        if (cell.Y < 0 || cell.Y >= _gridSizeZ)
        {
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.5f);

        float minX = GetEdge(0, _gridSizeX);
        float maxX = GetEdge(_gridSizeX, _gridSizeX);
        float minZ = GetEdge(0, _gridSizeZ);
        float maxZ = GetEdge(_gridSizeZ, _gridSizeZ);

        Vector3 origin = transform.position;

        for (int x = 0; x <= _gridSizeX; x++)
        {
            float edge = GetEdge(x, _gridSizeX);

            Gizmos.DrawLine(origin + new Vector3(edge, 0f, minZ), origin + new Vector3(edge, 0f, maxZ));
        }

        for (int z = 0; z <= _gridSizeZ; z++)
        {
            float edge = GetEdge(z, _gridSizeZ);

            Gizmos.DrawLine(origin + new Vector3(minX, 0f, edge), origin + new Vector3(maxX, 0f, edge));
        }
    }

    private float GetEdge(int index, int size)
    {
        return (index - size / 2f - 0.5f) * _spacing;
    }
}
