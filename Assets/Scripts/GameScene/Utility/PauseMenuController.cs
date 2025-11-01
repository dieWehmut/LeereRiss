using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Press right mouse button to toggle Pause Menu.
/// If no UI is bound, this will create a simple overlay with Title, Description and three buttons:
/// Restart | Main Menu | Continue
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Bindings (optional)")]
    public GameObject pausePanel;
    public Text titleText;
    public Text detailText;
    public Button restartButton;
    public Button mainMenuButton;
    public Button continueButton;
    public Button quitButton;
    public PlayerInput playerInput;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    public bool preserveExistingButtonOnClicks = false;

    private bool paused = false;

    void Awake()
    {
        EnsureEventSystem();
        if (playerInput == null)
            playerInput = FindObjectOfType<PlayerInput>();
        if (pausePanel != null)
        {
            // 如果编辑器中放了 Panel，先隐藏
            pausePanel.SetActive(false);
        }
        else
        {
            // 不创建直到第一次按下右键——这样场景里不会被打扰
        }
    }

    void Update()
    {
        // 右键按下切换暂停 —— 使用 PlayerInput 提供的按键状态，如果没有则回退到直接读取 Input
        bool pauseRequest = false;
        if (playerInput != null) pauseRequest = playerInput.pausePressed;
        else pauseRequest = Input.GetMouseButtonDown(1);

        if (pauseRequest)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (!paused)
        {
            ShowPause();
        }
        else
        {
            HidePause();
        }
    }

    private void ShowPause()
    {
        paused = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 启用模态输入阻塞：禁用键盘与右键，保留左键
        InputBlocker.EnableModalBlock();

        if (pausePanel == null)
        {
            CreateDefaultUI();
        }

        // 自动从层级绑定文本与按钮（如果未绑定）
        TryAutoWireFromHierarchy();

        if (pausePanel != null) pausePanel.SetActive(true);
        if (titleText != null) titleText.text = "Paused";
        if (detailText != null)
        {
            detailText.text = "Game is paused.\nRight click or press Continue to resume.\nControls: WASD to move, Space to jump, Left-click to shoot.";
        }

        WireButtons();
    }

    private void HidePause()
    {
        paused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (pausePanel != null) pausePanel.SetActive(false);

        // 解除阻塞
        InputBlocker.DisableModalBlock();
    }

    private void WireButtons()
    {
        if (restartButton != null)
        {
            if (!preserveExistingButtonOnClicks)
                restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });
            ApplyButtonStyle(restartButton);
        }

        if (mainMenuButton != null)
        {
            if (!preserveExistingButtonOnClicks)
                mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                if (!string.IsNullOrEmpty(mainMenuSceneName)) SceneManager.LoadScene(mainMenuSceneName);
            });
            ApplyButtonStyle(mainMenuButton);
        }

        if (continueButton != null)
        {
            if (!preserveExistingButtonOnClicks)
                continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                HidePause();
            });
            ApplyButtonStyle(continueButton);
        }
        if (quitButton != null)
        {
            if (!preserveExistingButtonOnClicks)
                quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            ApplyButtonStyle(quitButton);
        }
    }

    private void ApplyButtonStyle(Button btn)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.18f);
        colors.highlightedColor = new Color(0.35f, 0.65f, 1f, 0.35f);
        colors.pressedColor = new Color(0.2f, 0.45f, 0.9f, 0.5f);
        btn.colors = colors;

        var hover = btn.GetComponent<ButtonHover>();
        if (hover == null)
            btn.gameObject.AddComponent<ButtonHover>();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void CreateDefaultUI()
    {
        // Create a basic Canvas/Panel with texts and buttons
        var canvasGO = new GameObject("PauseMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);

        pausePanel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        pausePanel.transform.SetParent(canvas.transform, false);
        var panelRt = pausePanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0, 0);
        panelRt.anchorMax = new Vector2(1, 1);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        var panelImg = pausePanel.GetComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.6f);

        var container = new GameObject("Container", typeof(RectTransform));
        container.transform.SetParent(pausePanel.transform, false);
        var crt = container.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(640, 360);
        crt.anchoredPosition = Vector2.zero;

        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGO.transform.SetParent(container.transform, false);
        var tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.5f, 1f);
        tRT.anchorMax = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(600, 80);
        tRT.anchoredPosition = new Vector2(0, -40);
        titleText = titleGO.GetComponent<Text>();
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = 48;
        titleText.color = Color.white;

        var detailGO = new GameObject("Detail", typeof(RectTransform), typeof(Text));
        detailGO.transform.SetParent(container.transform, false);
        var dRT = detailGO.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.5f, 0.5f);
        dRT.anchorMax = new Vector2(0.5f, 0.5f);
        dRT.sizeDelta = new Vector2(600, 120);
        dRT.anchoredPosition = new Vector2(0, -10);
        detailText = detailGO.GetComponent<Text>();
        detailText.alignment = TextAnchor.MiddleCenter;
        detailText.fontSize = 22;
        detailText.color = Color.white;

        var btns = new GameObject("Buttons", typeof(RectTransform));
        btns.transform.SetParent(container.transform, false);
        var btnsRT = btns.GetComponent<RectTransform>();
        btnsRT.anchorMin = new Vector2(0.5f, 0f);
        btnsRT.anchorMax = new Vector2(0.5f, 0f);
        btnsRT.sizeDelta = new Vector2(600, 80);
        btnsRT.anchoredPosition = new Vector2(0, 40);

        Button CreateButton(string name, string text, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(btns.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(180, 56);
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.15f);
            var btn = go.GetComponent<Button>();

            var labelGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var lt = labelGO.GetComponent<Text>();
            lt.alignment = TextAnchor.MiddleCenter;
            lt.fontSize = 22;
            lt.color = Color.white;
            lt.text = text;

            return btn;
        }

        restartButton = CreateButton("Restart", "Restart", new Vector2(-270, 0));
        mainMenuButton = CreateButton("MainMenu", "Main Menu", new Vector2(-90, 0));
        continueButton = CreateButton("Continue", "Continue", new Vector2(90, 0));
        quitButton = CreateButton("Quit", "Quit", new Vector2(270, 0));
    }

    private void TryAutoWireFromHierarchy()
    {
        if (pausePanel == null) return;
        if (titleText == null || detailText == null)
        {
            var texts = pausePanel.GetComponentsInChildren<Text>(includeInactive: true);
            foreach (var t in texts)
            {
                var name = t.gameObject.name.ToLower();
                if (titleText == null && name.Contains("title")) titleText = t;
                else if (detailText == null && name.Contains("detail")) detailText = t;
            }
            if (titleText == null && texts.Length > 0) titleText = texts[0];
            if (detailText == null && texts.Length > 1) detailText = texts[1];
        }

        if (restartButton == null || mainMenuButton == null || continueButton == null)
        {
            var btns = pausePanel.GetComponentsInChildren<Button>(includeInactive: true);
            foreach (var b in btns)
            {
                var name = b.gameObject.name.ToLower();
                if (restartButton == null && (name.Contains("restart") || name.Contains("retry") || name.Contains("again"))) restartButton = b;
                else if (mainMenuButton == null && (name.Contains("mainmenu") || name.Contains("menu") || name == "main")) mainMenuButton = b;
                else if (continueButton == null && (name.Contains("continue") || name.Contains("resume"))) continueButton = b;
                else if (quitButton == null && (name.Contains("quit") || name.Contains("exit"))) quitButton = b;
            }
        }
    }
}
