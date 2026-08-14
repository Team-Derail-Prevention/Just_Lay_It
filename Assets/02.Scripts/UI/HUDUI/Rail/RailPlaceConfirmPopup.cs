using UnityEngine;
using System;

public class RailPlaceConfirmPopup : UIBase
{
    [SerializeField] private UIButton Button_Rotate;
    [SerializeField] private UIButton Button_Confirm;
    [SerializeField] private UIButton Button_Cancel;

    private Action _onRotate;
    private Action _onConfirm;
    private Action _onCancel;

    private void OnEnable()
    {
        if (Button_Rotate != null)
        {
            Button_Rotate.BindOnClickButtonEvent(OnClick_Rotate);
        }

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
        if (Button_Rotate != null)
        {
            Button_Rotate.UnBindOnClickButtonEvent(OnClick_Rotate);
        }

        if (Button_Confirm != null)
        {
            Button_Confirm.UnBindOnClickButtonEvent(OnClick_Confirm);
        }

        if (Button_Cancel != null)
        {
            Button_Cancel.UnBindOnClickButtonEvent(OnClick_Cancel);
        }

        _onRotate = null;
        _onConfirm = null;
        _onCancel = null;
    }

    public void Init(Action onRotate, Action onConfirm, Action onCancel)
    {
        _onRotate = onRotate;
        _onConfirm = onConfirm;
        _onCancel = onCancel;
    }

    private void OnClick_Rotate()
    {
        _onRotate?.Invoke();
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
