using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    private readonly Dictionary<Action, UnityAction> _boundActions = new Dictionary<Action, UnityAction>();

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
            _boundActions.Clear();
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
        if (Button_Base == null || onClickCallback == null)
        {
            return;
        }

        if (_boundActions.ContainsKey(onClickCallback) == true)
        {
            return;
        }

        UnityAction unityAction = onClickCallback.Invoke;
        _boundActions.Add(onClickCallback, unityAction);
        Button_Base.onClick.AddListener(unityAction);
        _isManualUnbindEvent = isManualUnbindEvent;
    }

    public void UnBindOnClickButtonEvent(Action onClickCallback)
    {
        if (Button_Base == null || onClickCallback == null)
        {
            return;
        }

        if (_boundActions.TryGetValue(onClickCallback, out var unityAction) == false)
        {
            return;
        }

        Button_Base.onClick.RemoveListener(unityAction);
        _boundActions.Remove(onClickCallback);
    }

    public void UnBindAllOnClickButtonEvent()
    {
        if (Button_Base == null)
        {
            return;
        }

        Button_Base.onClick.RemoveAllListeners();
        _boundActions.Clear();
    }

    public void ChangeButtonText(string buttonStr)
    {
        if (Text_Base == null)
        {
            return;
        }

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
