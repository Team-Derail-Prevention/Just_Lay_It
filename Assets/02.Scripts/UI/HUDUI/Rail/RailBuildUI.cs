using UnityEngine;
using TMPro;

public class RailBuildUI : UIBase
{
    [Header("제작 슬롯")]
    [SerializeField] private RailCraftSlotUI CraftSlot_Straight;
    [SerializeField] private RailCraftSlotUI CraftSlot_Curve;

    [Header("설치 슬롯")]
    [SerializeField] private RailPlaceSlotUI PlaceSlot_Straight;

    [Header("모래폭풍 경고")]
    [SerializeField] private GameObject Warning_Box;
    [SerializeField] private TextMeshProUGUI Text_Time;

    private RailBuildViewModel _vm;
    private bool _isSandstormCountingDown;
    private float _sandstormRemainingTime;

    private void OnEnable()
    {
        _vm = NetworkRailService.Instance.GetLocalRailBuildViewModel();

        if (CraftSlot_Straight != null)
        {
            CraftSlot_Straight.InitSlot(_vm.GetSlot(RailType.Straight), OnClick_Craft);
        }

        if (CraftSlot_Curve != null)
        {
            CraftSlot_Curve.InitSlot(_vm.GetSlot(RailType.Corner), OnClick_Craft);
        }

        if (PlaceSlot_Straight != null)
        {
            PlaceSlot_Straight.InitSlot(_vm.GetSlot(RailType.Straight), OnClick_Place);
        }

        if (Warning_Box != null)
        {
            Warning_Box.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountSandStorm += HandleSandstormStarted;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountSandStorm -= HandleSandstormStarted;
        }

        _isSandstormCountingDown = false;
    }

    private void Update()
    {
        if (_isSandstormCountingDown == false)
        {
            return;
        }

        _sandstormRemainingTime -= Time.deltaTime;

        if (_sandstormRemainingTime <= 0f)
        {
            _sandstormRemainingTime = 0f;
            _isSandstormCountingDown = false;

            if (Warning_Box != null)
            {
                Warning_Box.SetActive(false);
            }
        }

        UpdateCountdownText();
    }

    private void HandleSandstormStarted(float durationSeconds)
    {
        _sandstormRemainingTime = durationSeconds;
        _isSandstormCountingDown = true;

        if (Warning_Box != null)
        {
            Warning_Box.SetActive(true);
        }

        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        if (Text_Time == null)
        {
            return;
        }

        int displaySeconds = Mathf.CeilToInt(_sandstormRemainingTime);
        Text_Time.text = displaySeconds.ToString();
    }

    private void OnClick_Craft(RailType railType)
    {
        NetworkRailService.Instance.RequestCraft(railType);
    }

    private void OnClick_Place(RailType railType)
    {
        NetworkRailService.Instance.RequestStartPlacement(railType);
    }
}
