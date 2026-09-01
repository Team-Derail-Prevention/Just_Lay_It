using Enums;
using System.Collections.Generic;
using UnityEngine;

public class TrainFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform _frontTrain;


    [Header("Follow Setting")]
    [SerializeField] private float _followDistance = 3.0f;
    [SerializeField] private float _reachThreshold = 0.2f;
    [SerializeField] private float _maxSpeedMultiplier = 2.5f;
    [SerializeField] private int _targetIndex = 0;

    [Header("Weapon Setting")]
    [SerializeField] private Transform _weaponSlotsRoot;

    private TrainData _trainData;
    private float _moveSpeed = 2f;
    private float _rotateSpeed = 5f;

    private List<Transform> _weaponSlots = new List<Transform>();
    private GameObject[] _equippedWeaponObjs;

    public TrainData Data
    {
        get { return _trainData; }
    }

    private void Awake()
    {
        InitWeaponSlots();
    }

    private void Update()
    {
        FollowFrontTrain();
    }

    public void FollowInit(TrainData data, Transform frontTrain)
    {
        _trainData = data;

        if (data != null)
        {
            _moveSpeed = data.MoveSpeed;
            _rotateSpeed = data.RotateSpeed;
        }

        SetFrontTrain(frontTrain);
    }

    private void FollowFrontTrain()
    {
        if (_frontTrain == null || TrainManager.Instance == null)
        {
            return;
        }

        if (TrainManager.Instance.IsStation)
        {
            return;
        }

        float distanceFront = Vector3.Distance(transform.position, _frontTrain.position);
        if (distanceFront <= _followDistance)
        {
            return;
        }

        Transform targetNode = TrainManager.Instance.GetWaypoint(_targetIndex);
        if (targetNode == null)
        {
            return;
        }

        //벌어진 거리만큼 가속 (기본 1.0배 ~ 최대 _maxSpeedMultiplier 배)
        float distanceExcess = distanceFront - _followDistance;
        float speedMultiplier = Mathf.Clamp(1f + (distanceExcess * 1.5f), 1f, _maxSpeedMultiplier);
        float currentSpeed = _moveSpeed * speedMultiplier;



        Vector3 direction = targetNode.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotateSpeed);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetNode.position, currentSpeed * Time.deltaTime);


        if (Vector3.Distance(transform.position, targetNode.position) <= _reachThreshold)
        {
            _targetIndex++;
        }
    }

    public void SetFrontTrain(Transform frontTrain)
    {
        _frontTrain = frontTrain;
    }

    public void SetTargetIndex(int index = 0)
    {
        _targetIndex = index;
    }

    private void InitWeaponSlots()
    {
        _weaponSlots.Clear();

        if (_weaponSlotsRoot == null)
        {
            Debug.LogWarning("[TrainFollow] WeaponSlotsRoot가 연결되어 있지 않습니다.");
            return;
        }

        for (int i = 0; i < _weaponSlotsRoot.childCount; i++)
        {
            _weaponSlots.Add(_weaponSlotsRoot.GetChild(i));
        }

        _equippedWeaponObjs = new GameObject[_weaponSlots.Count];
    }

    public bool HasWeaponAtSlot(int slotIndex)
    {
        if (_equippedWeaponObjs == null || slotIndex < 0 || slotIndex >= _equippedWeaponObjs.Length)
        {
            return false;
        }

        return _equippedWeaponObjs[slotIndex] != null;
    }

    public bool MountWeapon(GameObject weaponPrefab, string weaponDataId, int slotIndex)
    {
        if (weaponPrefab == null)
        {
            return false;
        }

        if (slotIndex < 0 || slotIndex >= _weaponSlots.Count)
        {
            Debug.LogWarning($"[TrainFollow] 유효하지 않은 슬롯 인덱스({slotIndex})입니다.");
            return false;
        }

        if (HasWeaponAtSlot(slotIndex))
        {
            Debug.LogWarning($"[TrainFollow] {slotIndex}번 슬롯에 이미 무기가 장착되어 있습니다.");
            return false;
        }

        Transform slotTransform = _weaponSlots[slotIndex];

        GameObject weaponObj = Instantiate(weaponPrefab, slotTransform.position, slotTransform.rotation, slotTransform);
        weaponObj.transform.localPosition = Vector3.zero;
        weaponObj.transform.localRotation = Quaternion.identity;

        WeaponFire weaponFire = weaponObj.GetComponent<WeaponFire>();
        if (weaponFire != null)
        {
            weaponFire.SetWeaponId(weaponDataId);
        }

        _equippedWeaponObjs[slotIndex] = weaponObj;

        return true;
    }

    public void UnmountWeapon(int slotIndex)
    {
        if (_equippedWeaponObjs == null || slotIndex < 0 || slotIndex >= _equippedWeaponObjs.Length)
        {
            return;
        }

        if (_equippedWeaponObjs[slotIndex] != null)
        {
            Destroy(_equippedWeaponObjs[slotIndex]);
            _equippedWeaponObjs[slotIndex] = null;
        }
    }

    public GameObject GetWeaponStat(int slotIndex)
    {
        if (_equippedWeaponObjs == null || slotIndex < 0 || slotIndex >= _equippedWeaponObjs.Length)
        {
            return null;
        }

        return _equippedWeaponObjs[slotIndex];
    }
}