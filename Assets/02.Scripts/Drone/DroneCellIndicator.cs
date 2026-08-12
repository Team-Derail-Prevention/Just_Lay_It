using UnityEngine;

public class DroneCellIndicator : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DroneClickCommand _command;
    [SerializeField] private GridCursor _cursor;
    [SerializeField] private DroneStateMachine _stateMachine;
    [SerializeField] private Renderer _indicator;

    [Header("표시")]
    [SerializeField] private Color _allowedColor = Color.green;
    [SerializeField] private Color _blockedColor = Color.red;
    [SerializeField] private float _heightOffset = 0.02f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();

        SetVisible(false);
    }

    private void Update()
    {
        if (_command == null || _cursor == null || _stateMachine == null || _indicator == null)
        {
            SetVisible(false);

            return;
        }

        if (_command.IsOrderMode == false)
        {
            SetVisible(false);

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

        if (_stateMachine.CanAssign(cell))
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
