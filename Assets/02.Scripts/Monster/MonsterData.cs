using UnityEngine;
using System;

[Serializable]
public class MonsterData : GameDataBase
{
    public string MonsterName_Ko;
    public string MonsterName_En;
    public int Hp;
    public int Atk;
    public float Speed;
    public int DropGold;
    public string AttackType;
    public float DebuffDuration;
    public float DebuffPower;

    public string UseAttackSound;
    public string UseTakeDamageSound;
    public string UseDieSound;

    public string ProjectileColor;

    public string Description_Ko;
    public string Description_En;
    public string UseIconName;
    public int NameFontSize_Ko;
    public int NameFontSize_En;
    public int DescriptionFontSize_Ko;
    public int DescriptionFontSize_En;
}
