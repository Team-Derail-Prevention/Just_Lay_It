using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

public class GachaCardUI : MonoBehaviour
{
    [Header("BK")]
    [SerializeField] private GameObject GameObject_Frame;
    [SerializeField] private GameObject GameObject_TopBox;

    [Header("아이콘")]
    [SerializeField] private RectTransform Transform_IconRollContainer;
    [SerializeField] private Image[] Image_IconRollSlots;
    [SerializeField] private float IconSlotHeight = 100f;

    [Header("슬롯머신 연출")]
    [SerializeField] private int MinSpinTicks = 13;
    [SerializeField] private int MaxSpinTicks = 26;
    [SerializeField] private float TickDurationMin = 0.05f;
    [SerializeField] private float TickDurationMax = 0.28f;

    [Header("이름/등급")]
    [SerializeField] private TextMeshProUGUI Text_Name;
    [SerializeField] private TextMeshProUGUI Text_Grade;

    [Header("스탯")]
    [SerializeField] private GameObject GameObject_StatGroup;
    [SerializeField] private TextMeshProUGUI Text_Stat_Attack;
    [SerializeField] private TextMeshProUGUI Text_Stat_Range;
    [SerializeField] private TextMeshProUGUI Text_Stat_AttackSpeed;
    [SerializeField] private TextMeshProUGUI Text_Stat_ReloadSpeed;
    [SerializeField] private TextMeshProUGUI Text_Stat_MagazineCount;

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
        var cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            _isSpinning = true;
            SetRevealVisible(false);

            if (Transform_IconRollContainer != null)
            {
                Transform_IconRollContainer.anchoredPosition = Vector2.zero;
            }

            if (startDelay > 0f)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(startDelay), ignoreTimeScale: true, cancellationToken: cancellationToken);
            }

            SoundManager.Instance?.PlaySFX(SfxAddress.Ui.GachaSpin);

            await PlaySlotMachineReelAsync(cancellationToken);

            SoundManager.Instance?.PlaySFX(SfxAddress.Ui.WeaponPick);

            ApplyCardData();
            SetRevealVisible(true);
        }
        catch (System.OperationCanceledException)
        {
            
        }
        catch (System.Exception ex)
        {
            
            Debug.LogError($"[GachaCardUI] 카드 연출 중 예외가 발생했습니다: {ex}");
            SetRevealVisible(true);
        }
        finally
        {
            _isSpinning = false;
            RebindCardButtonEvents();
        }
    }

    private async UniTask PlaySlotMachineReelAsync(System.Threading.CancellationToken cancellationToken)
    {
        if (Transform_IconRollContainer == null || Image_IconRollSlots == null || Image_IconRollSlots.Length == 0 || _cardState == null)
        {
            return;
        }

        var pool = NetworkGachaService.Instance.GetGachaWeaponPool();
        if (pool == null || pool.Count == 0)
        {
            return;
        }

        var orderedPool = new System.Collections.Generic.List<WeaponData>(pool);
        orderedPool.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

        int winningIndex = orderedPool.FindIndex(data => data.Id == _cardState.WeaponDataId);
        if (winningIndex < 0)
        {
            winningIndex = 0;
        }

        var sprites = new Sprite[orderedPool.Count];
        for (int i = 0; i < orderedPool.Count; i++)
        {
            sprites[i] = await ResourceManager.Instance.LoadAsset<Sprite>(orderedPool[i].IconPath);
        }

        int totalTicks = UnityEngine.Random.Range(MinSpinTicks, MaxSpinTicks + 1);

        Sprite GetSpriteAtStep(int step)
        {
            int index = ModIndex(winningIndex - (totalTicks - step), orderedPool.Count);
            return sprites[index];
        }

        var slotList = new System.Collections.Generic.List<Image>(Image_IconRollSlots);
        for (int i = 0; i < slotList.Count; i++)
        {
            slotList[i].transform.SetSiblingIndex(i);
            slotList[i].sprite = GetSpriteAtStep(i);
        }

        Transform_IconRollContainer.anchoredPosition = Vector2.zero;

        for (int tick = 1; tick <= totalTicks; tick++)
        {
            float ratio = (float)tick / totalTicks;
            float eased = ratio * ratio; 
            float tickDuration = Mathf.Lerp(TickDurationMin, TickDurationMax, eased);

            await MoveContainerOneSlotAsync(tickDuration, cancellationToken);

            if (Transform_IconRollContainer == null)
            {
                return;
            }

            Image recycled = slotList[0];
            slotList.RemoveAt(0);
            slotList.Add(recycled);
            recycled.transform.SetAsLastSibling();
            recycled.sprite = GetSpriteAtStep(tick + slotList.Count - 1);

            Transform_IconRollContainer.anchoredPosition = Vector2.zero;
        }
    }

    private async UniTask MoveContainerOneSlotAsync(float duration, System.Threading.CancellationToken cancellationToken)
    {
        float elapsed = 0f;
        Vector2 startPos = Transform_IconRollContainer.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, -IconSlotHeight);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Transform_IconRollContainer.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            if (Transform_IconRollContainer == null)
            {
                return;
            }
        }

        Transform_IconRollContainer.anchoredPosition = endPos;
    }

    private int ModIndex(int value, int mod)
    {
        int r = value % mod;
        return r < 0 ? r + mod : r;
    }

    private void RebindCardButtonEvents()
    {
        if (UIButton_RerollSingle != null)
        {
            UIButton_RerollSingle.UnBindAllOnClickButtonEvent();
            UIButton_RerollSingle.BindOnClickButtonEvent(OnClick_Reroll);
        }

        if (UIButton_Select != null)
        {
            UIButton_Select.UnBindAllOnClickButtonEvent();
            UIButton_Select.BindOnClickButtonEvent(OnClick_Select);
        }

    }

    public void SetRerollInteractable(bool isInteractable)
    {
        if (UIButton_RerollSingle != null)
        {
            UIButton_RerollSingle.SetInteractable(isInteractable);
        }
    }

    private void ApplyCardData()
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
        ApplyStatRow(Text_Stat_ReloadSpeed, 3);
        ApplyStatRow(Text_Stat_MagazineCount, 4);

        if (Text_Description != null)
        {
            Text_Description.text = _cardState.Description;
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
