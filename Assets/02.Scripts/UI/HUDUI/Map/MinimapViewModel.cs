using UnityEngine;
using System;
using System.Collections.Generic;

public class MinimapViewModel : ViewModelBase
{
    private Rect _worldBounds;
    public Rect WorldBounds
    {
        get
        {
            return _worldBounds;
        }
        set
        {
            if (_worldBounds != value)
            {
                _worldBounds = value;
                OnPropertyChanged(nameof(WorldBounds));
            }
        }
    }

    private Vector2 _basePosition;
    public Vector2 BasePosition
    {
        get
        {
            return _basePosition;
        }
        set
        {
            if (_basePosition != value)
            {
                _basePosition = value;
                OnPropertyChanged(nameof(BasePosition));
            }
        }
    }

    private Vector2 _trainPosition;
    public Vector2 TrainPosition
    {
        get
        {
            return _trainPosition;
        }
        set
        {
            if (_trainPosition != value)
            {
                _trainPosition = value;
                OnPropertyChanged(nameof(TrainPosition));
            }
        }
    }

    private readonly List<Vector2> _stationPositions = new List<Vector2>();
    public IReadOnlyList<Vector2> StationPositions => _stationPositions;

    public event Action OnStationPositionsChanged;

    public void SetStationPositions(IEnumerable<Vector2> normalizedPositions)
    {
        _stationPositions.Clear();

        foreach (Vector2 position in normalizedPositions)
        {
            _stationPositions.Add(position);
        }

        OnStationPositionsChanged?.Invoke();
    }

    public Vector2 NormalizeWorldPosition(Vector3 worldPosition)
    {
        float normalizedX = Mathf.InverseLerp(_worldBounds.xMin, _worldBounds.xMax, worldPosition.x);
        float normalizedZ = Mathf.InverseLerp(_worldBounds.yMin, _worldBounds.yMax, worldPosition.z);

        return new Vector2(normalizedX, normalizedZ);
    }
}
