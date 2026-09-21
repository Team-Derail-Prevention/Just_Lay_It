using Cysharp.Threading.Tasks;
using UnityEngine;

public class MonsterDeathEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particle;

    private ParticleSystem[] _particles;

    private void Awake()
    {
        // 최하위 자식에 있는 모든 파티클 가져옴
        _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        if (_particles != null)
        {
            foreach (var ps in _particles)
            {
                ps.Clear(true); //  이전에 남아있던 잔상과 내부 타이머를 완전히 리셋
                ps.Play(true);  //  파티클 다시 재생 시작
            }
        }
    }

    //기존
    //private void OnEnable()
    //{
    //    if (_particle == null)
    //        _particle = GetComponent<ParticleSystem>();

    //    if (_particle != null)
    //    {
    //        _particle.Clear();
    //        _particle.Play();

    //        // 파티클 재생 완료 후 자동 반납
    //        AutoDespawnAsync().Forget();
    //    }
    //}

    //private async UniTaskVoid AutoDespawnAsync()
    //{
    //    // 파티클이 멈출 때까지 대기
    //    await UniTask.WaitUntil(() => _particle != null && !_particle.isPlaying, cancellationToken: this.GetCancellationTokenOnDestroy());

    //    // 이펙트 자가 반납
    //    if (PoolManager.Instance != null)
    //    {
    //        PoolManager.Instance.DespawnToPool(gameObject);
    //    }
    //    else
    //    {
    //        Destroy(gameObject);
    //    }
    //}
}
