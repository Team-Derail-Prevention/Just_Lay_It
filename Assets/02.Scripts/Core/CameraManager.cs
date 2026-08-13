   using Unity.Cinemachine;
   using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private CinemachineCamera _cinemachineCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[CameraManager:Awake] 현재 인스턴스가 존재하여 중복 오브젝트를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        TrainManager.OnTrainSpawn += TrainSpawn;
    }

    private void OnDisable()
    {
        TrainManager.OnTrainSpawn -= TrainSpawn;

    }

    private void TrainSpawn(Transform headTrain)
    {
        SetCameraTarget(headTrain);
    }

    public void SetCameraTarget(Transform transform)
    {
        _cinemachineCamera.Follow = transform;
    }
}

