using UnityEngine;

public class DroneCellIndicator : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DroneClickCommand _command;
    [SerializeField] private DroneTargetCursor _cursor;
    [SerializeField] private DroneStateMachine _stateMachine;
    [SerializeField] private Renderer _indicator;

    [Header("표시")]
    [SerializeField] private Color _allowedColor = Color.green;
    [SerializeField] private Color _blockedColor = Color.red;
    [SerializeField] private float _heightOffset = 0.02f;
    [SerializeField, Min(0.1f)] private float _size = 2f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();

        SetVisible(false);
    }

    private void Update()
    {
        if (_command == null || _cursor == null || _indicator == null)
        {
            SetVisible(false);

            return;
        }

        if (_command.IsOrderMode == false)
        {
            SetVisible(false);

            return;
        }

        if (_cursor.TryGetCell(out Vector3 cellCenter, out MaterialObject target) == false)
        {
            SetVisible(false);

            return;
        }

        SetVisible(true);
        PlaceAt(cellCenter);
        Colorize(target);
    }

    private void PlaceAt(Vector3 cellCenter)
    {
        Vector3 position = cellCenter;

        position.y += _heightOffset;

        _indicator.transform.position = position;
        _indicator.transform.localScale = new Vector3(_size, _size, 1f);
    }

    private void Colorize(MaterialObject target)
    {
        Color color;

        if (CanAssign(target))
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

    private bool CanAssign(MaterialObject target)
    {
        if (DroneManager.Instance != null)
        {
            return DroneManager.Instance.CanAssignMining(target);
        }

        if (_stateMachine != null)
        {
            return _stateMachine.CanAssign(target);
        }

        return false;
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
