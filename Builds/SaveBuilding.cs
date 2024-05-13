using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

public class SaveBuilding : MonoBehaviour
{
    [SerializeField] PanelSaveBuilding panelSaveBuildingPrefab;

    public static SaveBuilding Instance;
    public static UnityEvent onInit = new UnityEvent();

    public List<BlockData> saveBlocks;
    public bool WriteMode { get; set; }

    Transform UIRoot;

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        Instance = this;

        UIRoot = FindObjectOfType<CanvasScaler>().transform;

        saveBlocks = new List<BlockData>();

        WorldGenerator.onBlockPick.AddListener(Block_Picked);
        WorldGenerator.onBlockPlace.AddListener(Block_Placed);

        onInit?.Invoke();
    }

    public void Save()
    {
        PrepareBlocksPositions();

        var panel = Instantiate(panelSaveBuildingPrefab, UIRoot);
        panel.Init(saveBlocks);
    }

    private void Block_Placed(BlockData blockData)
    {
        if (WriteMode)
        {
            saveBlocks.Add(blockData);
        }
    }

    private void Block_Picked(BlockData blockData)
    {
        if (WriteMode)
        {
            var pos = blockData.pos;
            var block = saveBlocks.Find(b => b.pos == pos);

            if (block != null)
            {
                print($"делетнул {saveBlocks.Remove(block)}");
                //saveBlocks.Remove(block);
            }
        }
    }

    /// <summary>
    /// —мещаем все блоки относительно точки 0,0,0
    /// </summary>
    void PrepareBlocksPositions()
    {
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var minZ = float.MaxValue;

        foreach (var blockData in saveBlocks)
        {
            if (blockData.pos.x < minX)
            {
                minX = blockData.pos.x;
            }
            if (blockData.pos.y < minY)
            {
                minY = blockData.pos.y;
            }
            if (blockData.pos.z < minZ)
            {
                minZ = blockData.pos.z;
            }
        }

        foreach (var blockData in saveBlocks)
        {
            blockData.pos.x -= minX;
            blockData.pos.y -= minY;
            blockData.pos.z -= minZ;
        }
    }
}
