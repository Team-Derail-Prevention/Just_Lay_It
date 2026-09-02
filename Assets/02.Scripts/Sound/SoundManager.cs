using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Enums;

public class SoundManager : SingletonBase<SoundManager>
{
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _bgmSource;

    [Header("상태별 BGM")]
    [SerializeField] private string _lobbyBgmAddress = "Bgm/OutGame";
    [SerializeField] private string _inGameBgmAddress = "Bgm/InGame";

    [Header("일시정지 먹먹함")]
    [SerializeField, Min(0f)] private float _muffleCutoff = 1000f;
    [SerializeField, Min(0f)] private float _normalCutoff = 22000f;

    [Header("자원 획득음 연사 제한")]
    [SerializeField, Min(0f)] private float _resourceSfxCooldown = 0.08f;

    [Header("3D 효과음")]
    [SerializeField, Min(1)] private int _sfxVoiceLimit = 8;
    [SerializeField, Min(0f)] private float _sfxMinDistance = 3f;
    [SerializeField, Min(0f)] private float _sfxMaxDistance = 60f;

    public float SfxVolume { get { return _sfxVolume; } }

    public float BgmVolume { get { return _bgmVolume; } }

    [Header("기본 볼륨")]
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.5f;

    private readonly List<AudioSource> _spatialSources = new List<AudioSource>();

    private readonly Dictionary<string, float> _lastSfxTimes = new Dictionary<string, float>();

    private string _currentBgmAddress;
    private GameState _previousGameState = GameState.Ready;
    private bool _hasPreviousGameState;

    private AudioLowPassFilter _bgmLowPassFilter;

    protected override void Init()
    {
        base.Init();

        if (Instance != this)
        {
            return;
        }

        EnsureSources();

        DontDestroyOnLoad(gameObject);

        PreloadSfxAsync().Forget();
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        LoadSavedVolume();

        MaterialObject.OnMaterialObjectCollected -= HandleMaterialCollected;
        MaterialObject.OnMaterialObjectCollected += HandleMaterialCollected;

        if (GameManager.Instance == null)
        {
            HandleGameStateChanged(GameState.Ready);

            return;
        }

        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        HandleGameStateChanged(GameManager.Instance.CurrentGameState);
    }

    private void OnDestroy()
    {
        MaterialObject.OnMaterialObjectCollected -= HandleMaterialCollected;

        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleMaterialCollected(MaterialObjectData data)
    {
        if (data == null)
        {
            return;
        }

        PlaySFXThrottled(SfxAddress.Resource.Collected, _resourceSfxCooldown);
    }

    private void HandleGameStateChanged(GameState gameState)
    {
        SetMuffled(gameState == GameState.EventPaused);

        PlayDepartureOnGameStart(gameState);

        string address = ResolveBgmAddress(gameState);

        if (string.IsNullOrEmpty(address))
        {
            return;
        }

        PlayBGM(address);
    }

    private void PlayDepartureOnGameStart(GameState gameState)
    {
        bool previousStateWasPlaying = _hasPreviousGameState && _previousGameState == GameState.Playing;

        _previousGameState = gameState;
        _hasPreviousGameState = true;

        if (gameState != GameState.Playing || previousStateWasPlaying)
        {
            return;
        }

        PlaySFX(SfxAddress.Train.Depart);
    }

    private string ResolveBgmAddress(GameState gameState)
    {
        if (gameState == GameState.Ready)
        {
            return _lobbyBgmAddress;
        }

        return _inGameBgmAddress;
    }

    private void SetMuffled(bool isMuffled)
    {
        if (_bgmLowPassFilter == null)
        {
            return;
        }

        if (isMuffled)
        {
            _bgmLowPassFilter.cutoffFrequency = _muffleCutoff;

            return;
        }

        _bgmLowPassFilter.cutoffFrequency = _normalCutoff;
    }

    private void LoadSavedVolume()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        SetBgmVolume(SaveManager.Instance.BgmVolume);
        SetSfxVolume(SaveManager.Instance.SfxVolume);
    }

    public async UniTask PreloadSfxAsync()
    {
        for (int i = 0; i < SfxAddress.All.Length; i++)
        {
            await LoadClipAsync(SfxAddress.All[i]);
        }
    }

    public void PlaySFX(string assetPath)
    {
        LoadAndPlayOneShot(_sfxSource, assetPath).Forget();
    }

    public void PlaySFXThrottled(string assetPath, float minInterval)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        float now = Time.unscaledTime;

        if (_lastSfxTimes.TryGetValue(assetPath, out float lastTime) && now - lastTime < minInterval)
        {
            return;
        }

        _lastSfxTimes[assetPath] = now;

        PlaySFX(assetPath);
    }

    public void PlaySFXAt(string assetPath, Vector3 position)
    {
        LoadAndPlaySpatial(assetPath, position).Forget();
    }

    public void PlayBGM(string assetPath)
    {
        if (_currentBgmAddress == assetPath && IsBgmPlaying())
        {
            return;
        }

        if (_currentBgmAddress == assetPath)
        {
            _currentBgmAddress = null;
        }

        ReleaseCurrentBgm();

        _currentBgmAddress = assetPath;

        LoadAndPlayBgm(assetPath).Forget();
    }

    private bool IsBgmPlaying()
    {
        if (_bgmSource == null)
        {
            return false;
        }

        return _bgmSource.isPlaying;
    }

    public void StopSFX()
    {
        if (_sfxSource != null)
        {
            _sfxSource.Stop();
        }

        for (int i = 0; i < _spatialSources.Count; i++)
        {
            _spatialSources[i].Stop();
        }
    }

    public void StopBGM()
    {
        if (_bgmSource == null)
        {
            return;
        }

        _bgmSource.Stop();
    }

    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);

        if (_sfxSource != null)
        {
            _sfxSource.volume = _sfxVolume;
        }

        for (int i = 0; i < _spatialSources.Count; i++)
        {
            _spatialSources[i].volume = _sfxVolume;
        }
    }

    public void SetBgmVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);

        if (_bgmSource == null)
        {
            return;
        }

        _bgmSource.volume = _bgmVolume;
    }

    private static async UniTaskVoid LoadAndPlayOneShot(AudioSource audioSource, string assetPath)
    {
        AudioClip clip = await LoadClipAsync(assetPath);

        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private async UniTaskVoid LoadAndPlaySpatial(string assetPath, Vector3 position)
    {
        AudioClip clip = await LoadClipAsync(assetPath);

        if (clip == null)
        {
            return;
        }

        AudioSource source = GetIdleSpatialSource();

        if (source == null)
        {
            return;
        }

        source.transform.position = position;
        source.clip = clip;
        source.loop = false;
        source.volume = _sfxVolume;
        source.Play();
    }

    private async UniTaskVoid LoadAndPlayBgm(string assetPath)
    {
        AudioClip clip = await LoadClipAsync(assetPath);

        if (clip == null || _bgmSource == null)
        {
            if (_currentBgmAddress == assetPath)
            {
                _currentBgmAddress = null;
            }

            return;
        }

        if (_currentBgmAddress != assetPath)
        {
            return;
        }

        _bgmSource.Stop();
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    private void ReleaseCurrentBgm()
    {
        if (string.IsNullOrEmpty(_currentBgmAddress))
        {
            return;
        }

        if (_bgmSource != null)
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.Release(_currentBgmAddress);
        }

        _currentBgmAddress = null;
    }

    private static async UniTask<AudioClip> LoadClipAsync(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning($"SoundManager : ResourceManager.Instance가 null이라 '{assetPath}'를 재생할 수 없습니다.");

            return null;
        }

        AudioClip clip = await ResourceManager.Instance.LoadAsset<AudioClip>(assetPath);

        if (clip == null)
        {
            Debug.LogWarning($"SoundManager : '{assetPath}' 클립을 불러오지 못했습니다. 어드레서블 설정이 되어 있는지 확인해주세요.");
        }

        return clip;
    }

    private void EnsureSources()
    {
        if (_sfxSource == null)
        {
            _sfxSource = CreateSource("SfxSource", isSpatial: false);
        }

        if (_bgmSource == null)
        {
            _bgmSource = CreateSource("BgmSource", isSpatial: false);
        }

        _bgmSource.priority = 0;

        EnsureLowPassFilter();

        ApplyVolume();
    }

    private void EnsureLowPassFilter()
    {
        if (_bgmSource == null)
        {
            return;
        }

        _bgmLowPassFilter = _bgmSource.GetComponent<AudioLowPassFilter>();

        if (_bgmLowPassFilter == null)
        {
            _bgmLowPassFilter = _bgmSource.gameObject.AddComponent<AudioLowPassFilter>();
        }

        _bgmLowPassFilter.cutoffFrequency = _normalCutoff;
    }

    private AudioSource CreateSource(string sourceName, bool isSpatial)
    {
        GameObject holder = new GameObject(sourceName);

        holder.transform.SetParent(transform, false);

        AudioSource source = holder.AddComponent<AudioSource>();

        source.playOnAwake = false;

        if (isSpatial == false)
        {
            source.spatialBlend = 0f;

            return source;
        }

        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = _sfxMinDistance;
        source.maxDistance = _sfxMaxDistance;

        return source;
    }

    private AudioSource GetIdleSpatialSource()
    {
        for (int i = 0; i < _spatialSources.Count; i++)
        {
            if (_spatialSources[i].isPlaying == false)
            {
                return _spatialSources[i];
            }
        }

        if (_spatialSources.Count >= _sfxVoiceLimit)
        {
            return null;
        }

        AudioSource source = CreateSource($"SfxSource3D_{_spatialSources.Count}", isSpatial: true);

        _spatialSources.Add(source);

        return source;
    }

    private void ApplyVolume()
    {
        SetBgmVolume(_bgmVolume);
        SetSfxVolume(_sfxVolume);
    }
}
