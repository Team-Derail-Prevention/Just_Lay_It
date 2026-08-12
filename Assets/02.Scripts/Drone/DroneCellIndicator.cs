using UnityEngine;
using UnityEngine.InputSystem;

public class DroneCellIndicator : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GridCursor _cursor;
    [SerializeField] private Drone _drone;
    [SerializeField] private Renderer _indicator;

    [Header("입력")]
    [SerializeField] private Key _toggleKey = Key.B;

    [Header("표시")]
    [SerializeField] private Color _allowedColor = Color.green;
    [SerializeField] private Color _blockedColor = Color.red;
    [SerializeField] private float _heightOffset = 0.02f;

    public bool IsOn { get { return _isOn; } }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private bool _isOn;
    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();

        SetVisible(false);
    }

    private void Update()
    {
        HandleToggle();

        if (_isOn == false)
        {
            return;
        }

        if (_cursor == null || _drone == null || _indicator == null)
        {
            return;
        }

        if (_cursor.TryGetCell(out CellPos cell) == false)
        {
            SetVisible(false);

            return;
        }

        SetVisible(true);
        PlaceAt(cell);
        Colorize(cell);
    }

    private void HandleToggle()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[_toggleKey].wasPressedThisFrame == false)
        {
            return;
        }

        _isOn = !_isOn;

        SetVisible(_isOn);
    }

    private void PlaceAt(CellPos cell)
    {
        Vector3 center = _cursor.GetCellCenter(cell);

        center.y += _heightOffset;

        float size = _cursor.CellSize;

        _indicator.transform.position = center;
        _indicator.transform.localScale = new Vector3(size, size, 1f);
    }

    private void Colorize(CellPos cell)
    {
        Color color;

        if (_drone.CanMoveTo(cell))
        {
            color = _allowedColor;
        }
        else
        {
            color = _blockedColor;
        }

        _indicator.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseColorId, color);
        _indicator.SetPropertyBlock(_propertyBlock);
    }

    private void SetVisible(bool visible)
    {
        if (_indicator == null)
        {
            return;
        }

        _indicator.enabled = visible;
    }
}
