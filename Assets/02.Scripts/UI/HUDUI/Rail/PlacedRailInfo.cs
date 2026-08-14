using System.Collections.Generic;
using UnityEngine;

public class PlacedRailInfo : MonoBehaviour
{
    public ERailType RailType { get; set; }
    public Vector2Int GridCell { get; set; }

    private readonly Dictionary<Renderer, Material[]> _originalMaterialsDic = new Dictionary<Renderer, Material[]>();

    public void SaveOriginalMaterials(Dictionary<Renderer, Material[]> originalMaterialsDic)
    {
        _originalMaterialsDic.Clear();
        foreach (var materialKv in originalMaterialsDic)
        {
            _originalMaterialsDic.Add(materialKv.Key, materialKv.Value);
        }
    }

    // 실체화를 할 경우 호출할 함수
    public void MarkMaterialized()
    {
        RestoreOriginalMaterials();
        Destroy(this);
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var materialKv in _originalMaterialsDic)
        {
            var rendererItem = materialKv.Key;
            if (rendererItem != null)
            {
                rendererItem.materials = materialKv.Value;
            }
        }
    }
}
