using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [Header("References")]
    public PlayerResource playerResource;
    public MazeGenerator mazeGenerator;
    public Transform player;

    [Header("UI (optional, will auto-create if empty)")]
    public GameObject gameOverPanel;
    public Text titleText;
    public Text detailText;
    private Component titleTextTMP;   // 运行时反射绑定 TMP_Text（若工程存在）
    private Component detailTextTMP;
    public Button restartButton;
    public Button mainMenuButton;
    public Button quitButton;

    [Header("Settings")] 
    [Tooltip("与出口位置的判定距离（世界坐标）")]
    public float exitProximity = 1.0f;
    [Tooltip("主菜单场景名（用于返回主菜单）")]
    public string mainMenuSceneName = "MainMenu";
    [Tooltip("保留在 Inspector 中为按钮配置的 OnClick 事件（勾选后脚本不会清空现有 OnClick）")]
    public bool preserveExistingButtonOnClicks = false;
    [Tooltip("在正常游戏过程中托管鼠标（隐藏并锁定），仅在弹窗时释放")]
    public bool manageGameplayCursor = true;

    private bool ended;
    private bool prevCursorVisible;
    private CursorLockMode prevCursorLockMode;
    private bool gameplayCursorApplied;

    // 显示期间临时置顶画布并还原
    private Canvas cachedCanvas;
    private bool prevOverrideSorting;
    private int prevSortingOrder;

    void Awake()
    {
        if (playerResource == null)
            playerResource = FindObjectOfType<PlayerResource>();
        if (mazeGenerator == null)
            mazeGenerator = FindObjectOfType<MazeGenerator>();
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            if (player == null)
            {
                var playerMono = FindObjectOfType<Player>();
                if (playerMono != null) player = playerMono.transform;
            }
        }

        EnsureUI();
        EnsureEventSystem();
        HidePanel();
        // 开局进入正常游戏状态时，隐藏并锁定光标
        if (manageGameplayCursor)
        {
            ApplyGameplayCursor();
        }
    }

    void Update()
    {
        if (ended) return;
        if (playerResource == null) return;

        // 失败：生命归零
        if (playerResource.CurrentHealth <= 0.0001f)
        {
            ShowLose();
            return;
        }

        // 胜利：到达出口
        if (mazeGenerator != null && player != null)
        {
            if (mazeGenerator.TryGetExitWorldPosition(out var exitPos))
            {
                float dist = Vector3.Distance(player.position, exitPos);
                // 动态阈值：考虑到单元大小可能很大/很小，取 cellSize 的一半与 exitProximity 的较大者
                float dynamicThreshold = Mathf.Max(exitProximity, mazeGenerator.cellSize * 0.5f);
                if (dist <= dynamicThreshold)
                {
                    ShowWin();
                    return;
                }
            }
        }

        // 守护：在正常游戏过程确保光标保持隐藏+锁定
        // 但当有模态阻塞（例如 Pause 或 GameOver）时不要覆盖它们。
        if (!ended && manageGameplayCursor && !InputBlocker.IsBlocked)
        {
            if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
            {
                ApplyGameplayCursor();
            }
        }
    }

    public void ShowWin()
    {
        ShowPanel("Victory", "You reached the maze exit!");
    }

    public void ShowLose()
    {
        ShowPanel("Defeat", "You died. Try again!");
    }

    private void ShowPanel(string title, string detail)
    {
        ended = true;
        // 捕获并释放鼠标
        prevCursorVisible = Cursor.visible;
        prevCursorLockMode = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

    // 启用模态阻塞：禁用键盘与右键，保留左键
    InputBlocker.EnableModalBlock();

        // 自动从层级绑定（避免忘绑导致无响应）
        TryAutoWireFromHierarchy();
        EnsureRaycastBlocker();
        EnsureGraphicRaycaster();
        EnsurePanelCanvasGroup(gameOverPanel);
        SetTopmostCanvas(gameOverPanel, true);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = title;
        }
        SetTMPTextActiveAndValue(titleTextTMP, title, true);
        if (detailText != null)
        {
            detailText.gameObject.SetActive(true);
            detailText.text = detail;
        }
        SetTMPTextActiveAndValue(detailTextTMP, detail, true);
        Time.timeScale = 0f; // 暂停游戏
    }

    private void HidePanel()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        SetTopmostCanvas(gameOverPanel, false);
    }

    private void EnsureUI()
    {
        // 如果未绑定任何 UI，则动态创建一个最简的覆盖 UI
        if (gameOverPanel != null && titleText != null && detailText != null)
        {
            // 同时补充按钮回调
            WireButtons();
            return;
        }

        // Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
        }

        // Panel 背景
        gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        gameOverPanel.transform.SetParent(canvas.transform, false);
        var panelRt = gameOverPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0, 0);
        panelRt.anchorMax = new Vector2(1, 1);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        var panelImg = gameOverPanel.GetComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.6f);

        // 容器
        var container = new GameObject("Container", typeof(RectTransform));
        container.transform.SetParent(gameOverPanel.transform, false);
        var crt = container.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(600, 320);
        crt.anchoredPosition = Vector2.zero;

        // 标题
        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGO.transform.SetParent(container.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(600, 80);
        titleRT.anchoredPosition = new Vector2(0, -40);
        titleText = titleGO.GetComponent<Text>();
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = 48;
        titleText.color = Color.white;
        titleText.text = "游戏结束";

        // 描述
        var detailGO = new GameObject("Detail", typeof(RectTransform), typeof(Text));
        detailGO.transform.SetParent(container.transform, false);
        var detailRT = detailGO.GetComponent<RectTransform>();
        detailRT.anchorMin = new Vector2(0.5f, 0.5f);
        detailRT.anchorMax = new Vector2(0.5f, 0.5f);
        detailRT.sizeDelta = new Vector2(600, 80);
        detailRT.anchoredPosition = new Vector2(0, -10);
        detailText = detailGO.GetComponent<Text>();
        detailText.alignment = TextAnchor.MiddleCenter;
        detailText.fontSize = 28;
        detailText.color = Color.white;
        detailText.text = "";

        // 按钮容器
        var btns = new GameObject("Buttons", typeof(RectTransform));
        btns.transform.SetParent(container.transform, false);
        var btnsRT = btns.GetComponent<RectTransform>();
        btnsRT.anchorMin = new Vector2(0.5f, 0f);
        btnsRT.anchorMax = new Vector2(0.5f, 0f);
        btnsRT.sizeDelta = new Vector2(600, 80);
        btnsRT.anchoredPosition = new Vector2(0, 40);

        // 创建按钮方法
        Button CreateButton(string name, string text, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(btns.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(160, 48);
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

        restartButton = CreateButton("Restart", "重新开始", new Vector2(-190, 0));
        mainMenuButton = CreateButton("MainMenu", "主菜单", new Vector2(0, 0));
        quitButton = CreateButton("Quit", "退出游戏", new Vector2(190, 0));

        WireButtons();
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
                // 解除阻塞并恢复游戏内光标状态
                InputBlocker.DisableModalBlock();
                if (manageGameplayCursor)
                {
                    ApplyGameplayCursor();
                }
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
                if (string.IsNullOrEmpty(mainMenuSceneName)) return;
                Time.timeScale = 1f;
                // 解除阻塞后跳回主菜单（主菜单自己的脚本负责显示光标）
                InputBlocker.DisableModalBlock();
                SceneManager.LoadScene(mainMenuSceneName);
            });
            ApplyButtonStyle(mainMenuButton);
        }
        if (quitButton != null)
        {
            if (!preserveExistingButtonOnClicks)
                quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                InputBlocker.DisableModalBlock();
                UnityEditor.EditorApplication.isPlaying = false;
#else
                InputBlocker.DisableModalBlock();
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
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f,1f,1f,0.1f);
        btn.colors = colors;

        // 悬浮缩放效果：重用 MainMenu/ButtonHover.cs
        var hover = btn.GetComponent<ButtonHover>();
        if (hover == null)
        {
            hover = btn.gameObject.AddComponent<ButtonHover>();
        }

        // 可选描边
        var outline = btn.GetComponent<Outline>();
        if (outline == null)
        {
            outline = btn.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            // 默认设置即可
        }
    }

    private void EnsureRaycastBlocker()
    {
        if (gameOverPanel == null) return;
        var img = gameOverPanel.GetComponent<Image>();
        if (img == null) img = gameOverPanel.AddComponent<Image>();
        img.color = img.color.a < 0.01f ? new Color(0f, 0f, 0f, 0.01f) : img.color; // 保持透明但可拦截
        img.raycastTarget = true; // 确保能拦截点击
    }

    private void EnsureGraphicRaycaster()
    {
        if (gameOverPanel == null) return;
        var canvas = gameOverPanel.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsurePanelCanvasGroup(GameObject panel)
    {
        if (panel == null) return;
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.ignoreParentGroups = false;
    }

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

    private void TryAutoWireFromHierarchy()
    {
        if (gameOverPanel == null) return;

        // 文本自动绑定
        if (titleText == null || detailText == null || titleTextTMP == null || detailTextTMP == null)
        {
            var texts = gameOverPanel.GetComponentsInChildren<Text>(includeInactive: true);
            foreach (var t in texts)
            {
                var name = t.gameObject.name.ToLower();
                if (titleText == null && name.Contains("title")) titleText = t;
                else if (detailText == null && name.Contains("detail")) detailText = t;
            }
            // 兜底：如果还为空，挑选第一个/第二个 Text
            if (titleText == null && texts.Length > 0) titleText = texts[0];
            if (detailText == null && texts.Length > 1) detailText = texts[1];

            // 反射查找 TMP_Text（避免强依赖 TextMeshPro 包）
            var allComps = gameOverPanel.GetComponentsInChildren<Component>(includeInactive: true);
            foreach (var c in allComps)
            {
                if (c == null) continue;
                var typeName = c.GetType().Name; // "TMP_Text" 或其它
                if (typeName == "TMP_Text")
                {
                    var n = c.gameObject.name.ToLower();
                    if (titleTextTMP == null && n.Contains("title")) { titleTextTMP = c; continue; }
                    if (detailTextTMP == null && n.Contains("detail")) { detailTextTMP = c; continue; }
                }
            }
            if (titleTextTMP == null)
            {
                foreach (var c in allComps)
                {
                    if (c != null && c.GetType().Name == "TMP_Text") { titleTextTMP = c; break; }
                }
            }
            if (detailTextTMP == null)
            {
                bool skippedFirst = false;
                foreach (var c in allComps)
                {
                    if (c != null && c.GetType().Name == "TMP_Text")
                    {
                        if (!skippedFirst) { skippedFirst = true; continue; }
                        detailTextTMP = c; break;
                    }
                }
            }
        }

        // 按钮自动绑定
        if (restartButton == null || mainMenuButton == null || quitButton == null)
        {
            var btns = gameOverPanel.GetComponentsInChildren<Button>(includeInactive: true);
            foreach (var b in btns)
            {
                var name = b.gameObject.name.ToLower();
                if (restartButton == null && (name.Contains("restart") || name.Contains("retry") || name.Contains("again"))) restartButton = b;
                else if (mainMenuButton == null && (name.Contains("mainmenu") || name.Contains("menu") || name == "main")) mainMenuButton = b;
                else if (quitButton == null && (name.Contains("quit") || name.Contains("exit") || name.Contains("close"))) quitButton = b;
            }
        }

        // 若新绑定了按钮，补充监听与样式
        WireButtons();
    }

    // 反射设置 TMP 文本
    private void SetTMPTextActiveAndValue(Component tmp, string value, bool active)
    {
        if (tmp == null) return;
        var go = tmp.gameObject;
        if (go != null) go.SetActive(active);
        var prop = tmp.GetType().GetProperty("text");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(tmp, value, null);
        }
    }

    private void ApplyGameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gameplayCursorApplied = true;
    }
}
