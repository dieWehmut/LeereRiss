public partial class GuideLib
{
	private sealed class EnemyGuide : IGuideAlgorithm
	{
		public float CalculateYaw(in GuideComputationContext context)
		{
			return 0f;
		}
	}
}
