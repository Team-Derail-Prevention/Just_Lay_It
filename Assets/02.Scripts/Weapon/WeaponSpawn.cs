using Cysharp.Threading.Tasks;
using Enums;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSpawn : SingletonBase<WeaponSpawn>
{
    protected override void Init()
    {
        base.Init();
    }

    private void OnEnable()
    {
        if (WeaponEquipEventHub.Instance != null)
        {
            WeaponEquipEventHub.Instance.OnWeaponEquipped += OnWeaponEquipped;
            WeaponEquipEventHub.Instance.OnWeaponUnequipped += OnWeaponUnequipped;
        }
    }

    private void OnDisable()
    {
        if (WeaponEquipEventHub.Instance != null)
        {
            WeaponEquipEventHub.Instance.OnWeaponEquipped -= OnWeaponEquipped;
            WeaponEquipEventHub.Instance.OnWeaponUnequipped -= OnWeaponUnequipped;
        }
    }

    private void OnWeaponEquipped(TrainCarSection section, int slotIndex, string weaponDataId)
    {
        WeaponInstall(section, slotIndex, weaponDataId).Forget();
    }

    private void OnWeaponUnequipped(TrainCarSection section, int slotIndex)
    {
        WeaponUninstall(section, slotIndex);
    }

    public async UniTaskVoid WeaponInstall(TrainCarSection section, int slotIndex, string weaponDataId)
    {
        TrainFollow targetTrain = GetTrainBySection(section);
        if (targetTrain == null)
        {
            return;
        }

        if (targetTrain.HasWeaponAtSlot(slotIndex))
        {
            Debug.LogWarning($"[WeaponSpawn] {section}칸 {slotIndex}번 슬롯에 이미 무기가 장착되어 있습니다.");
            return;
        }

        await InstWeapon(weaponDataId, targetTrain, slotIndex);
    }

    public void WeaponUninstall(TrainCarSection section, int slotIndex)
    {
        TrainFollow targetTrain = GetTrainBySection(section);
        if (targetTrain == null)
        {
            return;
        }

        targetTrain.UnmountWeapon(slotIndex);
    }

    private async UniTask InstWeapon(string weaponDataId, TrainFollow targetTrain, int slotIndex)
    {
        WeaponData weaponData = DataManager.Instance.GetData<WeaponData>(weaponDataId);
        if (weaponData == null || targetTrain == null)
        {
            Debug.LogError($"[WeaponSpawn] {weaponDataId} 데이터 조회 실패 또는 대상 열차 없음");
            return;
        }

        GameObject weaponPrefab = await ResourceManager.Instance.LoadAsset<GameObject>(weaponData.WeaponName);
        if (weaponPrefab == null)
        {
            Debug.LogError($"[WeaponSpawn] {weaponData.WeaponName} 프리팹 로드 실패");
            return;
        }

        targetTrain.MountWeapon(weaponPrefab, weaponDataId, slotIndex);
    }

    private TrainFollow GetTrainBySection(TrainCarSection section)
    {
        List<GameObject> trainList = TrainManager.Instance.carList;
        int carIndex = (int)section;

        if (carIndex < 0 || carIndex >= trainList.Count)
        {
            Debug.LogError($"[WeaponSpawn] 유효하지 않은 칸({section})입니다.");
            return null;
        }

        GameObject carObj = trainList[carIndex];
        if (carObj == null)
        {
            Debug.LogError($"[WeaponSpawn] {section}칸이 비어있습니다.");
            return null;
        }

        TrainFollow train = carObj.GetComponent<TrainFollow>();
        if (train == null)
        {
            Debug.LogError($"[WeaponSpawn] {section}칸에서 TrainFollow를 찾을 수 없습니다.");
        }

        return train;
    }

    public WeaponCurrentStats? GetCurrentWeaponStats(TrainCarSection section, int slotIndex)
    {
        TrainFollow train = GetTrainBySection(section);
        if (train == null)
        {
            return null;
        }

        GameObject weaponObj = train.GetWeaponStat(slotIndex);
        if (weaponObj == null)
        {
            return null;
        }

        WeaponFire weaponFire = weaponObj.GetComponent<WeaponFire>();
        if (weaponFire == null)
        {
            return null;
        }

        return weaponFire.GetCurrentStats();
    }
}