using UnityEngine;

public class UpdateAlgorithm0 : IUpdateAlgorithm
{
    public void UpdateMaze(int[,,] maze, int width, int height, int depth)
    {
        // 示例更新算法：在初始迷宫基础上动态变化
        // 随机翻转一些内部格子（Void <-> Solid），但保持底部和表面结构
        // 注意：这个简单实现可能不保证完全连通，仅作为示例

        int changes = Random.Range(1, 10); // 随机改变1-9个格子

        for (int i = 0; i < changes; i++)
        {
            int x = Random.Range(1, width - 1); // 内部x
            int y = Random.Range(1, height - 1); // 内部y
            int z = Random.Range(1, depth - 1); // 内部z

            // 翻转
            if (maze[x, y, z] == (int)MazeData.CellType.Void)
            {
                maze[x, y, z] = (int)MazeData.CellType.Solid;
            }
            else
            {
                maze[x, y, z] = (int)MazeData.CellType.Void;
            }
        }
    }
}