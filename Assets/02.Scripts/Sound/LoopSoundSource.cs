using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoopSoundSource : MonoBehaviour
{
    [SerializeField] private string _address;
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField, Range(0f, 1f)] private float _baseVolume = 1f;

    [Header("3D 감쇠")]
    [SerializeField, Min(0f)] private float _minDistance = 3f;
    [SerializeField, Min(0f)] private float _maxDistance = 60f;

    [Header("속도에 따른 피치")]
    [SerializeField] private bool _usePitchBySpeed;
    [SerializeField, Min(0.01f)] private float _speedForMaxPitch = 12f;
    [SerializeField, Min(0.01f)] private float _minPitch = 0.85f;
    [SerializeField, Min(0.01f)] private float _maxPitch = 1.15f;
    [SerializeField, Min(0f)] private float _pitchSmoothTime = 0.25f;

    [Header("멈추면 무음")]
    [SerializeField] private bool _stopWhenIdle;
    [SerializeField, Min(0f)] private float _idleSpeedThreshold = 0.1f;

    [Header("개체 편차")]
    [SerializeField] private bool _randomizeStartTime = true;
    [SerializeField, Range(0f, 0.3f)] private float _pitchOffsetRange = 0.06f;

    private AudioSource _source;
    private bool _isLoading;
    private Vector3 _lastPosition;
    private float _currentPitch = 1f;
    private float _currentSpeed;
    private float _pitchVelocity;
    private float _pitchOffset = 1f;

    public bool IsPlaying
    {
        get
        {
            if (_source == null)
            {
                return false;
            }

            return _source.isPlaying;
        }
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
        _currentPitch = 1f;
        _pitchOffset = 1f + Random.Range(-_pitchOffsetRange, _pitchOffsetRange);

        if (_playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        EnsureSource();

        if (_source.clip != null)
        {
            if (_source.isPlaying == false)
            {
                StartPlayback();
            }

            return;
        }

        LoadAndPlayAsync().Forget();
    }

    public void Stop()
    {
        if (_source == null)
        {
            return;
        }

        _source.Stop();
    }

    public void SetAddress(string address)
    {
        if (_address == address)
        {
            return;
        }

        _address = address;

        if (_source != null)
        {
            _source.Stop();
            _source.clip = null;
        }
    }

    private async UniTaskVoid LoadAndPlayAsync()
    {
        if (_isLoading)
        {
            return;
        }

        if (string.IsNullOrEmpty(_address))
        {
            return;
        }

        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning($"[LoopSoundSource] ResourceManager가 없어 '{_address}'를 재생하지 못했습니다. {gameObject.name}");

            return;
        }

        _isLoading = true;

        AudioClip clip = await ResourceManager.Instance.LoadAsset<AudioClip>(_address);

        _isLoading = false;

        if (clip == null)
        {
            Debug.LogWarning($"[LoopSoundSource] '{_address}' 클립을 불러오지 못했습니다. 어드레서블 등록을 확인해주세요. {gameObject.name}");

            return;
        }

        if (this == null || _source == null)
        {
            return;
        }

        _source.clip = clip;

        StartPlayback();
    }

    private void StartPlayback()
    {
        if (_randomizeStartTime && _source.clip != null)
        {
            _source.time = Random.Range(0f, _source.clip.length * 0.95f);
        }

        _source.Play();
    }

    private void EnsureSource()
    {
        if (_source != null)
        {
            return;
        }

        _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = true;
        _source.spatialBlend = 1f;
        _source.rolloffMode = AudioRolloffMode.Logarithmic;
        _source.minDistance = _minDistance;
        _source.maxDistance = _maxDistance;
        _source.volume = ResolveVolume();
    }

    private void Update()
    {
        if (_source == null)
        {
            return;
        }

        UpdateSpeed();

        _source.volume = ResolveVolume();

        ApplyPitch();

        ApplyIdleStop();
    }

    private void UpdateSpeed()
    {
        _currentSpeed = 0f;

        if (Time.deltaTime > 0f)
        {
            _currentSpeed = (transform.position - _lastPosition).magnitude / Time.deltaTime;
        }

        _lastPosition = transform.position;
    }

    private void ApplyIdleStop()
    {
        if (_stopWhenIdle == false)
        {
            return;
        }

        if (_currentSpeed > _idleSpeedThreshold)
        {
            _source.UnPause();

            return;
        }

        _source.Pause();
    }

    private void ApplyPitch()
    {
        if (_usePitchBySpeed == false)
        {
            _source.pitch = _pitchOffset;

            return;
        }

        float ratio = Mathf.Clamp01(_currentSpeed / _speedForMaxPitch);
        float targetPitch = Mathf.Lerp(_minPitch, _maxPitch, ratio);

        _currentPitch = Mathf.SmoothDamp(_currentPitch, targetPitch, ref _pitchVelocity, _pitchSmoothTime);

        _source.pitch = _currentPitch * _pitchOffset;
    }

    private float ResolveVolume()
    {
        if (SoundManager.Instance == null)
        {
            return _baseVolume;
        }

        return _baseVolume * SoundManager.Instance.SfxVolume;
    }
}
