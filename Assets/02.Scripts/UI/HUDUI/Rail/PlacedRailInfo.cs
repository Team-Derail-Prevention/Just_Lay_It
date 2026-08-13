using UnityEngine;

public class PlacedRailInfo : MonoBehaviour
{
    public ERailType RailType { get; set; }
    public Vector2Int GridCell { get; set; }

    // 실체화를 했을 경우 호출할 함수
    public void MarkMaterialized()
    {
        Destroy(this);
    }
}
