using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button Button_Base;
    [SerializeField] private TextMeshProUGUI Text_Base;
    [SerializeField] private Image Image_Base;
    [SerializeField] private Image Image_Select;

    [Header("호버 효과")]
    [SerializeField] private Image Image_BKFrame;
    [SerializeField] private Color _normalFrameColor = Color.white;
    [SerializeField] private Color _hoverFrameColor = Color.green;

    [Header("호버 설명")]
    [SerializeField][TextArea] private string _description;

    private bool _isManualUnbindEvent;
    private Action _boundCallback;
    private UnityAction _boundUnityAction;

    public event Action<string> OnPointerEnterButton;
    public event Action OnPointerExitButton;

    private void Awake()
    {
        InitUIButton();
        SetDefaultUI();
    }

    private void OnDisable()
    {
        if (Button_Base == null)
        {
            return;
        }

        if (_isManualUnbindEvent == false)
        {
            Button_Base.onClick.RemoveAllListeners();
        }
    }


    private void SetDefaultUI()
    {
        if (Image_Select != null)
        {
            Image_Select.gameObject.SetActive(false);
        }

        if (Image_BKFrame != null)
        {
            Image_BKFrame.color = _normalFrameColor;
        }
    }

    private void InitUIButton()
    {
        if (Button_Base != null)
        {
            return;
        }

        var button = this.gameObject.GetComponentInChildren<Button>();

        if (button != null)
        {
            this.Button_Base = button;
        }
    }

    public void BindOnClickButtonEvent(Action onClickCallback, bool isManualUnbindEvent = false)
    {
        if (Button_Base == null) return;

        _boundCallback = onClickCallback;
        _boundUnityAction = onClickCallback.Invoke;
        Button_Base.onClick.AddListener(_boundUnityAction);
        _isManualUnbindEvent = isManualUnbindEvent;
    }

    public void UnBindOnClickButtonEvent(Action onClickCallback)
    {
        if (Button_Base == null || _boundUnityAction == null) return;

        Button_Base.onClick.RemoveListener(_boundUnityAction);
        _boundUnityAction = null;
        _boundCallback = null;
    }

    public void UnBindAllOnClickButtonEvent()
    {
        if (Button_Base == null)
        {
            return;
        }

        Button_Base.onClick.RemoveAllListeners();
    }

    public void ChangeButtonText(string buttonStr)
    {
        if (Text_Base == null) return;

        Text_Base.text = buttonStr;
    }
    public void SetSelectedUI(bool isSelected)
    {
        if (Image_Select == null)
        {
            return;
        }

        Image_Select.gameObject.SetActive(isSelected);
    }

    public void SetInteractable(bool isInteractable)
    {
        if (Button_Base == null)
        {
            return;
        }

        Button_Base.interactable = isInteractable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Image_BKFrame != null)
        {
            Image_BKFrame.color = _hoverFrameColor;
        }

        OnPointerEnterButton?.Invoke(_description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Image_BKFrame != null)
        {
            Image_BKFrame.color = _normalFrameColor;
        }

        OnPointerExitButton?.Invoke();
    }
}
