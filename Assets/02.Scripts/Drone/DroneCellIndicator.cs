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

        if (_cursor.TryGetTarget(out MaterialObject target) == false)
        {
            SetVisible(false);

            return;
        }

        SetVisible(true);
        PlaceAt(target);
        Colorize(target);
    }

    private void PlaceAt(MaterialObject target)
    {
        Vector3 position = target.transform.position;

        position.y += _heightOffset;

        _indicator.transform.position = position;
        _indicator.transform.localScale = new Vector3(_size, _size, 1f);
    }

    private void Colorize(MaterialObject target)
    {
        Color color;

        if (_stateMachine.CanAssign(target))
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
