using UnityEngine;

public class RailActivator : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private int _activationRailIndex = 3;

    private Train _headTrain;
    private Renderer[] _renderers;
    private bool _isActivated = false;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        if (_isActivated || _headTrain == null) return;

        if (_headTrain._targetIndex >= _activationRailIndex)
        {
            ActivateVisuals();
        }
    }
    public void InitActivator(Train headTrain, int targetIndex)
    {
        _headTrain = headTrain;
        _activationRailIndex = targetIndex;
        _isActivated = false;
        SetVisualVisible(false);
    }

    private void ActivateVisuals()
    {
        _isActivated = true;
        SetVisualVisible(true);
        Debug.Log($"[RailIndexActivator] {_activationRailIndex}번째 레일 도달! {gameObject.name} 활성화 완료.");
    }

    private void SetVisualVisible(bool isVisible)
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].enabled = isVisible;
            }
        }
    }
}
