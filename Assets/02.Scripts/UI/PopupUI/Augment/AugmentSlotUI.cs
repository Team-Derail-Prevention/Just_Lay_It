using UnityEngine;
using System.ComponentModel;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AugmentSlotUI : MonoBehaviour,IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image Image_Icon;
    [SerializeField] private GameObject GameObject_LockedOverlay;
    [SerializeField] private GameObject GameObject_Description;

    [SerializeField] private TMPro.TextMeshProUGUI Text_NameGrade;
    [SerializeField] private TMPro.TextMeshProUGUI Text_Atk;
    [SerializeField] private TMPro.TextMeshProUGUI Text_Range;
    [SerializeField] private TMPro.TextMeshProUGUI Text_FireRate;
    [SerializeField] private TMPro.TextMeshProUGUI Text_ReloadTime;
    [SerializeField] private TMPro.TextMeshProUGUI Text_MagazineSize;

    private AugmentSlotState _slotState;
    private AugmentSlotContainerViewModel _ownerContainer;
    private RectTransform _rectTransform;
    private Canvas _rootCanvas;
    private Vector2 _originalAnchoredPos;
    private Transform _originalParent;
    private bool _isDragging;

    private RectTransform _descriptionRectTransform;
    private Transform _descriptionOriginalParent;

    public int SlotIndex
    {
        get
        {
            return _slotState.SlotIndex;
        }
    }

    public AugmentSlotContainerViewModel OwnerContainer
    {
        get
        {
            return _ownerContainer;
        }
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();

        if (GameObject_Description != null)
        {
            _descriptionRectTransform = GameObject_Description.GetComponent<RectTransform>();
            _descriptionOriginalParent = GameObject_Description.transform.parent;
            GameObject_Description.SetActive(false);
        }
    }

    public void InitSlot(AugmentSlotState slotState, AugmentSlotContainerViewModel ownerContainer)
    {
        _slotState = slotState;
        _ownerContainer = ownerContainer;
        _slotState.PropertyChanged += OnPropertyChanged_View;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (_slotState != null)
        {
            _slotState.PropertyChanged -= OnPropertyChanged_View;
        }

        HideDescription();
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (GameObject_LockedOverlay != null)
        {
            GameObject_LockedOverlay.SetActive(_slotState.IsLocked);
        }

        bool isFilled = (_slotState.Augment != null);
        if (Image_Icon != null)
        {
            Image_Icon.gameObject.SetActive(isFilled);

            // 무기 증강 정해지면 ResourceManager로 로드 추후 수정
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_slotState.IsLocked == true || _slotState.Augment == null)
        {
            return;
        }

        _isDragging = true;
        _originalParent = transform.parent;
        _originalAnchoredPos = _rectTransform.anchoredPosition;

        transform.SetParent(_rootCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging == false)
        {
            return;
        }

        _rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDragging == false)
        {
            return;
        }

        _isDragging = false;

        if (transform.parent == _rootCanvas.transform)
        {
            ReturnToOriginalPosition();
        }
    }

    public void ReturnToOriginalPosition()
    {
        transform.SetParent(_originalParent, true);
        _rectTransform.anchoredPosition = _originalAnchoredPos;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedObj = eventData.pointerDrag;
        if (draggedObj == null)
        {
            return;
        }

        var draggedSlot = draggedObj.GetComponent<AugmentSlotUI>();
        if (draggedSlot == null || draggedSlot == this)
        {
            return;
        }

        NetworkAugmentService.Instance.RequestMove(draggedSlot.OwnerContainer, draggedSlot.SlotIndex, _ownerContainer, SlotIndex);
        draggedSlot.ReturnToOriginalPosition();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        if (_slotState.IsLocked == true || _slotState.Augment == null)
        {
            return;
        }

        UIManager.Instance.OpenExitConfirmPopup(ConfirmSell, null, "이 증강을 판매하시겠습니까?");
    }

    private void ConfirmSell()
    {
        NetworkAugmentService.Instance.RequestSell(_ownerContainer, SlotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_slotState.Augment == null || GameObject_Description == null)
        {
            return;
        }

        FillDescription(_slotState.Augment.AugmentDataId);
        ShowDescription();
    }

    private void FillDescription(string weaponDataId)
    {
        /*
        WeaponStatSnapshot stat = WeaponFire.GetCurrentStats(weaponDataId);
        if (stat == null)
        {
            return;
        }

        if (Text_NameGrade != null)
        {
            Text_NameGrade.text = $"{stat.WeaponName} ({stat.GradeName})";
        }

        if (Text_Atk != null)
        {
            Text_Atk.text = $"{stat.Atk}";
        }

        if (Text_Range != null)
        {
            Text_Range.text = $"{stat.Range}";
        }

        if (Text_FireRate != null)
        {
            Text_FireRate.text = $"{stat.FireRate}";
        }

        if (Text_ReloadTime != null)
        {
            Text_ReloadTime.text = $"{stat.ReloadTime}";
        }

        if (Text_MagazineSize != null)
        {
            Text_MagazineSize.text = $"{stat.MagazineSize}";
        } */
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideDescription();
    }

    private void ShowDescription()
    {
        GameObject_Description.transform.SetParent(_rootCanvas.transform, true);

        bool isInBottomHalf = (_rectTransform.position.y < Screen.height / 2f);
        float slotHeight = _rectTransform.rect.height;
        float descriptionHeight = _descriptionRectTransform != null ? _descriptionRectTransform.rect.height : 0f;
        float yOffset = isInBottomHalf ? (slotHeight + descriptionHeight) : -(slotHeight + descriptionHeight);

        Vector3 slotPosition = _rectTransform.position;
        GameObject_Description.transform.position = new Vector3(slotPosition.x, slotPosition.y + yOffset, slotPosition.z);

        GameObject_Description.SetActive(true);
    }

    private void HideDescription()
    {
        if (GameObject_Description == null)
        {
            return;
        }

        GameObject_Description.SetActive(false);
        GameObject_Description.transform.SetParent(_descriptionOriginalParent, true);
    }
}
