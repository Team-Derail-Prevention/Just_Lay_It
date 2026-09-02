using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HudTrainStatusUI : UIBase
{
    [Header("체력")]
    [SerializeField] private Image Image_HpFill;
    [SerializeField] private TextMeshProUGUI Text_Hp;

    [Header("체력 색상")]
    [SerializeField] private Color _colorHpHigh = Color.white;
    [SerializeField] private Color _colorHpMid = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color _colorHpLow = Color.red;

    [Header("체력 텍스트 대비 (아웃라인)")]
    [SerializeField] private Color _textOutlineColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float _textOutlineWidth = 0.2f;

    [Header("속도")]
    [SerializeField] private TextMeshProUGUI Text_Speed;

    [Header("게임 시간")]
    [SerializeField] private TextMeshProUGUI Text_Time;

    [Header("이동 거리")]
    [SerializeField] private TextMeshProUGUI Text_Distance;

    private void Awake()
    {
        InitHpTextOutline();
    }

    private void InitHpTextOutline()
    {
        if (Text_Hp == null)
        {
            return;
        }

        Text_Hp.color = Color.white;
        Text_Hp.outlineWidth = _textOutlineWidth;
        Text_Hp.outlineColor = _textOutlineColor;
    }

    private void OnEnable()
    {
        if (TrainStatusEventHub.Instance == null)
        {
            Debug.LogError("[HudTrainStatusUI] TrainStatusEventHub.Instance가 null입니다. 씬에 배치했는지 확인하세요.");
            return;
        }

        TrainStatusEventHub.Instance.OnHpChanged += SetHp;
        TrainStatusEventHub.Instance.OnSpeedChanged += SetSpeed;
        TrainStatusEventHub.Instance.OnDistanceChanged += SetDistance;
        TrainStatusEventHub.Instance.OnPlayTimeChanged += SetPlayTime;

        if (TrainManager.Instance != null && TrainManager.Instance.ActiveTrain != null)
        {
            Train activeTrain = TrainManager.Instance.ActiveTrain;
            SetHp(activeTrain.CurrentHp, activeTrain.MaxHp);
        }
    }

    private void OnDisable()
    {
        if (TrainStatusEventHub.Instance == null)
        {
            return;
        }

        TrainStatusEventHub.Instance.OnHpChanged -= SetHp;
        TrainStatusEventHub.Instance.OnSpeedChanged -= SetSpeed;
        TrainStatusEventHub.Instance.OnDistanceChanged -= SetDistance;
        TrainStatusEventHub.Instance.OnPlayTimeChanged -= SetPlayTime;
    }

    public void SetHp(float curHp, float maxHp)
    {
        float hpRatio = 0f;
        if (maxHp > 0f)
        {
            hpRatio = curHp / maxHp;
        }

        if (Image_HpFill != null)
        {
            Image_HpFill.fillAmount = hpRatio;
        }

        if (Text_Hp != null)
        {
            Text_Hp.text = $"{Mathf.RoundToInt(curHp)} / {Mathf.RoundToInt(maxHp)}";
        }

        UpdateHpColor(hpRatio);
    }

    private void UpdateHpColor(float hpRatio)
    {
        Color targetColor;
        if (hpRatio <= 0.25f)
        {
            targetColor = _colorHpLow;
        }
        else if (hpRatio <= 0.5f)
        {
            targetColor = _colorHpMid;
        }
        else
        {
            targetColor = _colorHpHigh;
        }

        if (Image_HpFill != null)
        {
            Image_HpFill.color = targetColor;
        }
    }

    public void SetSpeed(float speedKmh)
    {
        if (Text_Speed != null)
        {
            Text_Speed.text = $"{Mathf.RoundToInt(speedKmh)}Km";
        }
    }

    public void SetPlayTime(float totalSeconds)
    {
        if (Text_Time == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        Text_Time.text = $"{minutes:00}:{seconds:00}";
    }

    public void SetDistance(float distanceMeters)
    {
        if (Text_Distance != null)
        {
            Text_Distance.text = $"{Mathf.RoundToInt(distanceMeters)}M";
        }
    }
}
