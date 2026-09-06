using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GachaUI : UIBase
{
    [Header("카드 슬롯")]
    [SerializeField] private GameObject Prefab_CardSlot;
    [SerializeField] private Transform Transform_CardGroup;

    [Header("재굴림")]
    [SerializeField] private UIButton Button_RerollAll;
    [SerializeField] private TextMeshProUGUI Text_RerollCount;

    [Header("연출")]
    [SerializeField] private float SlotStaggerDelay = 0.25f;

    private GachaViewModel _vm;
    private readonly List<GachaCardUI> _createdCardList = new List<GachaCardUI>();

    private void OnEnable()
    {
        NetworkGachaService.Instance.PreloadGachaIcons();

        if (Button_RerollAll != null)
        {
            Button_RerollAll.BindOnClickButtonEvent(OnClick_RerollAll);
        }

        _vm = NetworkGachaService.Instance.GetLocalGachaViewModel();

        ClearExistingCardSlots();
        CreateAllCardSlots();

        bool isOpened = NetworkGachaService.Instance.OpenGachaBox();
        if (isOpened == false)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "재화가 부족합니다.");
            UIManager.Instance.CloseWeaponGachaUI();
            return;
        }

        PlayDrawAnimationForAllSlots();
        RefreshRerollUI();
    }

    private void OnDisable()
    {
        if (Button_RerollAll != null)
        {
            Button_RerollAll.UnBindOnClickButtonEvent(OnClick_RerollAll);
        }
    }

    private void ClearExistingCardSlots()
    {
        if (Transform_CardGroup == null)
        {
            return;
        }

        for (int i = Transform_CardGroup.childCount - 1; i >= 0; i--)
        {
            Destroy(Transform_CardGroup.GetChild(i).gameObject);
        }

        _createdCardList.Clear();
    }

    private void CreateAllCardSlots()
    {
        if (Prefab_CardSlot == null || Transform_CardGroup == null)
        {
            return;
        }

        for (int i = 0; i < GachaViewModel.CARD_SLOT_COUNT; i++)
        {
            var cardState = _vm.GetCard(i);
            if (cardState == null)
            {
                continue;
            }

            var gObj = Instantiate(Prefab_CardSlot, Transform_CardGroup);
            var cardComponent = gObj.GetComponentInChildren<GachaCardUI>();
            if (cardComponent == null)
            {
                continue;
            }

            cardComponent.InitSlot(cardState, OnClickSelectCard, OnClickRerollSingle);
            _createdCardList.Add(cardComponent);
        }
    }

    private void PlayDrawAnimationForAllSlots()
    {
        for (int i = 0; i < _createdCardList.Count; i++)
        {
            _createdCardList[i].PlayDrawAnimation(i * SlotStaggerDelay);
        }
    }

    private void RefreshRerollUI()
    {
        if (Text_RerollCount != null)
        {
            Text_RerollCount.text = $"x{_vm.RerollCountCurrent}/{_vm.RerollCountMax}";
        }

        if (Button_RerollAll != null)
        {
            Button_RerollAll.SetInteractable(_vm.RerollCountCurrent >= NetworkGachaService.REROLL_COST_ALL);
        }

        bool canRerollSingle = _vm.RerollCountCurrent >= NetworkGachaService.REROLL_COST_SINGLE;
        for (int i = 0; i < _createdCardList.Count; i++)
        {
            _createdCardList[i].SetRerollInteractable(canRerollSingle);
        }
    }

    private void OnClickSelectCard(int slotIndex)
    {
        NetworkGachaService.Instance.RequestSelectCard(slotIndex);
    }

    private bool IsAnyCardSpinning()
    {
        for (int i = 0; i < _createdCardList.Count; i++)
        {
            if (_createdCardList[i].IsSpinning == true)
            {
                return true;
            }
        }

        return false;
    }

    private void OnClickRerollSingle(int slotIndex)
    {
        if (IsAnyCardSpinning() == true)
        {
            return;
        }

        bool isRerolled = NetworkGachaService.Instance.RequestRerollSingle(slotIndex);
        if (isRerolled == false)
        {
            return;
        }

        _createdCardList[slotIndex].PlayDrawAnimation();
        RefreshRerollUI();
    }

    private void OnClick_RerollAll()
    {
        if (IsAnyCardSpinning() == true)
        {
            return;
        }

        bool isRerolled = NetworkGachaService.Instance.RequestRerollAll();
        if (isRerolled == false)
        {
            return;
        }

        PlayDrawAnimationForAllSlots();
        RefreshRerollUI();
    }
}
