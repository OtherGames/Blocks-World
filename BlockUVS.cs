using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BlockUVS
{
    public int TextureX;
    public int TextureY;

    public int TextureXSide;
    public int TextureYSide;

    public int TextureXBottom;
    public int TextureYBottom;

    public BlockUVS(int tX, int tY, int sX, int sY, int bX, int bY)
    {
        TextureX = tX;
        TextureY = tY;
        TextureXSide = sX;
        TextureYSide = sY;
        TextureXBottom = bX;
        TextureYBottom = bY;
    }

    public BlockUVS(int tX, int tY, int sX, int sY)
    {
        TextureX = tX;
        TextureY = tY;
        TextureXSide = sX;
        TextureYSide = sY;
        TextureXBottom = tX;
        TextureYBottom = tY;
    }

    public BlockUVS(int tX, int tY)
    {
        TextureX = tX;
        TextureY = tY;
        TextureXSide = tX;
        TextureYSide = tY;
        TextureXBottom = tX;
        TextureYBottom = tY;
    }

    public static BlockUVS GetBlock(byte id)
    {
        switch (id)
        {
            case 1:// �����
                return new BlockUVS(0, 15, 3, 15, 2, 15);
            case 2:// ������
                return new BlockUVS(1, 15);
            case 3:// ��������
                return new BlockUVS(0, 14);
            case 4:// �����
                return new BlockUVS(2, 15);
            case 5:
                return new BlockUVS(1, 1);
            case 6:// �������� ����
                return new BlockUVS(2, 13);
            case 7:
                return new BlockUVS(4, 2);
            case 8:
                return new BlockUVS(5, 14, 4, 14);
            case 9:// ������
                return new BlockUVS(5, 14, 4, 14);
            case 10:// ������
                return new BlockUVS(5, 12);
            case 11:// �����
                return new BlockUVS(4, 15);
            case 12:// ������� �����
                return new BlockUVS(2, 4);
            case 15:// ������� �����
                return new BlockUVS(2, 11);
            case 14:// �����-�� ������
                return new BlockUVS(8, 3);
            case 30:// �������� ����
                return new BlockUVS(1, 13);
            case 31:// �������
                return new BlockUVS(15, 1);
            case 32:// ����
                return new BlockUVS(15, 2);
            case 36:// ������
                return new BlockUVS(3, 14);
            case 61:// Cliff Road
                return new BlockUVS(8, 2);
            case 62:// Cobblestone Wall
                return new BlockUVS(7, 1);
            case 63:// Wall Cliff
                return new BlockUVS(8, 1);
            case 64:// Cobblestone Road
                return new BlockUVS(9, 1);
            case 66:
                return new BlockUVS(0, 1);
            case 88:// Engine
                return new BlockUVS(2, 14);
            case 90:// �����
                return new BlockUVS(2, 14);
            case 91:// Actuator Rotary
                return new BlockUVS(5, 1);
            case 92:// Wooded Timber 
                return new BlockUVS(12, 3, 12, 4);
            case 94:// INTERWOVEN_STONE 
                return new BlockUVS(9, 2);
            case 100:// ������� �������
                return new BlockUVS(15, 0);
            case 101:// �������
                return new BlockUVS(11, 13);
            case 102:// ����
                return new BlockUVS(14, 12, 12, 13);

        }

        return new BlockUVS(0, 15, 3, 15, 2, 15);
    }
}