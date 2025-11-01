using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GuideLib : MonoBehaviour
{
	public enum GuideMode
	{
		ShortestPath,
		SafeRoute,
		EnemyFocus,
		Stealth
	}

	[Serializable]
	private struct ModeVisual
	{
		public GuideMode mode;
		public Sprite sprite;
	}

	[SerializeField] private PlayerInput playerInput;
	[SerializeField] private ModeVisual[] modeVisuals;

	private static readonly GuideMode[] ModeSequence = (GuideMode[])Enum.GetValues(typeof(GuideMode));
	private readonly Dictionary<GuideMode, Sprite> spriteLookup = new Dictionary<GuideMode, Sprite>();
	private readonly Dictionary<GuideMode, IGuideAlgorithm> algorithmLookup = new Dictionary<GuideMode, IGuideAlgorithm>();
	private int modeIndex;
	private GuideMode currentMode;

	public GuideMode CurrentMode => currentMode;

	private void Awake()
	{
		if (playerInput == null) playerInput = GetComponent<PlayerInput>();
		BuildSpriteLookup();
		RegisterAlgorithms();
		currentMode = ModeSequence.Length > 0 ? ModeSequence[modeIndex] : GuideMode.ShortestPath;
	}

	private void OnValidate()
	{
		if (playerInput == null) playerInput = GetComponent<PlayerInput>();
		BuildSpriteLookup();
	}

	private void Update()
	{
		if (playerInput != null && playerInput.guideCyclePressed)
		{
			AdvanceMode();
		}
	}

	private void AdvanceMode()
	{
		if (ModeSequence.Length == 0) return;
		modeIndex = (modeIndex + 1) % ModeSequence.Length;
		currentMode = ModeSequence[modeIndex];
	}

	private void BuildSpriteLookup()
	{
		spriteLookup.Clear();
		if (modeVisuals == null) return;
		foreach (var visual in modeVisuals)
		{
			if (!spriteLookup.ContainsKey(visual.mode) && visual.sprite != null)
			{
				spriteLookup.Add(visual.mode, visual.sprite);
			}
		}
	}

	private void RegisterAlgorithms()
	{
		algorithmLookup.Clear();
		algorithmLookup[GuideMode.ShortestPath] = new ShortestPathGuide();
		algorithmLookup[GuideMode.SafeRoute] = new SafeGuide();
		algorithmLookup[GuideMode.EnemyFocus] = new EnemyGuide();
		algorithmLookup[GuideMode.Stealth] = new StealthGuide();
	}

	internal Sprite GetSpriteForMode(GuideMode mode)
	{
		return spriteLookup.TryGetValue(mode, out var sprite) ? sprite : null;
	}

	internal float EvaluateCurrentYaw(Transform arrowTransform)
	{
		if (!algorithmLookup.TryGetValue(currentMode, out var algorithm)) return 0f;
		var context = new GuideComputationContext(transform, arrowTransform);
		return NormalizeYaw(algorithm.CalculateYaw(context));
	}

	internal static float NormalizeYaw(float yaw)
	{
		if (float.IsNaN(yaw) || float.IsInfinity(yaw)) return 0f;
		yaw %= 360f;
		if (yaw > 180f) yaw -= 360f;
		else if (yaw < -180f) yaw += 360f;
		return yaw;
	}

	internal readonly struct GuideComputationContext
	{
		public readonly Transform PlayerTransform;
		public readonly Transform ArrowTransform;

		public GuideComputationContext(Transform playerTransform, Transform arrowTransform)
		{
			PlayerTransform = playerTransform;
			ArrowTransform = arrowTransform;
		}
	}

	private interface IGuideAlgorithm
	{
		float CalculateYaw(in GuideComputationContext context);
	}
}
