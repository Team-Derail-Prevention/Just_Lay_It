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
    private bool _isSlotsCreated;

    private void OnEnable()
    {
        if (Button_RerollAll != null)
        {
            Button_RerollAll.BindOnClickButtonEvent(OnClick_RerollAll);
        }

        _vm = NetworkGachaService.Instance.GetLocalGachaViewModel();

        if (_isSlotsCreated == false)
        {
            CreateAllCardSlots();
            _isSlotsCreated = true;
        }

        NetworkGachaService.Instance.OpenGachaBox();
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
            var cardComponent = gObj.GetComponent<GachaCardUI>();
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

    private void OnClickRerollSingle(int slotIndex)
    {
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
        bool isRerolled = NetworkGachaService.Instance.RequestRerollAll();
        if (isRerolled == false)
        {
            return;
        }

        PlayDrawAnimationForAllSlots();
        RefreshRerollUI();
    }
}
