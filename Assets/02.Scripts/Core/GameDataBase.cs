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