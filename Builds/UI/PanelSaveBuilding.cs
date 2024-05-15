using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;
using System.IO;

public class PanelSaveBuilding : MonoBehaviour
{
    [SerializeField] Button btnSave;
    [SerializeField] Button btnBack; 
    [SerializeField] TMP_InputField input;
    [SerializeField] TMP_Text txtSaveInfo;

    List<BlockData> saveBlocks;

    public void Init(List<BlockData> saveBlocks)
    {
        btnSave.onClick.AddListener(Save_Clicked);
        btnBack.onClick.AddListener(Close_Clicked);

        this.saveBlocks = saveBlocks;

        Cursor.lockState = CursorLockMode.None;
    }


    private void Save_Clicked()
    {
        if (input.text.Length == 0 && saveBlocks.Count == 0)
            return;

        var data = new BuildGenerator.BuildBlocks() { blocks = saveBlocks };
        var path = $"{Application.dataPath}/Data/Builds/{input.text}.json";
        var json = JsonUtility.ToJson(data);//Json.Serialize(saveBlocks);
        File.WriteAllText(path, json);
        txtSaveInfo.SetText($"≈бать, сохранил в {path}");
        btnSave.gameObject.SetActive(false);
    }

    private void Close_Clicked()
    {
        Cursor.lockState = CursorLockMode.Locked;

        Destroy(gameObject);
    }


    
}

