using UnityEngine;
using System;

public class RailPlaceConfirmPopup : UIBase
{
    [SerializeField] private UIButton Button_Confirm;
    [SerializeField] private UIButton Button_Cancel;

    private Action _onConfirm;
    private Action _onCancel;

    private void OnEnable()
    {
        if (Button_Confirm != null)
        {
            Button_Confirm.BindOnClickButtonEvent(OnClick_Confirm);
        }

        if (Button_Cancel != null)
        {
            Button_Cancel.BindOnClickButtonEvent(OnClick_Cancel);
        }
    }

    private void OnDisable()
    {
        if (Button_Confirm != null)
        {
            Button_Confirm.UnBindOnClickButtonEvent(OnClick_Confirm);
        }

        if (Button_Cancel != null)
        {
            Button_Cancel.UnBindOnClickButtonEvent(OnClick_Cancel);
        }

        _onConfirm = null;
        _onCancel = null;
    }

    public void Init(Action onConfirm, Action onCancel)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;
    }

    private void OnClick_Confirm()
    {
        _onConfirm?.Invoke();
    }

    private void OnClick_Cancel()
    {
        _onCancel?.Invoke();
    }
}
