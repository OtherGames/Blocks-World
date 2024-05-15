using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TriggerSystem : MonoBehaviour
{
    /// <summary>
    /// 1 GO - Объект вошедший в триггер #
    /// 2 GO - Сам объект триггера
    /// </summary>
    public static Action<GameObject, GameObject> onTriggerEnter;
    /// <summary>
    /// 1 GO - Объект покинувший триггер #
    /// 2 GO - Сам объект триггера
    /// </summary>
    public static Action<GameObject, GameObject> onTriggerExit;


    private void OnTriggerEnter(Collider other)
    {
        onTriggerEnter?.Invoke(other.gameObject, gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        onTriggerExit?.Invoke(other.gameObject, gameObject);
    }
}
