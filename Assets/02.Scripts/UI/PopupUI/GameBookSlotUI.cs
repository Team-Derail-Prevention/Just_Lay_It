using UnityEngine;
using System;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class GameBookSlotUI : UIBase
{
    [Header("기본 슬롯 정보")]
    [SerializeField] private Image Image_SlotIcon;
    [SerializeField] private GameObject GameObject_Check;
    [SerializeField] private UIButton Button_SlotClick;

    private event Action<string, EGameBookCategory> _onClickSlot;
    private string _slotDataId;
    private EGameBookCategory _curSlotCategory;

    public string GetSlotDataId()
    {
        return _slotDataId;
    }

    private void OnEnable()
    {
        if (Button_SlotClick != null)
        {
            Button_SlotClick.BindOnClickButtonEvent(OnClick_GameBookSlot);
        }
    }

    private void OnDisable()
    {
        if (Button_SlotClick != null)
        {
            Button_SlotClick.UnBindOnClickButtonEvent(OnClick_GameBookSlot);
        }

        _onClickSlot = null;
    }

    public void OnClick_GameBookSlot()
    {
        _onClickSlot?.Invoke(_slotDataId, _curSlotCategory);
    }

    public void InitSlot(string dataId, EGameBookCategory curCategory, Action<string, EGameBookCategory> onClickCallback)
    {
        string iconPath = null;

        if (curCategory == EGameBookCategory.CategoryA)
        {
            var weaponData = DataManager.Instance.GetData<WeaponData>(dataId);
            if (weaponData != null)
            {
                iconPath = weaponData.IconPath;
            }
        }
        else if (curCategory == EGameBookCategory.CategoryB)
        {
            var monsterData = DataManager.Instance.GetData<MonsterData>(dataId);
            if (monsterData != null)
            {
                iconPath = monsterData.UseIconName;
            }
        }

        if (string.IsNullOrEmpty(iconPath) == false)
        {
            LoadSlotIconAsync(iconPath).Forget();
        }

        _slotDataId = dataId;
        _curSlotCategory = curCategory;
        _onClickSlot += onClickCallback;
    }

    private async UniTaskVoid LoadSlotIconAsync(string iconPath)
    {
        var sprite = await ResourceManager.Instance.LoadAsset<Sprite>(iconPath);
        if (sprite != null && Image_SlotIcon != null)
        {
            Image_SlotIcon.sprite = sprite;
        }
    }

    public void SetSelectedUI(bool isSelect)
    {
        if (GameObject_Check != null)
        {
            GameObject_Check.SetActive(isSelect);
        }
    }
}
