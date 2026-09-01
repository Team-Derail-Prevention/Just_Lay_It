using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private MonsterMove _monsterMove;

    private void Awake()
    {
        _monsterMove = GetComponentInParent<MonsterMove>();
    }

    public void ExecuteMeleeHit()
    {
        if (_monsterMove != null)
        {
            _monsterMove.ExecuteMeleeHit();
        }
    }
}
