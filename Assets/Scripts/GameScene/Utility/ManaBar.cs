using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour
{
	[SerializeField]
	private Slider slider;

	[SerializeField]
	private Image fillImage;

	void Awake()
	{
		if (slider == null)
		{
			slider = GetComponentInChildren<Slider>(includeInactive: true);
		}

		if (fillImage == null)
		{
			fillImage = GetComponentInChildren<Image>(includeInactive: true);
		}
	}

	public void SetValue(float normalizedValue)
	{
		normalizedValue = Mathf.Clamp01(normalizedValue);
		if (slider != null)
		{
			slider.value = normalizedValue;
		}
		if (fillImage != null)
		{
			fillImage.fillAmount = normalizedValue;
		}
	}
}
