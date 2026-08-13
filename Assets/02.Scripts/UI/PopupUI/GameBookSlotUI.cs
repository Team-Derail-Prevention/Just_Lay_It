using UnityEngine;
using System;
using UnityEngine.UI;

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
        // 추후 도감 내용 정해지면 수정

        _slotDataId = dataId;
        _curSlotCategory = curCategory;
        _onClickSlot = onClickCallback;
    }

    public void SetSelectedUI(bool isSelect)
    {
        if (GameObject_Check != null)
        {
            GameObject_Check.SetActive(isSelect);
        }
    }
}
