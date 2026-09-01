using UnityEngine;
using TMPro;
using System;
using UnityEngine.InputSystem;

public class TextInputPopupUI : UIBase
{
    [Header("컴포넌트 연결")]
    [SerializeField] private TextMeshProUGUI Text_Message;
    [SerializeField] private TMP_InputField InputField_Amount;

    private Action<int> _onConfirm;
    private Action _onInvalidInput;
    private int _maxAllowedAmount;

    private void OnEnable()
    {
        if (InputField_Amount != null)
        {
            InputField_Amount.contentType = TMP_InputField.ContentType.IntegerNumber;
            InputField_Amount.lineType = TMP_InputField.LineType.SingleLine;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        bool isEnterPressed = (Keyboard.current.enterKey.wasPressedThisFrame == true || Keyboard.current.numpadEnterKey.wasPressedThisFrame == true);
        if (isEnterPressed == true && InputField_Amount != null)
        {
            OnSubmitAmount(InputField_Amount.text);
            return;                                         
        }

        bool isEscPressed = Input.GetKeyDown(KeyCode.Escape); 
        if (isEscPressed == true)                         
        {                                                     
            OnCancel();                                       
        }
    }

    private void OnDisable()
    {
        _onConfirm = null;
        _onInvalidInput = null;
    }

    public void Init(string message, Action<int> onConfirm, Action onInvalidInput = null, int maxAllowedAmount = int.MaxValue)
    {
        if (Text_Message != null && string.IsNullOrEmpty(message) == false)
        {
            Text_Message.text = message;
        }

        if (InputField_Amount != null)
        {
            InputField_Amount.text = string.Empty;
            InputField_Amount.Select();
            InputField_Amount.ActivateInputField();
        }

        _onConfirm = onConfirm;
        _onInvalidInput = onInvalidInput;
        _maxAllowedAmount = maxAllowedAmount;
    }

    private void OnSubmitAmount(string inputText)
    {
        bool isParsed = int.TryParse(inputText, out int amount);

        if (isParsed == false || amount <= 0 || amount > _maxAllowedAmount)
        {
            Action invalidCallback = _onInvalidInput;
            UIManager.Instance.CloseTextInputPopup();
            invalidCallback?.Invoke();
            return;
        }

        Action<int> confirmCallback = _onConfirm;
        UIManager.Instance.CloseTextInputPopup();
        confirmCallback?.Invoke(amount);
    }

    private void OnCancel()
    {
        UIManager.Instance.CloseTextInputPopup();
    }
}
