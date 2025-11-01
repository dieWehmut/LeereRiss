public partial class GuideLib
{
	private sealed class StealthGuide : IGuideAlgorithm
	{
		public float CalculateYaw(in GuideComputationContext context)
		{
			return 0f;
		}
	}
}
