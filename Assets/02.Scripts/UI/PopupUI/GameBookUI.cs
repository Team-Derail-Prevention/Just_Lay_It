using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public enum EGameBookCategory
{
    None = 0,
    CategoryA,
    CategoryB
}

public class GameBookUI : UIBase
{
    [Header("슬롯 프리팹")]
    [SerializeField] private GameObject Prefab_Slot;

    [Header("카테고리 영역")]
    [SerializeField] private GameObject Panel_CategoryA;
    [SerializeField] private GameObject Panel_CategoryB;

    [Header("무기 상세 정보 영역")]
    [SerializeField] private Image Image_WeaponIcon;
    [SerializeField] private TextMeshProUGUI Text_WeaponName;
    [SerializeField] private TextMeshProUGUI Text_WeaponGrade;
    [SerializeField] private TextMeshProUGUI Text_WeaponDescription;
    [SerializeField] private TextMeshProUGUI Text_Damage;
    [SerializeField] private TextMeshProUGUI Text_FireRate;
    [SerializeField] private TextMeshProUGUI Text_Range; 
    [SerializeField] private TextMeshProUGUI Text_MagazineSize;
    [SerializeField] private TextMeshProUGUI Text_ReloadTime;

    [Header("몬스터 상세 정보 영역")]
    [SerializeField] private Image Image_MonsterIcon;
    [SerializeField] private TextMeshProUGUI Text_MonsterName;
    [SerializeField] private TextMeshProUGUI Text_MonsterDescription;
    [SerializeField] private TextMeshProUGUI Text_Hp;
    [SerializeField] private TextMeshProUGUI Text_MonsterAtk;
    [SerializeField] private TextMeshProUGUI Text_Speed;
    [SerializeField] private TextMeshProUGUI Text_AttackType;
    [SerializeField] private TextMeshProUGUI Text_DropGold;

    [Header("카테고리 버튼")]
    [SerializeField] private UIButton Button_CategoryA;
    [SerializeField] private UIButton Button_CategoryB;

    [Header("슬롯 리스트 영역")]
    [SerializeField] private Transform Transform_SlotRoot;

    [Header("닫기 버튼")]
    [SerializeField] private UIButton Button_CloseUI;

    private Dictionary<string, GameBookSlotUI> _slotList = new Dictionary<string, GameBookSlotUI>();

    private void OnEnable()
    {
        if (Button_CloseUI != null)
        {
            Button_CloseUI.BindOnClickButtonEvent(OnClick_CloseGameBookUI);
        }

        if (Button_CategoryA != null)
        {
            Button_CategoryA.BindOnClickButtonEvent(OnClick_CategoryA);
        }

        if (Button_CategoryB != null)
        {
            Button_CategoryB.BindOnClickButtonEvent(OnClick_CategoryB);
        }

        OnClick_CategoryA();
    }

    private void OnDisable()
    {
        if (Button_CloseUI != null)
        {
            Button_CloseUI.UnBindOnClickButtonEvent(OnClick_CloseGameBookUI);
        }

        if (Button_CategoryA != null)
        {
            Button_CategoryA.UnBindOnClickButtonEvent(OnClick_CategoryA);
        }

        if (Button_CategoryB != null)
        {
            Button_CategoryB.UnBindOnClickButtonEvent(OnClick_CategoryB);
        }

        OnDestroyAndClearSlotList();
    }

    private void OnDestroyAndClearSlotList()
    {
        if (_slotList.Count <= 0)
        {
            return;
        }

        foreach (var slotKv in _slotList)
        {
            var slot = slotKv.Value;
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }

        _slotList.Clear();
    }

    public void OnClick_CloseGameBookUI()
    {
        UIManager.Instance.CloseGameBookUI();
    }

    public void OnClick_CategoryA()
    {
        SetGameBookCategory(EGameBookCategory.CategoryA);
    }

    public void OnClick_CategoryB()
    {
        SetGameBookCategory(EGameBookCategory.CategoryB);
    }

    private void SetGameBookCategory(EGameBookCategory category)
    {
        OnDestroyAndClearSlotList();

        switch (category)
        {
            case EGameBookCategory.CategoryA:
                if (Panel_CategoryA != null) Panel_CategoryA.SetActive(true);
                if (Panel_CategoryB != null) Panel_CategoryB.SetActive(false);

                ReadCategoryAListAndCreateSlot();
                break;
            case EGameBookCategory.CategoryB:
                if (Panel_CategoryA != null) Panel_CategoryA.SetActive(false);
                if (Panel_CategoryB != null) Panel_CategoryB.SetActive(true);

                ReadCategoryBListAndCreateSlot();
                break;
            default:
                break;
        }
    }

    private void ReadCategoryAListAndCreateSlot()
    {
        if (DataManager.Instance == null || DataManager.Instance.IsLoaded == false)
        {
            Debug.LogWarning("[GameBookUI] 데이터가 아직 로드되지 않았습니다.");
            return;
        }

        var dataList = DataManager.Instance.GetAllData<WeaponData>();

        foreach (var data in dataList)
        {
            if (data == null)
            {
                continue;
            }

            CreateGameBookSlot(data.Id, EGameBookCategory.CategoryA);
        }

        SelectFirstSlot();
    }

    private void ReadCategoryBListAndCreateSlot()
    {
        if (DataManager.Instance == null || DataManager.Instance.IsLoaded == false)
        {
            Debug.LogWarning("[GameBookUI] 데이터가 아직 로드되지 않았습니다.");
            return;
        }

        var dataList = DataManager.Instance.GetAllData<MonsterData>();

        foreach (var data in dataList)
        {
            if (data == null)
            {
                continue;
            }

            CreateGameBookSlot(data.Id, EGameBookCategory.CategoryB);
        }

        SelectFirstSlot();
    }

    private void SelectFirstSlot()
    {
        if (_slotList.Count <= 0)
        {
            return;
        }

        foreach (var slotKv in _slotList)
        {
            var slot = slotKv.Value;
            slot.OnClick_GameBookSlot();
            return;
        }
    }

    private void CreateGameBookSlot(string dataId, EGameBookCategory curCategory)
    {
        var gObj = Instantiate(Prefab_Slot, Transform_SlotRoot);
        if (gObj == null)
        {
            return;
        }

        var slotComponent = gObj.GetComponent<GameBookSlotUI>();
        if (slotComponent == null)
        {
            return;
        }

        slotComponent.InitSlot(dataId, curCategory, OnClickChildSlotSelected);
        _slotList.Add(dataId, slotComponent);
    }

    private void OnClickChildSlotSelected(string slotDataId, EGameBookCategory selectedSlotCategory)
    {
        foreach (var slotKv in _slotList)
        {
            var slot = slotKv.Value;
            var dataId = slot.GetSlotDataId();
            slot.SetSelectedUI(slotDataId == dataId);
        }

        if (selectedSlotCategory == EGameBookCategory.CategoryA)
        {
            var weaponData = DataManager.Instance.GetData<WeaponData>(slotDataId);
            if (weaponData == null)
            {
                return;
            }

            if (Text_WeaponName != null) Text_WeaponName.text = weaponData.WeaponName;
            if (Text_WeaponGrade != null) Text_WeaponGrade.text = weaponData.GradeName;
            if (Text_WeaponDescription != null) Text_WeaponDescription.text = weaponData.Description;
            if (Text_Damage != null) Text_Damage.text = weaponData.Atk.ToString();
            if (Text_FireRate != null) Text_FireRate.text = weaponData.FireRate.ToString();
            if (Text_Range != null) Text_Range.text = weaponData.Range.ToString();
            if (Text_MagazineSize != null) Text_MagazineSize.text = weaponData.MagazineSize.ToString();
            if (Text_ReloadTime != null) Text_ReloadTime.text = $"{weaponData.ReloadTime} 초";

            if (string.IsNullOrEmpty(weaponData.IconPath) == false)
            {
                LoadIconAsync(Image_WeaponIcon, weaponData.IconPath).Forget();
            }
        }
        else if (selectedSlotCategory == EGameBookCategory.CategoryB)
        {
            var monsterData = DataManager.Instance.GetData<MonsterData>(slotDataId);
            if (monsterData == null)
            {
                return;
            }

            if (Text_MonsterName != null) Text_MonsterName.text = monsterData.MonsterName;
            if (Text_MonsterDescription != null) Text_MonsterDescription.text = monsterData.Description;
            if (Text_Hp != null) Text_Hp.text = monsterData.Hp.ToString();
            if (Text_MonsterAtk != null) Text_MonsterAtk.text = monsterData.Atk.ToString();
            if (Text_Speed != null) Text_Speed.text = monsterData.Speed.ToString();
            if (Text_AttackType != null) Text_AttackType.text = string.IsNullOrEmpty(monsterData.AttackType) ? "None" : monsterData.AttackType;
            if (Text_DropGold != null) Text_DropGold.text = monsterData.DropGold.ToString();

            if (string.IsNullOrEmpty(monsterData.UseIconName) == false)
            {
                LoadIconAsync(Image_MonsterIcon, monsterData.UseIconName).Forget();
            }
        }
    }

    private async UniTaskVoid LoadIconAsync(Image targetImage, string iconPath)
    {
        if (targetImage == null || string.IsNullOrEmpty(iconPath) == true)
        {
            return;
        }

        var sprite = await ResourceManager.Instance.LoadAsset<Sprite>(iconPath);
        if (sprite != null && targetImage != null)
        {
            targetImage.sprite = sprite;
        }
    }
}

