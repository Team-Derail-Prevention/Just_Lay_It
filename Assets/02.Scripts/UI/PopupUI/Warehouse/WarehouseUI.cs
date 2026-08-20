using UnityEngine;
using Enums;
using TMPro;

public class WarehouseUI : UIBase
{
    private const string DEFAULT_AMOUNT_MESSAGE = "원하는 숫자를 기입 하세요.\n(기입 후 엔터)";

    [Header("돌")]
    [SerializeField] private UIButton Button_Stone;
    [SerializeField] private GameObject GameObject_StoneCheck;
    [SerializeField] private TextMeshProUGUI Text_StoneAmount;

    [Header("나무")]
    [SerializeField] private UIButton Button_Wood;
    [SerializeField] private GameObject GameObject_WoodCheck;
    [SerializeField] private TextMeshProUGUI Text_WoodAmount;

    [Header("수량 입력 및 적용")]
    [SerializeField] private UIButton Button_Input;
    [SerializeField] private TextMeshProUGUI Text_Amount;
    [SerializeField] private UIButton Button_PutIn; 
    [SerializeField] private UIButton Button_PutOut;

    [Header("나가기")]
    [SerializeField] private UIButton Button_Exit;

    private MaterialObejct? _selectedMaterialType;
    private int _confirmedAmount;

    private void OnEnable()
    {
        if (Button_Stone != null)
        {
            Button_Stone.BindOnClickButtonEvent(OnClick_SelectStone);
        }

        if (Button_Wood != null)
        {
            Button_Wood.BindOnClickButtonEvent(OnClick_SelectWood);
        }

        if (Button_Input != null)
        {
            Button_Input.BindOnClickButtonEvent(OnClick_Input);
        }

        if (Button_PutIn != null)
        {
            Button_PutIn.BindOnClickButtonEvent(OnClick_PutIn);
        }

        if (Button_PutOut != null)
        {
            Button_PutOut.BindOnClickButtonEvent(OnClick_PutOut);
        }

        if (Button_Exit != null)
        {
            Button_Exit.BindOnClickButtonEvent(OnClick_Exit);
        }

        if (MaterialTransferEventHub.Instance != null)
        {
            MaterialTransferEventHub.Instance.OnWarehouseStoneChanged += SetStoneAmountText;
            MaterialTransferEventHub.Instance.OnWarehouseWoodChanged += SetWoodAmountText;
        }

        _selectedMaterialType = null;
        _confirmedAmount = 0;
        RefreshAmountText();

        if (GameObject_StoneCheck != null)
        {
            GameObject_StoneCheck.SetActive(false);
        }

        if (GameObject_WoodCheck != null)
        {
            GameObject_WoodCheck.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (Button_Stone != null)
        {
            Button_Stone.UnBindOnClickButtonEvent(OnClick_SelectStone);
        }

        if (Button_Wood != null)
        {
            Button_Wood.UnBindOnClickButtonEvent(OnClick_SelectWood);
        }

        if (Button_Input != null)
        {
            Button_Input.UnBindOnClickButtonEvent(OnClick_Input);
        }

        if (Button_PutIn != null)
        {
            Button_PutIn.UnBindOnClickButtonEvent(OnClick_PutIn);
        }

        if (Button_PutOut != null)
        {
            Button_PutOut.UnBindOnClickButtonEvent(OnClick_PutOut);
        }

        if (Button_Exit != null)
        {
            Button_Exit.UnBindOnClickButtonEvent(OnClick_Exit);
        }

        if (MaterialTransferEventHub.Instance != null)
        {
            MaterialTransferEventHub.Instance.OnWarehouseStoneChanged -= SetStoneAmountText;
            MaterialTransferEventHub.Instance.OnWarehouseWoodChanged -= SetWoodAmountText;
        }
    }

    private void OnClick_SelectStone()
    {
        SelectMaterial(MaterialObejct.Rock);
    }

    private void OnClick_SelectWood()
    {
        SelectMaterial(MaterialObejct.DeadTree);
    }

    // + 돌/나무 중 하나만 선택 가능, Check 표시로 어떤 걸 골랐는지 시각적으로 알려줌
    private void SelectMaterial(MaterialObejct materialType)
    {
        _selectedMaterialType = materialType;

        if (GameObject_StoneCheck != null)
        {
            GameObject_StoneCheck.SetActive(materialType == MaterialObejct.Rock);
        }

        if (GameObject_WoodCheck != null)
        {
            GameObject_WoodCheck.SetActive(materialType == MaterialObejct.DeadTree);
        }
    }

    private void OnClick_Input()
    {
        if (_selectedMaterialType == null)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "재료를 먼저 선택해주세요.");
            return;
        }

        OpenAmountInputPopup(DEFAULT_AMOUNT_MESSAGE);
    }

    private void OpenAmountInputPopup(string message)
    {
        UIManager.Instance.OpenTextInputPopup(message, OnAmountConfirmed, OnInvalidAmountInput);
    }

    private void OnAmountConfirmed(int amount)
    {
        _confirmedAmount = amount;
        RefreshAmountText();
    }

    private void OnInvalidAmountInput()
    {
        UIManager.Instance.OpenExitConfirmPopup(OnRetryAmountInputAfterAlert, OnRetryAmountInputAfterAlert, "숫자를 입력해주세요.");
    }

    private void OnRetryAmountInputAfterAlert()
    {
        OpenAmountInputPopup(DEFAULT_AMOUNT_MESSAGE);
    }

    private void RefreshAmountText()
    {
        if (Text_Amount != null)
        {
            Text_Amount.text = _confirmedAmount.ToString();
        }
    }

    private void SetStoneAmountText(int curStoneCount)
    {
        if (Text_StoneAmount != null)
        {
            Text_StoneAmount.text = curStoneCount.ToString();
        }
    }

    private void SetWoodAmountText(int curWoodCount)
    {
        if (Text_WoodAmount != null)
        {
            Text_WoodAmount.text = curWoodCount.ToString();
        }
    }

    private void OnClick_PutIn()
    {
        if (_selectedMaterialType == null)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "재료를 먼저 선택해주세요.");
            return;
        }

        if (_confirmedAmount <= 0)
        {
            UIManager.Instance.OpenExitConfirmPopup(OnRetryAmountInputAfterAlert, OnRetryAmountInputAfterAlert, "숫자를 입력해주세요.");
            return;
        }

        MaterialTransferEventHub.Instance.RequestPutIntoWarehouse(_selectedMaterialType.Value, _confirmedAmount, OnTransferResult);
    }

    private void OnClick_PutOut()
    {
        if (_selectedMaterialType == null)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "재료를 먼저 선택해주세요.");
            return;
        }

        if (_confirmedAmount <= 0)
        {
            UIManager.Instance.OpenExitConfirmPopup(OnRetryAmountInputAfterAlert, OnRetryAmountInputAfterAlert, "숫자를 입력해주세요.");
            return;
        }

        MaterialTransferEventHub.Instance.RequestTakeFromWarehouse(_selectedMaterialType.Value, _confirmedAmount, OnTransferResult);
    }

    private void OnTransferResult(bool isSuccess)
    {
        if (isSuccess == false)
        {
            UIManager.Instance.OpenExitConfirmPopup(OnRetryAmountInputAfterAlert, OnRetryAmountInputAfterAlert, "수량이 부족합니다.\n다시 기입해주세요.");
            return;
        }
    }

    private void OnClick_Exit()
    {
        UIManager.Instance.CloseWarehouseUI();
    }
}
