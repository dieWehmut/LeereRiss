using UnityEngine;
using System.Collections.Generic;

public interface IGenerateAlgorithm
{
    void Generate(int[,,] maze, int width, int height, int depth);
}

public class GenerateLib : MonoBehaviour
{
    [SerializeField] private int defaultAlgorithmIndex = 0;
    public List<IGenerateAlgorithm> algorithms = new List<IGenerateAlgorithm>();

    void Awake()
    {
        // 添加已实现的生成算法
        algorithms.Add(new GenerateAlgorithm0());
        algorithms.Add(new GenerateAlgorithm1());
        algorithms.Add(new GenerateAlgorithm2());
    }

    // 可以添加方法来选择或运行特定算法
    public void RunGeneration(int[,,] maze, int width, int height, int depth)
    {
        if (defaultAlgorithmIndex >= 0 && defaultAlgorithmIndex < algorithms.Count)
        {
            algorithms[defaultAlgorithmIndex].Generate(maze, width, height, depth);
        }
        else
        {
            Debug.LogWarning($"Default algorithm index {defaultAlgorithmIndex} is out of range.");
        }
    }
}