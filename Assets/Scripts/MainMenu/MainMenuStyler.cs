using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime UI styler for the MainMenu scene.
/// Attach to an active GameObject (e.g. MainMenuManager). It will try to find the Canvas and a menu panel
/// and apply a simple, modern look: scaled canvas, background tint, button colors, text sizes and shadows.
/// Configure public fields in the Inspector to tweak colors, fonts and sizes.
/// This script is non-destructive and only changes component properties at runtime.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuStyler : MonoBehaviour
{
    [Header("References (optional)")]
    public Canvas targetCanvas;
    public RectTransform menuPanel; // the parent panel that contains menu controls

    [Header("General")]
    public Color backgroundColor = new Color(0.06f, 0.07f, 0.11f, 1f);
    public Color panelColor = new Color(1f, 1f, 1f, 0.06f);
    public Color accentColor = new Color(0.1f, 0.8f, 0.6f, 1f);

    [Header("Fonts & sizing")]
    public Font font; // if null will use built-in Arial
    public int titleFontSize = 72;
    public int labelFontSize = 24;
    public int buttonFontSize = 36;

    [Header("Button visuals")]
    public Color buttonNormal = new Color(0.12f, 0.12f, 0.12f, 0.9f);
    public Color buttonHighlighted = new Color(0.18f, 0.18f, 0.18f, 1f);
    public Color buttonPressed = new Color(0.08f, 0.08f, 0.08f, 1f);
    public Color buttonTextColor = Color.white;

    void Awake()
    {
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        if (targetCanvas != null)
        {
            var scaler = targetCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            // add a full-screen background image if none exists
            var bg = targetCanvas.transform.Find("_Background");
            if (bg == null)
            {
                GameObject bgGO = new GameObject("_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgGO.transform.SetParent(targetCanvas.transform, false);
                var rt = bgGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var img = bgGO.GetComponent<Image>();
                img.color = backgroundColor;
                img.raycastTarget = false;
            }
        }

        if (menuPanel == null && targetCanvas != null)
        {
            // try to find a reasonable menu panel by several common names or by VerticalLayoutGroup
            string[] tryNames = new[] { "Panel_Menu", "PanelMenu", "Panel_Difficulty", "PanelDifficulty", "Panel" };
            Transform found = null;
            foreach (var n in tryNames)
            {
                var t = targetCanvas.transform.Find(n);
                if (t != null) { found = t; break; }
            }
            if (found != null) menuPanel = found as RectTransform;
            else
            {
                var v = targetCanvas.GetComponentInChildren<VerticalLayoutGroup>(true);
                if (v != null) menuPanel = v.transform as RectTransform;
            }
        }

        ApplyStyle();
    }

    private void ApplyStyle()
    {
        if (menuPanel != null)
        {
            var img = menuPanel.GetComponent<Image>();
            if (img == null) img = menuPanel.gameObject.AddComponent<Image>();
            img.color = panelColor;

            // ensure layout group for neat spacing
            var layout = menuPanel.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = menuPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.padding = new RectOffset(24, 24, 24, 24);

            var fitter = menuPanel.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = menuPanel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Style all buttons under the canvas
        if (targetCanvas != null)
        {
            var buttons = targetCanvas.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                StyleButton(b);

                // add hover animation component if missing (makes UI feel responsive)
                if (b.GetComponent<ButtonHover>() == null)
                {
                    b.gameObject.AddComponent<ButtonHover>();
                }
            }

            var texts = targetCanvas.GetComponentsInChildren<Text>(true);
            foreach (var t in texts)
            {
                t.font = font;
                // try to detect title vs label vs button text by name or parent
                if (t.transform.name.ToLower().Contains("title") || t.text.ToLower().Contains("leereriss") )
                {
                    t.fontSize = titleFontSize;
                    t.alignment = TextAnchor.MiddleCenter;
                    AddOutlineIfMissing(t, Color.black, 0.15f);
                }
                else if (t.GetComponentInParent<Button>() != null)
                {
                    t.fontSize = buttonFontSize;
                    t.color = buttonTextColor;
                    t.alignment = TextAnchor.MiddleCenter;
                    AddOutlineIfMissing(t, Color.black, 0.15f);
                }
                else
                {
                    t.fontSize = labelFontSize;
                    t.color = Color.white;
                    AddShadowIfMissing(t, new Color(0,0,0,0.6f));
                }
            }

            // Style InputFields and Sliders
            var inputs = targetCanvas.GetComponentsInChildren<InputField>(true);
            foreach (var inp in inputs)
            {
                var bg = inp.GetComponent<Image>();
                if (bg == null) bg = inp.gameObject.AddComponent<Image>();
                bg.color = new Color(1f,1f,1f,0.06f);
                var txt = inp.textComponent;
                if (txt != null) { txt.font = font; txt.fontSize = labelFontSize; txt.color = Color.white; }
                var placeholder = inp.placeholder as Text;
                if (placeholder != null) { placeholder.font = font; placeholder.fontSize = labelFontSize; placeholder.color = new Color(0.8f,0.8f,0.8f,0.7f); }
            }

            var sliders = targetCanvas.GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                var bg = s.GetComponent<Image>();
                if (bg != null) bg.color = new Color(1f,1f,1f,0.04f);
            }
        }
    }

    private void StyleButton(Button b)
    {
        var img = b.GetComponent<Image>();
        if (img == null) img = b.gameObject.AddComponent<Image>();
        img.color = buttonNormal;
        img.type = Image.Type.Sliced;

        var cb = b.colors;
        cb.normalColor = buttonNormal;
        cb.highlightedColor = buttonHighlighted;
        cb.pressedColor = buttonPressed;
        cb.disabledColor = new Color(0.3f,0.3f,0.3f,0.6f);
        cb.colorMultiplier = 1f;
        b.colors = cb;

        var txt = b.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.font = font;
            txt.color = buttonTextColor;
            txt.fontSize = buttonFontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            AddOutlineIfMissing(txt, Color.black, 0.15f);
        }
    }

    private void AddOutlineIfMissing(Text t, Color color, float effectDistance)
    {
        if (t.GetComponent<Outline>() == null)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(effectDistance * 10f, effectDistance * 10f);
        }
    }

    private void AddShadowIfMissing(Text t, Color color)
    {
        if (t.GetComponent<Shadow>() == null)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = color;
            s.effectDistance = new Vector2(2f, -2f);
        }
    }
}
