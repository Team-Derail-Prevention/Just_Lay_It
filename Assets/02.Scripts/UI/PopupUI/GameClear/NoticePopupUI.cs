using UnityEngine;
using System;
using TMPro;

public class NoticePopupUI : UIBase
{
    [Header("컴포넌트 연결")]
    [SerializeField] private TextMeshProUGUI _messageText;

    private Action _onConfirm;

    private void Update()
    {
        bool isEnterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (isEnterPressed == true)
        {
            OnConfirm();
        }
    }

    private void OnDisable()
    {
        _onConfirm = null;
    }

    public void Init(string message, Action onConfirm)
    {
        if (_messageText != null)
        {
            _messageText.text = message;
        }

        _onConfirm = onConfirm;
    }

    private void OnConfirm()
    {
        Action onConfirm = _onConfirm;
        UIManager.Instance.CloseNoticePopup();
        onConfirm?.Invoke();
    }
}
