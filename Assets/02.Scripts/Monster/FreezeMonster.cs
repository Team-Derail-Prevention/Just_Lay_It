using UnityEngine;

public class FreezeMonster : DebuffMonsterBase
{
    public override void ApplyDebuff()
    {
        Debug.Log("얼음몬스터 디버프 적용");
    }
}