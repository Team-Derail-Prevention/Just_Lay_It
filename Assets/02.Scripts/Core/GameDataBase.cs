using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializationWrapper<T>
{
    public List<T> items;
}

[System.Serializable]
public class GameDataBase : MonoBehaviour
{
    public string Id;

}
