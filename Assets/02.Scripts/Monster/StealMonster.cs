using UnityEngine;

public class StealMonster : DebuffMonsterBase
{
    public override void ApplyDebuff()
    {
        Debug.Log($"[StealMonster] 기차에 접근하여 자재를 {(int)_debuffPower}만큼 훔쳤습니다");
    }

}
