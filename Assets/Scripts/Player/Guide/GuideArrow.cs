using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GuideArrow : MonoBehaviour
{
	[SerializeField] private GuideLib guideLib;
	[SerializeField] private SpriteRenderer spriteRenderer;

	private GuideLib.GuideMode lastMode = (GuideLib.GuideMode)(-1);
	private float lastYaw = float.NaN;

	private void Awake()
	{
		if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
		if (guideLib == null) guideLib = GetComponentInParent<GuideLib>();
	}

	private void Update()
	{
		if (guideLib == null || spriteRenderer == null) return;

		var mode = guideLib.CurrentMode;
		UpdateSprite(mode);
		UpdateYaw(mode);
	}

	private void UpdateSprite(GuideLib.GuideMode mode)
	{
		var sprite = guideLib.GetSpriteForMode(mode);
		if (sprite == null)
		{
			spriteRenderer.enabled = false;
			lastMode = mode;
			return;
		}

		if (!spriteRenderer.enabled) spriteRenderer.enabled = true;
		if (lastMode != mode || spriteRenderer.sprite != sprite)
		{
			spriteRenderer.sprite = sprite;
			lastMode = mode;
		}
	}

	private void UpdateYaw(GuideLib.GuideMode mode)
	{
		if (!spriteRenderer.enabled) return;
		var yaw = guideLib.EvaluateCurrentYaw(transform);
		if (float.IsNaN(yaw) || float.IsInfinity(yaw)) yaw = 0f;
		if (!Mathf.Approximately(yaw, lastYaw))
		{
			ApplyYaw(yaw);
			lastYaw = yaw;
		}
	}

	private void ApplyYaw(float yaw)
	{
		transform.localRotation = Quaternion.Euler(90f, GuideLib.NormalizeYaw(yaw), 0f);
	}
}
