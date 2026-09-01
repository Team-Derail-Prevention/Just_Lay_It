using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class NoticePopupUI : UIBase
{
    [Header("컴포넌트 연결")]
    [SerializeField] private Image _tipImage;
    [SerializeField] private TextMeshProUGUI _enterGuideText;

    [Header("안내 문구")]
    [SerializeField] private string _nextPageGuideMessage = "다음 'Enter'";
    [SerializeField] private string _confirmGuideMessage = "확인 'Enter'";

    [Header("TIP 이미지 목록")]
    [SerializeField] private Sprite[] _tipSprites;

    private int _currentPageIndex;
    private Action _onConfirm;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        bool isEnterPressed = (Keyboard.current.enterKey.wasPressedThisFrame == true || Keyboard.current.numpadEnterKey.wasPressedThisFrame == true);
        if (isEnterPressed == false)
        {
            return;
        }

        ProceedToNextPageOrConfirm();
    }

    private void OnDisable()
    {
        _onConfirm = null;
    }

    public void Init(Action onConfirm)
    {
        _onConfirm = onConfirm;
        _currentPageIndex = 0;

        UpdatePage();
    }

    private void ProceedToNextPageOrConfirm()
    {
        if (_tipSprites == null || _tipSprites.Length == 0)
        {
            Confirm();
            return;
        }

        bool isLastPage = (_currentPageIndex >= _tipSprites.Length - 1);
        if (isLastPage == true)
        {
            Confirm();
            return;
        }

        _currentPageIndex++;
        UpdatePage();
    }

    private void Confirm()
    {
        Action onConfirm = _onConfirm;

        UIManager.Instance.CloseNoticePopup();

        onConfirm?.Invoke();
    }

    private void UpdatePage()
    {
        try
        {
            if (_tipSprites == null || _tipSprites.Length == 0)
            {
                return;
            }

            bool isLastPage = (_currentPageIndex >= _tipSprites.Length - 1);

            if (_tipImage != null)
            {
                _tipImage.sprite = _tipSprites[_currentPageIndex];
            }

            if (_enterGuideText != null)
            {
                _enterGuideText.text = isLastPage ? _confirmGuideMessage : _nextPageGuideMessage;
            }

            Debug.Log($"[NoticePopupUI] UpdatePage 성공. index={_currentPageIndex}, Application.isFocused={Application.isFocused}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NoticePopupUI] UpdatePage 중 예외 발생! index={_currentPageIndex}, 예외={ex}");
        }
    }
}
