using UnityEngine;
using System;

public class DirectionButtonUI : MonoBehaviour
{

    [SerializeField] private UIButton Button_Click;
    [SerializeField] private GameObject GameObject_Up;
    [SerializeField] private GameObject GameObject_Down;

    private Action _onClick;

    private void OnEnable()
    {
        if (Button_Click != null)
        {
            Button_Click.BindOnClickButtonEvent(OnClick_Direction);
        }
    }

    private void OnDisable()
    {
        if (Button_Click != null)
        {
            Button_Click.UnBindOnClickButtonEvent(OnClick_Direction);
        }
    }

    public void InitButton(Action onClick)
    {
        _onClick = onClick;
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (GameObject_Down != null)
        {
            GameObject_Down.SetActive(isSelected);
        }
    }

    private void OnClick_Direction()
    {
        _onClick?.Invoke();
    }
}
