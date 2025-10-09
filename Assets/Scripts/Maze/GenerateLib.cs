using UnityEngine;
using System.Collections.Generic;

public interface IGenerateAlgorithm
{
    void Generate(int[,,] maze, int width, int height, int depth);
}

public class GenerateLib : MonoBehaviour
{
    public List<IGenerateAlgorithm> algorithms = new List<IGenerateAlgorithm>();

    void Awake()
    {
        // 添加已实现的生成算法
        algorithms.Add(new GenerateAlgorithm0());
        // 其他算法留给组员实现并注册，例如：
        // algorithms.Add(new GenerateAlgorithm1()); // 待组员实现
        // algorithms.Add(new GenerateAlgorithm2()); // 待组员实现
    }

    // 可以添加方法来选择或运行特定算法
    public void RunGeneration(int[,,] maze, int width, int height, int depth)
    {
        foreach (var algo in algorithms)
        {
            algo.Generate(maze, width, height, depth);
        }
    }
}