using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BLOCKS
{
    public const byte STONE = 2;
    public const byte DIRT = 4;
    public const byte WOOD = 8;
    public const byte WOODEN_PLANK = 11;
    public const byte FURNACE = 102;
    public const byte SALTPETER = 31;
    public const byte ORE_SULFUR = 32;
    public const byte ORE_COAL = 6;
    public const byte GRAVEL = 36;
    public const byte ENGINE = 88;
    public const byte ACTUATOR = 90;
    public const byte ACTUATOR_ROTARY = 91;
    public const byte STEERING = 92;
    public const byte SIMPLE_WORKBENCH = 100;


    public enum BlockName : byte
    {
        GRASS = 1,
        STONE = 2,
        COBBLESTONE = 3,
        DIRT = 4,
        WOOD = 8,
    }
}
