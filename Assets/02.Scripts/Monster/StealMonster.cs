using UnityEngine;

public class StealMonster : DebuffMonsterBase
{
    public override void ApplyDebuff()
    {
        Debug.Log($"[StealMonster] 기차에 접근하여 자재를 {(int)_debuffPower}만큼 훔쳤습니다");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Train") || other.GetComponent<Train>() != null)
        {
            TriggerDebuffEvent();

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.DespawnToPool(gameObject);
            }
        }
    }
}
