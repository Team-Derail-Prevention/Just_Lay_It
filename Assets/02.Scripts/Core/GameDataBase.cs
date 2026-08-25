using System;
using System.Collections.Generic;

[Serializable]
public class SerializationWrapper<T>
{
    public List<T> items;
}

[Serializable]
public class GameDataBase
{
    public string Id;
}

[Serializable]
public class MapData : GameDataBase
{
    public string Type;
    public int GuestCount;
    public int Gold;
    public string AddressablePath;
}

public static class MapTypeConst
{
    public const string CentralTerminal = "CentralTerminal";
    public const string Station = "Station";
    public const string Normal = "Normal";
}

[Serializable]
public class MaterialObjectData : GameDataBase
{
    public string Type;
    public int amount;
    public string AddressablePath;
}

[Serializable]
public class TrainData : GameDataBase
{
    public string TrainName;
    public string TrainType;
    public string Description;
    public float MoveSpeed;
    public float RotateSpeed;
    public int MaxCargo;
    public int MaxHp;
    public int Defense;
    public int MaxWeaponMount;
    public int MaxCrew;
    public string PrefabPath;
}

[Serializable]
public class WeaponData : GameDataBase
{
    public string WeaponName;
    public int Atk;
    public float FireRate;
    public int MagazineSize;
    public float ReloadTime;
    public string UseFireSound;
    public string UseReloadSound;
}

public static class TrainTypeConst
{
    public const string Head = "Head";
    public const string Standard = "Standard";
    public const string Cargo = "Cargo";
}

[Serializable]
public class DroneUpgradeData : GameDataBase
{
    public string Name;
    public string Description;
    public string TargetStatType;
    public string Operation;
    public float Value;
    public int MaxLevel;
    public int GoldCost;
    public int GoldCostPerLevel;
    public string IconPath;
}

public static class DroneUpgradeIdConst
{
    public const string WorkSpeed = "DRONE_WORK_SPEED";
    public const string MoveSpeed = "DRONE_MOVE_SPEED";
    public const string YieldBonus = "DRONE_YIELD_BONUS";
}

public static class UpgradeOperationConst
{
    public const string Add = "Add";
    public const string AddPercent = "AddPercent";
}

public static class DroneStatTypeConst
{
    public const string WorkSpeed = "WorkSpeed";
    public const string MoveSpeed = "MoveSpeed";
    public const string YieldBonus = "YieldBonus";
}
