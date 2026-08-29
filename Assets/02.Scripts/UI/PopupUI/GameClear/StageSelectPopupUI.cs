using UnityEngine;
using System;
using Enums;

public class StageSelectPopupUI : UIBase
{
    [Header("난이도 버튼")]
    [SerializeField] private UIButton _btnNormal;
    [SerializeField] private UIButton _btnHard;
    [SerializeField] private UIButton _btnVeryHard;

    [Header("취소 버튼")]
    [SerializeField] private UIButton _btnCancel;

    private Action<GameStage> _onSelectStage;
    private Action _onCancel;

    private void OnEnable()
    {
        _btnNormal.BindOnClickButtonEvent(OnClick_Normal);
        _btnHard.BindOnClickButtonEvent(OnClick_Hard);
        _btnVeryHard.BindOnClickButtonEvent(OnClick_VeryHard);
        _btnCancel.BindOnClickButtonEvent(OnClick_Cancel);
    }

    private void OnDisable()
    {
        _btnNormal.UnBindOnClickButtonEvent(OnClick_Normal);
        _btnHard.UnBindOnClickButtonEvent(OnClick_Hard);
        _btnVeryHard.UnBindOnClickButtonEvent(OnClick_VeryHard);
        _btnCancel.UnBindOnClickButtonEvent(OnClick_Cancel);

        _onSelectStage = null;
        _onCancel = null;
    }

    public void Init(Action<GameStage> onSelectStage, Action onCancel)
    {
        _onSelectStage = onSelectStage;
        _onCancel = onCancel;
    }

    private void OnClick_Normal()
    {
        SelectStage(GameStage.Stage1);
    }

    private void OnClick_Hard()
    {
        SelectStage(GameStage.Stage2);
    }

    private void OnClick_VeryHard()
    {
        SelectStage(GameStage.Stage3);
    }

    private void SelectStage(GameStage stage)
    {
        Action<GameStage> onSelectStage = _onSelectStage;
        UIManager.Instance.CloseStageSelectPopup();
        onSelectStage?.Invoke(stage);
    }

    private void OnClick_Cancel()
    {
        Action onCancel = _onCancel;
        UIManager.Instance.CloseStageSelectPopup();
        onCancel?.Invoke();
    }
}
