using UnityEngine;
using System;

[Serializable]
public class MonsterData : GameDataBase
{
    public string MonsterName;
    public int Hp;
    public int Atk;
    public float Speed;

    public string UseAttackSound;
    public string UseTakeDamageSound;
    public string UseDieSound;
}
