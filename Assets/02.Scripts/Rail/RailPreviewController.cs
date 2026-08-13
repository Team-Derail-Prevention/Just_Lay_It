using UnityEngine;

public class RailPreviewController : MonoBehaviour
{
    [Header("Ghost Alpha")]
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private string _colorPropertyName = "_BaseColor";

    private int _currentRotationStep;
    private MaterialPropertyBlock _propertyBlock;

    public Quaternion CurrentRotation { get { return Quaternion.Euler(0f, _currentRotationStep * 90f, 0f); } }

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();

        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    public bool HandleRotationInput()
    {
        if (!Input.GetKeyDown(KeyCode.T))
        {
            return false;
        }

        _currentRotationStep = (_currentRotationStep + 1) % 4;
        return true;
    }

    public void Show(CubeInfo cubeInfo)
    {
        transform.SetPositionAndRotation(cubeInfo.Center, CurrentRotation);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetGhostAlpha(float alpha)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer targetRenderer = _renderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(_propertyBlock);
            Color currentColor = targetRenderer.sharedMaterial.GetColor(_colorPropertyName);
            Color ghostColor = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);

            _propertyBlock.SetColor(_colorPropertyName, ghostColor);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}