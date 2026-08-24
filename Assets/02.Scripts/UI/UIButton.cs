using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    [SerializeField] private Button Button_Base;
    [SerializeField] private TextMeshProUGUI Text_Base;
    [SerializeField] private Image Image_Base;
    [SerializeField] private Image Image_Select;

    private bool _isManualUnbindEvent;
    private Action _boundCallback;
    private UnityAction _boundUnityAction;

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
}
