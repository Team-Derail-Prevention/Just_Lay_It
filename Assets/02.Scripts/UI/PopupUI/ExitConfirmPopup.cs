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
        CaptureDefaultFontSizeIfNeeded();

        if (_messageText != null)
        {
            if (string.IsNullOrEmpty(message) == false)
            {
                _messageText.text = message;
                _messageText.fontSize = _customMessageFontSize;
            }
            else
            {
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

    private void OnClick_Yes()
    {
        UIManager.Instance.CloseExitConfirmPopup();
        _onConfirmExit?.Invoke();
    }

    private void OnClick_No()
    {
        UIManager.Instance.CloseExitConfirmPopup();
        _onCancel?.Invoke();
    }
}
