using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Target Setting")]
    [SerializeField] private Transform _target;             // 추적할 대상 (기차 머리)

    [Header("Camera Position Offset")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 15f, -10f); // 쿼터뷰 기본 거리/높이 (조절 가능)
    [SerializeField] private float _smoothSpeed = 5f;        // 추적 부드러움 (수치가 클수록 딱 붙어감)

    [Header("Camera Rotation")]
    [SerializeField] private bool _lookAtTarget = false;     // 타겟을 실시간으로 바라볼지 여부
    [SerializeField] private Vector3 _fixedRotation = new Vector3(55f, 0f, 0f); // 각도 고정용 (lookAtTarget이 false일 때)

    private void Start()
    {
        // 고정 각도 모드라면 시작할 때 카메라 각도 세팅
        if (!_lookAtTarget)
        {
            transform.rotation = Quaternion.Euler(_fixedRotation);
        }
    }

    private void OnEnable()
    {
        // TrainManager의 소환 이벤트가 있다면 자동 구독
        TrainManager.OnTrainSpawn += SetTarget;
    }

    private void OnDisable()
    {
        TrainManager.OnTrainSpawn -= SetTarget;
    }

    // 오브젝트 이동 후 카메라가 따라가도록 LateUpdate 사용 (떨림 현상 방지)
    private void LateUpdate()
    {
        if (_target == null) return;

        // 1. 목표 위치 계산 (타겟 위치 + 오프셋)
        Vector3 targetPosition = _target.position + _offset;

        // 2. 부드럽게 이동 (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);

        // 3. 회전 처리
        if (_lookAtTarget)
        {
            transform.LookAt(_target);
        }
    }

    public void SetTarget(Transform targetTransform)
    {
        _target = targetTransform;
    }
}
