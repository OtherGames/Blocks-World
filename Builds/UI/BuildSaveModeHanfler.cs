using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildSaveModeHanfler : MonoBehaviour
{
    [SerializeField] GameObject indicator;

    private void Awake()
    {
        SaveBuilding.onInit.AddListener(Inited);
    }

    private void Inited()
    {
        indicator.SetActive(SaveBuilding.Instance.WriteMode);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            var curState = SaveBuilding.Instance.WriteMode;
            SaveBuilding.Instance.WriteMode = !curState;
            indicator.SetActive(!curState);

            if (!SaveBuilding.Instance.WriteMode)
            {
                SaveBuilding.Instance.Save();
            }
        }
    }
}
