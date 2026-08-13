using UnityEngine;
using UnityEngine.InputSystem;

public class StartUi : UIBase
{
    [SerializeField] private Animator _animator;
    [SerializeField] private string _showStateName = "TitleSequence_Show";

    private bool _isShowFinished;

    private void OnEnable()
    {
        _isShowFinished = false;
        PlayTitleSequence();
    }

    private void PlayTitleSequence()
    {
        if (_animator == null)
        {
            Debug.LogWarning($"{name} : Animator가 연결되어 있지 않습니다.");
            return;
        }

        _animator.Play(_showStateName, 0, 0f);
    }

    private void Update()
    {
        if (_isShowFinished == false)
        {
            CheckShowFinished();
            return;
        }

        CheckEnterInputToProceed();
    }

    private void CheckShowFinished()
    {
        if (_animator == null)
        {
            return;
        }

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        bool isPlayingShowState = stateInfo.IsName(_showStateName);
        bool isAnimationDone = (stateInfo.normalizedTime >= 1f);

        if (isPlayingShowState == true && isAnimationDone == true)
        {
            _isShowFinished = true;
            Debug.Log("[DEBUG] 연출 종료 감지됨, 이제부터 엔터 입력을 받습니다.");
        }
    }

    private void CheckEnterInputToProceed()
    {
        if (Keyboard.current == null)
        {
            Debug.LogWarning("[DEBUG] Keyboard.current가 null입니다. Active Input Handling 설정을 확인하세요.");
            return;
        }

        bool isEnterPressed = (Keyboard.current.enterKey.wasPressedThisFrame == true || Keyboard.current.numpadEnterKey.wasPressedThisFrame == true);
        if (isEnterPressed == true)
        {
            Debug.Log("[DEBUG] 엔터 입력 감지됨, OnClickStartConfirm 호출.");
            OnClickStartConfirm();
        }
    }

    public void OnClickStartConfirm()
    {
        Debug.Log("[DEBUG] OnClickStartConfirm 호출됨, CompleteStartUI 실행.");
        UIManager.Instance.CompleteStartUI();
    }
}
