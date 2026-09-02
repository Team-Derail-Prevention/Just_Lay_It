using UnityEngine;
using System.ComponentModel;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

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
    [SerializeField] private TMPro.TextMeshProUGUI Text_Price;

    private AugmentSlotState _slotState;
    private AugmentSlotContainerViewModel _ownerContainer;
    private AugmentSlotViewModel _subscribedAugment;
    private RectTransform _rectTransform;
    private Canvas _rootCanvas;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalAnchoredPos;
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private bool _isDragging;
    private GameObject _dragGhost;
    private RectTransform _dragGhostRect;


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

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>(); 
        }

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

        SubscribeAugment(_slotState.Augment);

        RefreshIcon();
        RefreshLockedOverlay();
        RefreshDescriptionIfVisible();
    }

    private void OnEnable()
    {
        if (_slotState == null)
        {
            return;
        }

        _slotState.PropertyChanged -= OnPropertyChanged_View; 
        _slotState.PropertyChanged += OnPropertyChanged_View;

        SubscribeAugment(_slotState.Augment);

        RefreshIcon();
        RefreshLockedOverlay();
        RefreshDescriptionIfVisible();
    }


    private void OnDisable()
    {
        if (_slotState != null)
        {
            _slotState.PropertyChanged -= OnPropertyChanged_View;
        }

        UnsubscribeAugment();
        HideDescription();
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AugmentSlotState.Augment))
        {
            UnsubscribeAugment();
            SubscribeAugment(_slotState.Augment);
            RefreshIcon();
        }

        RefreshLockedOverlay();
        RefreshDescriptionIfVisible();
    }

    private void RefreshLockedOverlay()
    {
        if (GameObject_LockedOverlay != null)
        {
            GameObject_LockedOverlay.SetActive(_slotState.IsLocked);
        }
    }

    private void RefreshIcon()
    {
        bool isFilled = (_slotState.Augment != null);

        if (Image_Icon != null)
        {
            Image_Icon.gameObject.SetActive(isFilled);
            Debug.Log($"[AugmentSlotUI] SetActive({isFilled}) 호출됨\n{System.Environment.StackTrace}");

            if (isFilled == false)
            {
                Image_Icon.sprite = null;
            }
            else
            {
                LoadIconAsync(_slotState.Augment.IconPath).Forget();
            }
        }
    }

    private void TryReloadMissingIcon()
    {
        if (_slotState == null)
        {
            return;
        }

        if (_slotState.Augment == null)
        {
            return;
        }

        if (Image_Icon == null)
        {
            return;
        }

        if (Image_Icon.sprite == null)
        {
            LoadIconAsync(_slotState.Augment.IconPath).Forget();
        }
    }

    private void RefreshDescriptionIfVisible()
    {
        bool isFilled = (_slotState.Augment != null);

        if (isFilled == true && GameObject_Description != null && GameObject_Description.activeSelf == true)
        {
            FillDescription(_slotState.Augment);
        }
    }

    private void SubscribeAugment(AugmentSlotViewModel augment)
    {
        if (augment == null)
        {
            return;
        }

        augment.PropertyChanged += OnAugmentPropertyChanged_View;
        _subscribedAugment = augment;
    }

    private void UnsubscribeAugment()
    {
        if (_subscribedAugment != null)
        {
            _subscribedAugment.PropertyChanged -= OnAugmentPropertyChanged_View;
            _subscribedAugment = null;
        }
    }

    private void OnAugmentPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        RefreshDescriptionIfVisible();
    }

    private async UniTaskVoid LoadIconAsync(string iconPath)
    {
        Sprite sprite = await ResourceManager.Instance.LoadAssetWithRetry<Sprite>(iconPath);
        if (Image_Icon != null && sprite != null)
        {
            Image_Icon.sprite = sprite;
            Image_Icon.gameObject.SetActive(true);
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

        HideDescription();
        _isDragging = true;
        _canvasGroup.alpha = 0.4f;

        SoundManager.Instance?.PlaySFX(SfxAddress.Ui.WeaponUnEquip);

        CreateDragGhost();
    }

    private void CreateDragGhost()
    {
        _dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        _dragGhost.transform.SetParent(_rootCanvas.transform, false);
        _dragGhost.transform.SetAsLastSibling();

        _dragGhostRect = _dragGhost.GetComponent<RectTransform>();
        _dragGhostRect.sizeDelta = _rectTransform.rect.size;
        _dragGhostRect.position = _rectTransform.position;

        Image ghostImage = _dragGhost.GetComponent<Image>();
        ghostImage.sprite = Image_Icon.sprite;
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false; 

        CanvasGroup ghostCanvasGroup = _dragGhost.GetComponent<CanvasGroup>();
        ghostCanvasGroup.blocksRaycasts = false;
        ghostCanvasGroup.alpha = 0.9f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging == false || _dragGhostRect == null)
        {
            return;
        }

        _dragGhostRect.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDragging == false) return;

        _isDragging = false;
        _canvasGroup.alpha = 1f;

        if (_dragGhost != null)
        {
            Destroy(_dragGhost);
            _dragGhost = null;
            _dragGhostRect = null;
        }
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

        HideDescription();
        if (_slotState.IsLocked == true)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "아직 열리지 않은 칸입니다.");
            return;
        }

        bool isMoved = NetworkAugmentService.Instance.RequestMove(draggedSlot.OwnerContainer, draggedSlot.SlotIndex, _ownerContainer, SlotIndex);
        if (isMoved == true)
        {
            SoundManager.Instance?.PlaySFX(SfxAddress.Ui.WeaponEquip);
        }
        else if (_slotState.Augment != null)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "이미 다른 무기가 장착된 칸입니다.");
        }
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

        FillDescription(_slotState.Augment);
        ShowDescription();
    }

    private void FillDescription(AugmentSlotViewModel augment)
    {
        if (Text_NameGrade != null)
        {
            Text_NameGrade.text = $"{augment.DisplayName} ({augment.GradeName})";
        }

        if (Text_Atk != null)
        {
            Text_Atk.text = augment.IsStatReady ? $"{augment.Atk}" : "-";
        }

        if (Text_Range != null)
        {
            Text_Range.text = augment.IsStatReady ? $"{augment.Range}" : "-";
        }

        if (Text_FireRate != null)
        {
            Text_FireRate.text = augment.IsStatReady ? $"{augment.FireRate}" : "-";
        }

        if (Text_ReloadTime != null)
        {
            Text_ReloadTime.text = augment.IsStatReady ? $"{augment.ReloadTime}" : "-";
        }

        if (Text_MagazineSize != null)
        {
            Text_MagazineSize.text = augment.IsStatReady ? $"{augment.MagazineSize}" : "-";
        }

        if (Text_Price != null)
        {
            Text_Price.text = $"{augment.Price}";
        }
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
        float yOffset = isInBottomHalf ? (slotHeight / 2f + descriptionHeight / 2f) : -(slotHeight / 2f + descriptionHeight / 2f);

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
