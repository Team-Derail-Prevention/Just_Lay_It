using UnityEngine;
public class RailPreviewController : MonoBehaviour
{
    [Header("Ghost Alpha")]
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private string _colorPropertyName = "_BaseColor";

    private int _currentRotationStep;
    private MaterialPropertyBlock _propertyBlock;
    public Quaternion CurrentRotation { get { return Quaternion.Euler(0f, _currentRotationStep * 90f, 0f); } }
    public int CurrentRotationStep { get { return _currentRotationStep; } }

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    // 수동 회전(RotateNext 등)은 제거됨. 회전은 항상 RailManager의
    // ApplyAutoConnect가 인접 레일을 보고 계산해서 SetRotationStep으로만 설정함.
    public void SetRotationStep(int step)
    {
        _currentRotationStep = ((step % 4) + 4) % 4;
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

    public void SetGhost()
    {
        float alpha = 0.4f;
        if (RailManager.Instance != null)
        {
            alpha = RailManager.Instance.GhostAlpha;
        }
        SetGhostAlpha(alpha);
    }

    public void SetSolid()
    {
        SetGhostAlpha(1f);
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