using System;
using System.Collections.Generic;
using UnityEngine;

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
