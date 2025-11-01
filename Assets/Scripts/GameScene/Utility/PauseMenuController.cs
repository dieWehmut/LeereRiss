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
    // 防抖：避免同一次右键触发关闭后又立刻触发打开（多脚本/同帧次序导致的回闪）
    private float nextToggleAllowedTime = 0f; // 使用 unscaledTime（暂停时也走时钟）

    // 保存显示期间对画布的临时修改（置顶与排序）
    private Canvas cachedCanvas;
    private bool prevOverrideSorting;
    private int prevSortingOrder;

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
        // 右键按下切换暂停/继续
        // 未暂停时：用 PlayerInput（若有）控制打开菜单；
        // 已暂停时：不受 InputBlocker 限制，直接读取右键以便快速继续游戏。
        if (!paused)
        {
            bool pauseRequest = false;
            if (playerInput != null) pauseRequest = playerInput.pausePressed;
            else pauseRequest = Input.GetMouseButtonDown(1);
            if (pauseRequest && Time.unscaledTime >= nextToggleAllowedTime)
            {
                TogglePause();
                nextToggleAllowedTime = Time.unscaledTime + 0.2f;
            }
        }
        else
        {
            // 在菜单界面再次点击右键 = 点击“Continue”
            if ((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) && Time.unscaledTime >= nextToggleAllowedTime)
            {
                TogglePause();
                nextToggleAllowedTime = Time.unscaledTime + 0.2f;
            }
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

        // 确保 UI 可被点击/悬浮：父 Canvas 要有 GraphicRaycaster，Panel 要允许 Raycast
        EnsureGraphicRaycasterForPanelCanvas(pausePanel);
        EnsurePanelCanvasGroup(pausePanel);
        SetTopmostCanvas(pausePanel, true);

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

        // 还原画布排序
        SetTopmostCanvas(pausePanel, false);
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
                // 重要：离开暂停时解除模态阻塞，避免重载场景后输入被永久屏蔽
                InputBlocker.DisableModalBlock();
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
                // 重要：返回主菜单前解除阻塞，避免在主菜单/新一局输入失效
                InputBlocker.DisableModalBlock();
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

    // 确保包含 panel 的父 Canvas 有 GraphicRaycaster（否则不会接收 UI 射线）
    private void EnsureGraphicRaycasterForPanelCanvas(GameObject panel)
    {
        if (panel == null) return;
        var canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    // 确保 Panel（或其父物体）存在 CanvasGroup 并允许 Raycast & 交互
    private void EnsurePanelCanvasGroup(GameObject panel)
    {
        if (panel == null) return;
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.ignoreParentGroups = false;
        panel.SetActive(true);
    }

    // 将该 Panel 所在画布临时置顶，避免被其他全屏 UI 遮住（导致无法悬浮/点击）
    private void SetTopmostCanvas(GameObject panel, bool enable)
    {
        if (panel == null) return;
        var canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        if (enable)
        {
            cachedCanvas = canvas;
            prevOverrideSorting = canvas.overrideSorting;
            prevSortingOrder = canvas.sortingOrder;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999;
        }
        else if (cachedCanvas != null)
        {
            cachedCanvas.overrideSorting = prevOverrideSorting;
            cachedCanvas.sortingOrder = prevSortingOrder;
            cachedCanvas = null;
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
