using UnityEngine;

public class ElectricMonster : DebuffMonsterBase
{
    public override void ApplyDebuff()
    {
        Debug.Log("감전몬스터 디버프 적용");
    }
}
