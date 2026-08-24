using UnityEngine;

public class CorrosionMonster : DebuffMonsterBase
{
    public override void ApplyDebuff()
    {
        Debug.Log($"부식몬스터 디버프 적용");
    }
}
