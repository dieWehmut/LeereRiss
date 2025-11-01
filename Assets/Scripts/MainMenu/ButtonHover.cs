using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple hover animator for UI Buttons: scales up a bit on hover and returns on exit.
/// Attach to a Button GameObject (or add at runtime) to get a subtle zoom effect.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.06f;
    public float duration = 0.12f;

    Vector3 originalScale;
    Coroutine running;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartScale(originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartScale(originalScale);
    }

    void StartScale(Vector3 target)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ScaleRoutine(target));
    }

    System.Collections.IEnumerator ScaleRoutine(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f,1f,t / duration));
            yield return null;
        }
        transform.localScale = target;
        running = null;
    }
}
