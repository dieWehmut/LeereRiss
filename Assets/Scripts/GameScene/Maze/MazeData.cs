using UnityEngine;

public class MazeData : MonoBehaviour
{
    // 定义格子类型枚举
    public enum CellType
    {
        Void = 0,  // 空，无任何东西
        Solid = 1, // 放置Solid
        Ethereal = 2 // 以太类格子（行为上与 Solid 等同于生成流程，但不可放置在迷宫表面）
    }

    // 可以在这里添加其他数据类型或常量
}