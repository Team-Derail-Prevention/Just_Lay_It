using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class ExitConfirmPopup : UIBase
{
    [Header("컴포넌트 연결")]
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;

    [Header("폰트 크기")]
    [SerializeField] private float _customMessageFontSize = 42f;

    private float _defaultFontSize;
    private bool _isDefaultFontSizeCaptured;
    private string _defaultMessage;
    private bool _isDefaultMessageCaptured;
    private Action _onConfirmExit;
    private Action _onCancel;

    private void OnEnable()
    {
        _yesButton.onClick.AddListener(OnClick_Yes);
        _noButton.onClick.AddListener(OnClick_No);
    }

    private void OnDisable()
    {
        _yesButton.onClick.RemoveListener(OnClick_Yes);
        _noButton.onClick.RemoveListener(OnClick_No);

        _onConfirmExit = null;
        _onCancel = null;
    }

    public void Init(string message, Action onConfirmExit, Action onCancel)
    {
        Debug.Log($"[ExitConfirmPopup] Init 호출됨, onConfirmExit target={onConfirmExit?.Target}, method={onConfirmExit?.Method}");

        CaptureDefaultFontSizeIfNeeded();
        CaptureDefaultMessageIfNeeded();

        if (_messageText != null)
        {
            if (string.IsNullOrEmpty(message) == false)
            {
                _messageText.text = message;
                _messageText.fontSize = _customMessageFontSize;
            }
            else
            {
                _messageText.text = _defaultMessage;
                _messageText.fontSize = _defaultFontSize;
            }
        }

        _onConfirmExit = onConfirmExit;
        _onCancel = onCancel;
    }

    private void CaptureDefaultFontSizeIfNeeded()
    {
        if (_isDefaultFontSizeCaptured == true || _messageText == null)
        {
            return;
        }

        _defaultFontSize = _messageText.fontSize;
        _isDefaultFontSizeCaptured = true;
    }

    private void CaptureDefaultMessageIfNeeded()
    {
        if (_isDefaultMessageCaptured == true || _messageText == null)
        {
            return;
        }

        _defaultMessage = _messageText.text;
        _isDefaultMessageCaptured = true;
    }

    private void OnClick_Yes()
    {
        Action onConfirmExit = _onConfirmExit;
        UIManager.Instance.CloseExitConfirmPopup();
        onConfirmExit?.Invoke();
    }

    private void OnClick_No()
    {
        Action onCancel = _onCancel;
        UIManager.Instance.CloseExitConfirmPopup();
        onCancel?.Invoke();
    }
}
