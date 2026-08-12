using UnityEngine;

public interface IGridMap
{
    float CellSize { get; }
    CellPos ConvertWorldToCell(Vector3 world);
    Vector3 ConvertCellToWorld(CellPos cell);
    bool IsInside(CellPos cell);
}
