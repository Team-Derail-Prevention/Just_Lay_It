using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Uibuttonhovereffect : MonoBehaviour
{
    [SerializeField] private Image _targetImage;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.4f);
    [SerializeField] private float _proximityRadius = 120f;
    [SerializeField] private float _colorLerpSpeed = 8f;

    private RectTransform _rectTransform;
    private Canvas _parentCanvas;
    private Color _targetColor;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();
        _targetColor = _normalColor;

        if (_targetImage != null)
        {
            _targetImage.color = _normalColor;
        }
    }

    private void Update()
    {
        UpdateTargetColorByProximity();
        ApplyColorSmoothly();
    }

    private void UpdateTargetColorByProximity()
    {
        if (Mouse.current == null)
        {
            return;
        }

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        float distance = GetDistanceToRect(mouseScreenPos);

        bool isNear = (distance <= _proximityRadius);
        _targetColor = isNear ? _highlightColor : _normalColor;
    }

    private float GetDistanceToRect(Vector2 screenPoint)
    {
        bool isOverlayCanvas = (_parentCanvas == null || _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay);
        Camera eventCamera = isOverlayCanvas ? null : _parentCanvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        _rectTransform.GetWorldCorners(corners);

        Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);

        float clampedX = Mathf.Clamp(screenPoint.x, min.x, max.x);
        float clampedY = Mathf.Clamp(screenPoint.y, min.y, max.y);
        Vector2 closestPoint = new Vector2(clampedX, clampedY);

        return Vector2.Distance(screenPoint, closestPoint);
    }

    private void ApplyColorSmoothly()
    {
        if (_targetImage == null)
        {
            return;
        }

        _targetImage.color = Color.Lerp(_targetImage.color, _targetColor, Time.deltaTime * _colorLerpSpeed);
    }
}
