public partial class GuideLib
{
	private sealed class SafeGuide : IGuideAlgorithm
	{
		public float CalculateYaw(in GuideComputationContext context)
		{
			return 0f;
		}
	}
}
