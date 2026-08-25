using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

public class GachaCardUI : MonoBehaviour
{
    private const int ICON_STOP_INDEX = 3;

    [Header("BK")]
    [SerializeField] private GameObject GameObject_Frame;
    [SerializeField] private GameObject GameObject_TopBox;

    [Header("아이콘")]
    [SerializeField] private RectTransform Transform_IconRollContainer;
    [SerializeField] private Image Image_IconFinalResult;
    [SerializeField] private float IconSlotHeight = 100f;
    [SerializeField] private float SpinDuration = 1.2f;

    [Header("이름/등급")]
    [SerializeField] private TextMeshProUGUI Text_Name;
    [SerializeField] private TextMeshProUGUI Text_Grade;

    [Header("스탯")]
    [SerializeField] private GameObject GameObject_StatGroup;
    [SerializeField] private TextMeshProUGUI Text_Stat_Attack;
    [SerializeField] private TextMeshProUGUI Text_Stat_Range;
    [SerializeField] private TextMeshProUGUI Text_Stat_AttackSpeed;
    [SerializeField] private TextMeshProUGUI Text_Stat_DamageRange;

    [Header("상세 설명")]
    [SerializeField] private GameObject GameObject_DetailSection;
    [SerializeField] private TextMeshProUGUI Text_Description;

    [Header("버튼")]
    [SerializeField] private GameObject GameObject_RerollSingleRoot;
    [SerializeField] private UIButton UIButton_RerollSingle;
    [SerializeField] private GameObject GameObject_SelectRoot;
    [SerializeField] private UIButton UIButton_Select;

    private GachaCardState _cardState;
    private Action<int> _onClickSelect;
    private Action<int> _onClickReroll;
    private bool _isSpinning;

    private void OnEnable()
    {
        if (UIButton_RerollSingle != null)
        {
            UIButton_RerollSingle.BindOnClickButtonEvent(OnClick_Reroll);
        }

        if (UIButton_Select != null)
        {
            UIButton_Select.BindOnClickButtonEvent(OnClick_Select);
        }
    }

    private void OnDisable()
    {
        if (UIButton_RerollSingle != null)
        {
            UIButton_RerollSingle.UnBindOnClickButtonEvent(OnClick_Reroll);
        }

        if (UIButton_Select != null)
        {
            UIButton_Select.UnBindOnClickButtonEvent(OnClick_Select);
        }
    }

    public void InitSlot(GachaCardState cardState, Action<int> onClickSelect, Action<int> onClickReroll)
    {
        _cardState = cardState;
        _onClickSelect = onClickSelect;
        _onClickReroll = onClickReroll;

        if (GameObject_DetailSection != null)
        {
            GameObject_DetailSection.SetActive(false);
        }
    }

    // GachaUI가 뽑기/재굴림 직후 명시적으로 호출. 연출 시작 -> 데이터 반영 -> 공개.
    // startDelay를 슬롯마다 다르게 주면 멈추는 순서(0 -> 1 -> 2)를 만들 수 있음.
    public void PlayDrawAnimation(float startDelay = 0f)
    {
        if (_isSpinning == true)
        {
            return;
        }

        PlayDrawAnimationAsync(startDelay).Forget();
    }

    private async UniTaskVoid PlayDrawAnimationAsync(float startDelay)
    {
        _isSpinning = true;
        SetRevealVisible(false);

        if (Transform_IconRollContainer != null)
        {
            Transform_IconRollContainer.anchoredPosition = Vector2.zero;
        }

        if (startDelay > 0f)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(startDelay));
        }

        await PlayIconReelAsync();
        await ApplyCardDataAsync();

        SetRevealVisible(true);
        _isSpinning = false;
    }

    // GachaUI가 공용 재굴림 횟수 변경 시마다 호출. 카드는 자기 횟수를 안 들고 있고 그대로 반영만 함.
    public void SetRerollInteractable(bool isInteractable)
    {
        if (UIButton_RerollSingle != null)
        {
            UIButton_RerollSingle.SetInteractable(isInteractable);
        }
    }

    private async UniTask PlayIconReelAsync()
    {
        if (Transform_IconRollContainer == null)
        {
            return;
        }

        float startY = 0f;
        float endY = -(IconSlotHeight * ICON_STOP_INDEX);
        float elapsed = 0f;
        Vector2 pos = Transform_IconRollContainer.anchoredPosition;

        while (elapsed < SpinDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SpinDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out : 점점 느려지다 멈춤

            pos.y = Mathf.Lerp(startY, endY, eased);
            Transform_IconRollContainer.anchoredPosition = pos;

            await UniTask.Yield();
        }

        pos.y = endY;
        Transform_IconRollContainer.anchoredPosition = pos;
    }

    private async UniTask ApplyCardDataAsync()
    {
        if (_cardState == null)
        {
            return;
        }

        if (Text_Name != null)
        {
            Text_Name.text = _cardState.DisplayName;
        }

        if (Text_Grade != null)
        {
            Text_Grade.text = _cardState.GradeName;
        }

        ApplyStatRow(Text_Stat_Attack, 0);
        ApplyStatRow(Text_Stat_Range, 1);
        ApplyStatRow(Text_Stat_AttackSpeed, 2);
        ApplyStatRow(Text_Stat_DamageRange, 3);

        if (Text_Description != null)
        {
            Text_Description.text = _cardState.Description;
        }

        if (Image_IconFinalResult != null && string.IsNullOrEmpty(_cardState.IconPath) == false)
        {
            var sprite = await ResourceManager.Instance.LoadAsset<Sprite>(_cardState.IconPath);
            if (sprite != null && Image_IconFinalResult != null)
            {
                Image_IconFinalResult.sprite = sprite;
            }
        }
    }

    private void ApplyStatRow(TextMeshProUGUI targetText, int statIndex)
    {
        if (targetText == null || _cardState == null)
        {
            return;
        }

        if (statIndex < 0 || statIndex >= _cardState.StatTextList.Count)
        {
            targetText.text = string.Empty;
            return;
        }

        targetText.text = _cardState.StatTextList[statIndex];
    }

    private void SetRevealVisible(bool isVisible)
    {
        if (GameObject_Frame != null)
        {
            GameObject_Frame.SetActive(isVisible);
        }

        if (GameObject_TopBox != null)
        {
            GameObject_TopBox.SetActive(isVisible);
        }

        if (Text_Name != null)
        {
            Text_Name.gameObject.SetActive(isVisible);
        }

        if (Text_Grade != null)
        {
            Text_Grade.gameObject.SetActive(isVisible);
        }

        if (GameObject_StatGroup != null)
        {
            GameObject_StatGroup.SetActive(isVisible);
        }

        if (GameObject_DetailSection != null)
        {
            GameObject_DetailSection.SetActive(isVisible);
        }

        if (GameObject_RerollSingleRoot != null)
        {
            GameObject_RerollSingleRoot.SetActive(isVisible);
        }

        if (GameObject_SelectRoot != null)
        {
            GameObject_SelectRoot.SetActive(isVisible);
        }
    }

    private void OnClick_Select()
    {
        if (_isSpinning == true)
        {
            return;
        }

        _onClickSelect?.Invoke(_cardState.SlotIndex);
    }

    private void OnClick_Reroll()
    {
        if (_isSpinning == true)
        {
            return;
        }

        _onClickReroll?.Invoke(_cardState.SlotIndex);
    }
}
