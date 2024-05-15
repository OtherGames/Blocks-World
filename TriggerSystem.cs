using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TriggerSystem : MonoBehaviour
{
    /// <summary>
    /// 1 GO - ������ �������� � ������� #
    /// 2 GO - ��� ������ ��������
    /// </summary>
    public static Action<GameObject, GameObject> onTriggerEnter;
    /// <summary>
    /// 1 GO - ������ ���������� ������� #
    /// 2 GO - ��� ������ ��������
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
