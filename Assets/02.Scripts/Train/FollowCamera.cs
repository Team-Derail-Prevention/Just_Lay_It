using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Runtime Debug / Editable")]
    [SerializeField] private Transform _debugTarget;
    [SerializeField] private int _debugPresetIndex;
    [SerializeField] private int _debugPresetCount;
    [SerializeField] private string _debugPresetId;
    [SerializeField] private Vector3 _debugOffset;
    [SerializeField] private Vector3 _debugTargetPosition;
    [SerializeField] private bool _debugLookAtTarget;
    [SerializeField] private Vector3 _debugFixedRotation;

    [Header("Target Setting")]
    [SerializeField] private Transform _target;

    [Header("Camera Position Offset")]
    [SerializeField] private float _smoothSpeed = 5f;

    private List<CameraData> _cameraPresets = new List<CameraData>();
    private int _currentPresetIndex = 0;

    public event Action<int> OnPresetIndex;

    private void Start()
    {
        InitializeCameraPresetsAsync(this.GetCancellationTokenOnDestroy()).Forget();

        if (_target == null && TrainManager.Instance != null)
        {
            SetTarget(TrainManager.Instance.HeadTransform);
        }
    }

    private void OnEnable()
    {
        TrainManager.OnTrainSpawn += SetTarget;
        TrainManager.OnTrainRelocated += HandleTrainRelocated;
    }

    private void OnDisable()
    {
        TrainManager.OnTrainSpawn -= SetTarget;
        TrainManager.OnTrainRelocated -= HandleTrainRelocated;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleNextPreset();
        }
    }

    private void LateUpdate()
    {
        if (_target == null || _cameraPresets == null || _cameraPresets.Count == 0)
        {
            return;
        }

        CameraData current = _cameraPresets[_currentPresetIndex];

        Vector3 offsetVector = ParseVector3(current.offset);
        Vector3 targetPosition = _target.position + offsetVector;

        transform.position = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);

        if (current.lookAtTarget)
        {
            transform.LookAt(_target);
        }
        else
        {
            Vector3 rotVector = ParseVector3(current.fixedRotation);
            Quaternion targetRot = Quaternion.Euler(rotVector);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _smoothSpeed * Time.deltaTime);
        }

        _debugTarget = _target;
        _debugPresetIndex = _currentPresetIndex;
        _debugPresetCount = _cameraPresets.Count;
        _debugPresetId = current.Id;
        _debugOffset = offsetVector;
        _debugTargetPosition = targetPosition;
        _debugLookAtTarget = current.lookAtTarget;
        _debugFixedRotation = ParseVector3(current.fixedRotation);
    }

    private async UniTask InitializeCameraPresetsAsync(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => GameManager.Data != null && GameManager.Data.IsLoaded,cancellationToken: cancellationToken);

        IReadOnlyList<CameraData> dataList = GameManager.Data.GetAllData<CameraData>();

        if (dataList == null || dataList.Count == 0)
        {
            Debug.LogWarning("[FollowCamera] CameraData가 비어 있습니다.");
            return;
        }

        _cameraPresets = new List<CameraData>(dataList);
        _currentPresetIndex = 0;

        ApplyCurrentPresetRotation();
        OnPresetIndex?.Invoke(_currentPresetIndex);
    }

    private void CycleNextPreset()
    {
        if (_cameraPresets.Count == 0)
        {
            return;
        }

            _currentPresetIndex = (_currentPresetIndex + 1) % _cameraPresets.Count;

        OnPresetIndex?.Invoke(_currentPresetIndex);
    }

    private Vector3 ParseVector3(string s)
    {
        if (string.IsNullOrEmpty(s)) return Vector3.zero;

        string[] split = s.Split(',');
        if (split.Length == 3 &&
            float.TryParse(split[0], out float x) &&
            float.TryParse(split[1], out float y) &&
            float.TryParse(split[2], out float z))
        {
            return new Vector3(x, y, z);
        }

        return Vector3.zero;
    }

    private void ApplyCurrentPresetRotation()
    {
        if (_cameraPresets.Count == 0)
        {
            return;
        }

        CameraData current = _cameraPresets[_currentPresetIndex];

        if (!current.lookAtTarget)
        {
            transform.rotation = Quaternion.Euler(ParseVector3(current.fixedRotation));
        }
    }

    public void SetTarget(Transform targetTransform)
    {
        _target = targetTransform;
    }

    private void HandleTrainRelocated()
    {
        if (TrainManager.Instance != null)
        {
            SetTarget(TrainManager.Instance.HeadTransform);
        }
    }
}