using UnityEngine;

public abstract class GridMapBase : MonoBehaviour, IGridMap
{
    public abstract float CellSize { get; }

    public abstract CellPos ConvertWorldToCell(Vector3 world);
    public abstract Vector3 ConvertCellToWorld(CellPos cell);
    public abstract bool IsInside(CellPos cell);
}
