using UnityEngine;
using System.Collections.Generic;

public class GachaViewModel : ViewModelBase
{
    public const int CARD_SLOT_COUNT = 3;

    private readonly List<GachaCardState> _cardList = new List<GachaCardState>();
    public IReadOnlyList<GachaCardState> CardList
    {
        get
        {
            return _cardList;
        }
    }

    private int _rerollCountCurrent;
    public int RerollCountCurrent
    {
        get
        {
            return _rerollCountCurrent;
        }
        set
        {
            _rerollCountCurrent = value;
            OnPropertyChanged(nameof(RerollCountCurrent));
        }
    }

    public int RerollCountMax { get; private set; }

    public GachaViewModel()
    {
        for (int i = 0; i < CARD_SLOT_COUNT; i++)
        {
            var cardState = new GachaCardState();
            cardState.SlotIndex = i;
            _cardList.Add(cardState);
        }
    }

    public GachaCardState GetCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _cardList.Count)
        {
            return null;
        }

        return _cardList[slotIndex];
    }

    public void SetRerollCount(int current, int max)
    {
        RerollCountMax = max;
        RerollCountCurrent = current;
    }
}
