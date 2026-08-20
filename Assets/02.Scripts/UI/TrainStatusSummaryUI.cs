using TMPro;
using UnityEngine;

public class TrainStatusSummaryUI : UIBase
{
    [Header("전투")]
    [SerializeField] private TextMeshProUGUI Text_Hp;
    [SerializeField] private TextMeshProUGUI Text_Attack;
    [SerializeField] private TextMeshProUGUI Text_Defense;

    [Header("적재")]
    [SerializeField] private TextMeshProUGUI Text_Amount;
    [SerializeField] private TextMeshProUGUI Text_Resource;
    [SerializeField] private TextMeshProUGUI Text_Boarding;
    [SerializeField] private TextMeshProUGUI Text_Installation;

    [Header("드론")]
    [SerializeField] private TextMeshProUGUI Text_CollectSpeed;
    [SerializeField] private TextMeshProUGUI Text_CollectEfficiency;
    [SerializeField] private TextMeshProUGUI Text_ActionSpeed;

    private void OnEnable()
    {

    }
}
