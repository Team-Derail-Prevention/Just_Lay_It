using UnityEngine;
using System;
using UnityEngine.UI;

public class NoticePopupUI : UIBase
{
    [Header("컴포넌트 연결")]
    [SerializeField] private Image _tipImage;
    [SerializeField] private Button _leftArrowButton;
    [SerializeField] private Button _rightArrowButton;
    [SerializeField] private Button _okButton;

    [Header("TIP 이미지 목록")]
    [SerializeField] private Sprite[] _tipSprites;

    private int _currentPageIndex;
    private Action _onConfirm;

    private void OnEnable()
    {
        _leftArrowButton.onClick.AddListener(OnClick_LeftArrow);
        _rightArrowButton.onClick.AddListener(OnClick_RightArrow);
        _okButton.onClick.AddListener(OnClick_Ok);
    }

    private void OnDisable()
    {
        _leftArrowButton.onClick.RemoveListener(OnClick_LeftArrow);
        _rightArrowButton.onClick.RemoveListener(OnClick_RightArrow);
        _okButton.onClick.RemoveListener(OnClick_Ok);

        _onConfirm = null;
    }

    public void Init(Action onConfirm)
    {
        _onConfirm = onConfirm;
        _currentPageIndex = 0;

        UpdatePage();
    }

    private void OnClick_LeftArrow()
    {
        bool isFirstPage = (_currentPageIndex <= 0);
        if (isFirstPage == true)
        {
            return;
        }

        _currentPageIndex--;
        UpdatePage();
    }

    private void OnClick_RightArrow()
    {
        bool isLastPage = (_currentPageIndex >= _tipSprites.Length - 1);
        if (isLastPage == true)
        {
            return;
        }

        _currentPageIndex++;
        UpdatePage();
    }

    private void OnClick_Ok()
    {
        Action onConfirm = _onConfirm;

        UIManager.Instance.CloseNoticePopup();

        onConfirm?.Invoke();
    }

    private void UpdatePage()
    {
        if (_tipSprites == null || _tipSprites.Length == 0)
        {
            return;
        }

        bool isFirstPage = (_currentPageIndex <= 0);
        bool isLastPage = (_currentPageIndex >= _tipSprites.Length - 1);

        if (_tipImage != null)
        {
            _tipImage.sprite = _tipSprites[_currentPageIndex];
        }

        _leftArrowButton.gameObject.SetActive(!isFirstPage);
        _rightArrowButton.gameObject.SetActive(!isLastPage);
        _okButton.gameObject.SetActive(isLastPage);
    }

}
