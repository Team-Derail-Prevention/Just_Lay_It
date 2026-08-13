using UnityEngine;
using Cysharp.Threading.Tasks;

public class SoundManager : SingletonBase<SoundManager>
{
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _bgmSource;

    public float SfxVolume => _sfxSource.volume;
    public float BgmVolume => _bgmSource.volume;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySFX(string assetPath)
    {
        LoadAndPlayAudioClip(_sfxSource, assetPath).Forget();
    }

    public void PlayBGM(string assetPath)
    {
        LoadAndPlayAudioClip(_bgmSource, assetPath, isLoop: true).Forget();
    }

    private static async UniTaskVoid LoadAndPlayAudioClip(AudioSource audioSource, string assetPath, bool isLoop = false)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning($"SoundManager : ResourceManager.Instance가 null이라 '{assetPath}'를 재생할 수 없습니다.");
            return;
        }

        AudioClip clip = await ResourceManager.Instance.LoadAsset<AudioClip>(assetPath);

        if (clip == null)
        {
            Debug.LogWarning($"SoundManager : '{assetPath}' 클립을 불러오지 못했습니다. 어드레서블 설정이 되어 있는지 확인해주세요.");
            return;
        }

        if (isLoop)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void StopSFX()
    {
        _sfxSource.Stop();
    }

    public void SetBgmVolume(float volume)
    {
        _bgmSource.volume = Mathf.Clamp01(volume);
    }

    public void SetSfxVolume(float volume)
    {
        _sfxSource.volume = Mathf.Clamp01(volume);
    }
}
