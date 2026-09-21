using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CursorManager : SingletonBase<CursorManager>
{
    [SerializeField] private RectTransform _cursorRect;
    [SerializeField] private Image _cursorImage;

    [Header("커서 이미지")]
    [SerializeField] private Sprite _idleSprite;
    [SerializeField] private Sprite _clickDownSprite;

    private bool _isClickDown;

    protected override void Init()
    {
        base.Init();

        if (Instance != this)
        {
            return;
        }

        DontDestroyOnLoad(gameObject);

        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        Cursor.visible = true;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || _cursorRect == null)
        {
            return;
        }

        _cursorRect.position = mouse.position.ReadValue();

        SetClickDown(mouse.leftButton.isPressed);
    }

    private void SetClickDown(bool isClickDown)
    {
        if (_isClickDown == isClickDown || _cursorImage == null)
        {
            return;
        }

        _isClickDown = isClickDown;

        _cursorImage.sprite = isClickDown ? _clickDownSprite : _idleSprite;
    }
}
