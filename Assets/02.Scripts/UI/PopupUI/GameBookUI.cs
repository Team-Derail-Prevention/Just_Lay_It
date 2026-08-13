using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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

    [Header("카테고리 A 영역")]
    [SerializeField] private Image Image_CategoryAIcon;
    [SerializeField] private TextMeshProUGUI Text_CategoryAName;
    [SerializeField] private TextMeshProUGUI Text_CategoryADescription;

    [Header("카테고리 B 영역")]
    [SerializeField] private Image Image_CategoryBIcon;
    [SerializeField] private TextMeshProUGUI Text_CategoryBName;
    [SerializeField] private TextMeshProUGUI Text_CategoryBDescription;

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
        OnClick_CategoryA();

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
    }

    private void OnDisable()
    {
        if (Button_CloseUI != null)
        {
            Button_CloseUI.UnBindOnClickButtonEvent(OnClick_CloseGameBookUI);
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
        // 도감 들어 갈거 정해지면 추후 수정

        SelectFirstSlot();
    }

    private void ReadCategoryBListAndCreateSlot()
    {
        // 도감 들어 갈거 정해지면 추후 수정

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
        // 추후 도감 내용 정해지면 수정

        foreach (var slotKv in _slotList)
        {
            var slot = slotKv.Value;
            var dataId = slot.GetSlotDataId();
            slot.SetSelectedUI(slotDataId == dataId);
        }
    }
}
