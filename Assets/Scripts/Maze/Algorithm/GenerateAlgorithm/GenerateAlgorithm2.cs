public class GenerateAlgorithm2 : IGenerateAlgorithm
{
    public void Generate(int[,,] maze, int width, int height, int depth)
    {
        // 复用基础生成逻辑
        new GenerateAlgorithm0().Generate(maze, width, height, depth);
    }
}
