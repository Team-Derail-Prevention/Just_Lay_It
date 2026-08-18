using UnityEngine;

public class DroneDockPoint : MonoBehaviour
{
    [Header("기차 위 위치")]
    // TODO: 정명진님 기차에 드론칸이 생기면 _localOffset 대신 그 Transform에 붙일 것
    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 2f, -3f);

    private void OnEnable()
    {
        TrainManager.OnTrainSpawn += HandleTrainSpawn;
    }

    private void OnDisable()
    {
        TrainManager.OnTrainSpawn -= HandleTrainSpawn;
    }

    private void HandleTrainSpawn(Transform head)
    {
        if (head == null)
        {
            Debug.LogWarning("[DroneDockPoint] 기차 Transform이 null이라 드론칸을 붙이지 못했습니다.");

            return;
        }

        transform.SetParent(head, false);
        transform.localPosition = _localOffset;
        transform.localRotation = Quaternion.identity;

        Debug.Log($"[DroneDockPoint] 기차({head.name})에 드론칸을 붙였습니다.");
    }
}
