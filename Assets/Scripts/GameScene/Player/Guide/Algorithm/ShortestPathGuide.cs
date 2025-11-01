public partial class GuideLib
{
	private sealed class ShortestPathGuide : IGuideAlgorithm
	{
		public float CalculateYaw(in GuideComputationContext context)
		{
			return 0f;
		}
	}
}
