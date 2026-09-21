using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class MonsterDeathEffect : MonoBehaviour
{
    private ParticleSystem[] _particles;
    private CancellationTokenSource _despawnCts;

    private void Awake()
    {
        _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        _despawnCts?.Cancel();
        _despawnCts?.Dispose();
        _despawnCts = new CancellationTokenSource();

        foreach (var ps in _particles)
        {
            if (ps == null) continue;

            if (!ps.gameObject.activeSelf)
            {
                ps.gameObject.SetActive(true);
            }

            ps.Clear(true);
            ps.Play(true);
        }

        AutoDespawnAsync(_despawnCts.Token).Forget();
    }

    private void OnDisable()
    {
        _despawnCts?.Cancel();
        _despawnCts?.Dispose();
        _despawnCts = null;
    }
    private async UniTaskVoid AutoDespawnAsync(CancellationToken token)
    {
        try
        {
            await UniTask.WaitUntil(AllParticlesFinished, cancellationToken: token);

            if (PoolManager.Instance != null)
                PoolManager.Instance.DespawnToPool(gameObject);
            else
                Destroy(gameObject);
        }
        catch (System.OperationCanceledException)
        { }
    }

    private bool AllParticlesFinished()
    {
        foreach (var ps in _particles)
        {
            if (ps != null && ps.IsAlive(true))
                return false;
        }
        return true;
    }
}
