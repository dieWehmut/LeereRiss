using UnityEngine;
using System.Collections.Generic;

public interface IUpdateAlgorithm
{
    void UpdateMaze(int[,,] maze, int width, int height, int depth);
}

public class UpdateLib : MonoBehaviour
{
    public List<IUpdateAlgorithm> algorithms = new List<IUpdateAlgorithm>();

    void Awake()
    {
        // 添加所有更新算法
        algorithms.Add(new UpdateAlgorithm0());
        // algorithms.Add(new UpdateAlgorithm1()); // 待组员实现
        // algorithms.Add(new UpdateAlgorithm2()); // 待组员实现
    }

    public void RunUpdate(int[,,] maze, int width, int height, int depth)
    {
        foreach (var algo in algorithms)
        {
            algo.UpdateMaze(maze, width, height, depth);
        }
    }
}