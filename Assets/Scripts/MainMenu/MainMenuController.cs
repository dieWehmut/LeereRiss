using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// MainMenu 的控制器：把 UI 输入写入 MazeSettings，并在开始时加载迷宫场景。
/// 把这个脚本挂在 MainMenu 场景的一个 GameObject（例如 MainMenuManager）上，然后在 Inspector 中绑定 InputField/Dropdown 和 Start 按钮。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Input fields for maze size (positive integers)")]
    // 这些 InputField 用于直接输入数值（也会在 Difficulty 面板中显示）
    public InputField widthInput;
    public InputField heightInput;
    public InputField depthInput;

    [Header("Difficulty panel UI (pop-up)")]
    public GameObject difficultyPanel; // 面板，用于弹出三个参数的输入/滑条
    public Slider widthSlider;
    public Slider depthSlider;
    public Slider heightSlider;
    
    [Header("Buttons that should remain clickable when panel is open")]
    public Button startButton; // 将 Start Button 拖到这里
    public Button difficultyToggleButton; // 面板触发按钮（用于折叠）
    public Button exitButton; // Exit 按钮（可选,在 StartBtn 下方）
    public Button settingsButton; // 设置按钮
    
    [Header("Info panel (game instructions)")]
    public Button infoButton; // 点击显示说明弹窗
    public GameObject infoPanel; // 弹窗根对象（里面包含 Text 和 Close 按钮）
    public Text infoText; // 用于显示说明的正文 Text
    [TextArea(6, 20)] public string infoContent = "Game Info:\n\nControls:\n- WASD: Move\n- Space: Jump\n- V: Toggle first- / third-person view\n- C: Switch fire mode\n- G: Cycle guide hints\n- Mouse Move: Aim\n- Left Mouse Button: Shoot\n- Right Mouse Button: Pause / open menu\n\nObjective:\n- Reach the maze exit to win\n- Losing all health results in defeat\n\nTips:\n- Use cover and swap views to stay aware of enemies\n- Tweak maze size in the Difficulty panel to change the challenge";
    
    [Header("Settings panel (fullscreen & volume controls)")]
    public GameObject settingsPanel; // 设置面板根对象
    public Toggle fullscreenToggle; // 全屏切换开关
    public Slider volumeSlider; // 音量滑条
    public Button closeSettingsButton; // 关闭设置面板按钮

    [Header("Optional: show final applied sizes in the UI")]
    public Text willGenerateText; // e.g. a small label under difficulty panel

    [Header("Optional: difficulty dropdown (0=Easy,1=Medium,2=Hard)")]
    public Dropdown difficultyDropdown;

    [Header("Scene to load when Start Game is clicked")]
    public string sceneToLoad = "GameScene"; // 确保场景已加入 Build Settings，或者修改为实际场景名

    private void Start()
    {
        // 防御式：若从暂停/结算返回主菜单，确保输入阻塞被解除
        InputBlocker.DisableModalBlock();
        // 主菜单期望显示并释放鼠标
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 初始化输入框为当前设置
        if (widthInput) widthInput.text = MazeSettings.Width.ToString();
        if (heightInput) heightInput.text = MazeSettings.Height.ToString();
        if (depthInput) depthInput.text = MazeSettings.Depth.ToString();

        // 如果绑定了下拉，确保事件连通。下拉的 OnValueChanged 应该指向 OnDifficultyDropdownChanged
        if (difficultyDropdown)
        {
            // 保证有合理范围
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyDropdownChanged);
        }

        // 初始隐藏难度面板（如果已绑定）
        if (difficultyPanel) difficultyPanel.SetActive(false);

        // 如果滑条与输入框都绑定了，确保它们初始同步
        if (widthSlider)
        {
            widthSlider.wholeNumbers = true;
            widthSlider.minValue = MazeSettings.MinWidth;
            if (widthSlider.maxValue < widthSlider.minValue) widthSlider.maxValue = MazeSettings.MaxWidth;
            widthSlider.onValueChanged.AddListener(OnWidthSliderChanged);
        }
        if (depthSlider)
        {
            depthSlider.wholeNumbers = true;
            depthSlider.minValue = MazeSettings.MinDepth;
            if (depthSlider.maxValue < depthSlider.minValue) depthSlider.maxValue = MazeSettings.MaxDepth;
            depthSlider.onValueChanged.AddListener(OnDepthSliderChanged);
        }
        if (heightSlider)
        {
            heightSlider.wholeNumbers = true;
            heightSlider.minValue = MazeSettings.MinHeight;
            if (heightSlider.maxValue < heightSlider.minValue) heightSlider.maxValue = MazeSettings.MaxHeight;
            heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);
        }

        // 如果输入框没有可见标签/占位文本，运行时自动创建占位提示（方便快速测试/避免编辑器漏配）
        EnsurePlaceholder(widthInput, "Width (min 8)");
        EnsurePlaceholder(depthInput, "Depth (min 8)");
        EnsurePlaceholder(heightInput, "Height (min 5)");

    // 初始隐藏信息面板
    if (infoPanel) infoPanel.SetActive(false);
    if (infoText != null) infoText.text = infoContent;
    
        // 初始隐藏设置面板
        if (settingsPanel) settingsPanel.SetActive(false);
        
        // 初始化全屏设置（从 PlayerPrefs 加载）
        if (fullscreenToggle != null)
        {
            bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            Screen.fullScreen = savedFullscreen;
            fullscreenToggle.isOn = savedFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        }
        
        // 初始化音量设置（从 PlayerPrefs 加载）
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            float savedVolume = PlayerPrefs.GetFloat("Volume", 1f);
            AudioListener.volume = savedVolume;
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        }
    }

    /// <summary>
    /// 被 Start 按钮调用：读取输入，应用到 MazeSettings，然后加载场景。
    /// </summary>
    public void StartGame()
    {
        // 在加载场景前先确保当前输入/面板设置都已应用
        ApplyDifficultySettings();

        // 加载场景前可在此处加入过渡或保存设置到 PlayerPrefs（已在 ApplyDifficultySettings 中处理）
        SceneManager.LoadScene(sceneToLoad);
    }

    private int ParseInputOrDefault(InputField input, int fallback)
    {
        if (input == null || string.IsNullOrEmpty(input.text)) return fallback;
        int val;
        if (int.TryParse(input.text, out val))
        {
            // 保证入参不会超出安全范围
            val = Mathf.Clamp(val, MazeSettings.MinWidth, MazeSettings.MaxWidth);
            return val;
        }
        return fallback;
    }

    // 切换难度设置面板（按钮绑定）。面板显示时会把 Start 和 Difficulty 按钮提升到 UI 顶层以保持可点击
    public void ShowDifficultyPanel()
    {
        if (difficultyPanel == null) return;

        bool willShow = !difficultyPanel.activeSelf;

        if (willShow)
        {
            // 初始化滑条与输入框显示为当前 MazeSettings（并强制最小值）
            int w = Mathf.Max(8, MazeSettings.Width);
            int d = Mathf.Max(8, MazeSettings.Depth);
            int h = Mathf.Max(5, MazeSettings.Height);

            if (widthSlider) widthSlider.value = Mathf.Clamp(w, (int)widthSlider.minValue, (int)widthSlider.maxValue);
            if (depthSlider) depthSlider.value = Mathf.Clamp(d, (int)depthSlider.minValue, (int)depthSlider.maxValue);
            if (heightSlider) heightSlider.value = Mathf.Clamp(h, (int)heightSlider.minValue, (int)heightSlider.maxValue);

            if (widthInput) widthInput.text = w.ToString();
            if (depthInput) depthInput.text = d.ToString();
            if (heightInput) heightInput.text = h.ToString();

            difficultyPanel.SetActive(true);

            // 隐藏主菜单的四个按钮（Start / Difficulty / Exit / Info），只显示弹窗内的操作按钮（Apply/Cancel）
            if (startButton != null) startButton.gameObject.SetActive(false);
            if (exitButton != null) exitButton.gameObject.SetActive(false);
            if (difficultyToggleButton != null) difficultyToggleButton.gameObject.SetActive(false);
            if (infoButton != null) infoButton.gameObject.SetActive(false);
            if (settingsButton != null) settingsButton.gameObject.SetActive(false);
        }
        else
        {
            difficultyPanel.SetActive(false);

            // 恢复其他按钮显示
            if (startButton != null) startButton.gameObject.SetActive(true);
            if (exitButton != null) exitButton.gameObject.SetActive(true);
            if (difficultyToggleButton != null) difficultyToggleButton.gameObject.SetActive(true);
            if (infoButton != null) infoButton.gameObject.SetActive(true);
            if (settingsButton != null) settingsButton.gameObject.SetActive(true);
        }
    }

    // 取消并关闭面板（按钮绑定）
    public void CancelDifficultySettings()
    {
        if (difficultyPanel) difficultyPanel.SetActive(false);

        // 恢复主菜单四个按钮显示
        if (startButton != null) startButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(true);
        if (difficultyToggleButton != null) difficultyToggleButton.gameObject.SetActive(true);
        if (infoButton != null) infoButton.gameObject.SetActive(true);
        if (settingsButton != null) settingsButton.gameObject.SetActive(true);
    }

    // 应用面板上的设置（按钮绑定）
    public void ApplyDifficultySettings()
    {
        int w = ParseInputOrDefault(widthInput, MazeSettings.Width);
        int d = ParseInputOrDefault(depthInput, MazeSettings.Depth);
        int h = ParseInputOrDefault(heightInput, MazeSettings.Height);

        w = Mathf.Clamp(w, MazeSettings.MinWidth, MazeSettings.MaxWidth);
        d = Mathf.Clamp(d, MazeSettings.MinDepth, MazeSettings.MaxDepth);
        h = Mathf.Clamp(h, MazeSettings.MinHeight, MazeSettings.MaxHeight);

        MazeSettings.Apply(w, h, d);

        // 同步滑条
        if (widthSlider) widthSlider.value = Mathf.Clamp(w, (int)widthSlider.minValue, (int)widthSlider.maxValue);
        if (depthSlider) depthSlider.value = Mathf.Clamp(d, (int)depthSlider.minValue, (int)depthSlider.maxValue);
        if (heightSlider) heightSlider.value = Mathf.Clamp(h, (int)heightSlider.minValue, (int)heightSlider.maxValue);

        if (difficultyPanel) difficultyPanel.SetActive(false);

        // 恢复主菜单四个按钮显示
        if (startButton != null) startButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(true);
        if (difficultyToggleButton != null) difficultyToggleButton.gameObject.SetActive(true);
        if (infoButton != null) infoButton.gameObject.SetActive(true);
        if (settingsButton != null) settingsButton.gameObject.SetActive(true);

        // 更新 UI 中显示的最终生成尺寸（如果有绑定文本）
        UpdateWillGenerateText();
    }

    // 滑条/输入联动回调
    public void OnWidthSliderChanged(float value)
    {
        if (widthInput) widthInput.text = ((int)value).ToString();
    }

    public void OnDepthSliderChanged(float value)
    {
        if (depthInput) depthInput.text = ((int)value).ToString();
    }

    public void OnHeightSliderChanged(float value)
    {
        if (heightInput) heightInput.text = ((int)value).ToString();
    }

    // 输入框更改时（绑定到 InputField OnEndEdit 或 OnValueChanged）
    public void OnWidthInputChanged(string text)
    {
        int v;
        if (int.TryParse(text, out v))
        {
            v = Mathf.Max(8, v);
            if (widthSlider) widthSlider.value = Mathf.Clamp(v, (int)widthSlider.minValue, (int)widthSlider.maxValue);
            widthInput.text = v.ToString();
        }
    }

    public void OnDepthInputChanged(string text)
    {
        int v;
        if (int.TryParse(text, out v))
        {
            v = Mathf.Max(8, v);
            if (depthSlider) depthSlider.value = Mathf.Clamp(v, (int)depthSlider.minValue, (int)depthSlider.maxValue);
            depthInput.text = v.ToString();
        }
    }

    public void OnHeightInputChanged(string text)
    {
        int v;
        if (int.TryParse(text, out v))
        {
            v = Mathf.Max(5, v);
            if (heightSlider) heightSlider.value = Mathf.Clamp(v, (int)heightSlider.minValue, (int)heightSlider.maxValue);
            heightInput.text = v.ToString();
        }
    }

    // 如果 InputField 没有 placeholder，会在其子对象中创建一个 Text 并设置为 placeholder
    private void EnsurePlaceholder(InputField input, string label)
    {
        if (input == null) return;

        // 如果已有 placeholder 并且是 Text，确保显示文本
        var existing = input.placeholder as Text;
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.text)) existing.text = label;
            return;
        }

        // 创建 placeholder Text
        GameObject go = new GameObject("Placeholder");
        go.transform.SetParent(input.transform, false);
        var txt = go.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        txt.alignment = TextAnchor.MiddleLeft;
        txt.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(10f, 0f);
        rt.offsetMax = new Vector2(-10f, 0f);

        input.placeholder = txt;
    }

    private void UpdateWillGenerateText()
    {
        if (willGenerateText == null) return;

        int w = MazeSettings.Width;
        int d = MazeSettings.Depth;
        int h = MazeSettings.Height;
        willGenerateText.text = $"Will generate: {w} × {h} × {d} (W×H×D)";
    }

    /// <summary>
    /// 被 difficulty 下拉或其他 UI 调用（index: 0=Easy,1=Medium,2=Hard）。
    /// 会同时更新输入框的数值。也可直接在 Inspector 用按钮分别调用 SetDifficultyPreset。
    /// </summary>
    public void SetDifficultyPreset(int index)
    {
        switch (index)
        {
            case 0: // Easy
                MazeSettings.Apply(6, 6, 6);
                break;
            case 1: // Medium
                MazeSettings.Apply(10, 10, 10);
                break;
            case 2: // Hard
                MazeSettings.Apply(20, 20, 20);
                break;
            default:
                MazeSettings.Apply(10, 10, 10);
                break;
        }

        // 更新输入框显示
        if (widthInput) widthInput.text = MazeSettings.Width.ToString();
        if (heightInput) heightInput.text = MazeSettings.Height.ToString();
        if (depthInput) depthInput.text = MazeSettings.Depth.ToString();
    }

    // 用于 Dropdown 的回调
    public void OnDifficultyDropdownChanged(int index)
    {
        SetDifficultyPreset(index);
    }

    /// <summary>
    /// 显示信息/说明弹窗（绑定到 Info 按钮的 OnClick）
    /// </summary>
    public void ShowInfoPanel()
    {
        if (infoPanel == null) return;
        infoPanel.SetActive(true);

        // 隐藏其他主要按钮，保留 Info 按钮以便关闭
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);
        if (difficultyToggleButton != null) difficultyToggleButton.gameObject.SetActive(false);
        if (infoButton != null) infoButton.gameObject.SetActive(false);
        if (settingsButton != null) settingsButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 关闭信息面板（绑定到面板 Close 按钮）
    /// </summary>
    public void CloseInfoPanel()
    {
        if (infoPanel == null) return;
        infoPanel.SetActive(false);

        // 恢复其他主要按钮显示
        if (startButton != null) startButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(true);
        if (difficultyToggleButton != null) difficultyToggleButton.gameObject.SetActive(true);
        if (infoButton != null) infoButton.gameObject.SetActive(true);
        if (settingsButton != null) settingsButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 退出游戏（UI OnClick 绑定）。在编辑器中会停止播放模式，发布后会退出应用。
    /// </summary>
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 显示设置面板（绑定到 Settings 按钮的 OnClick）
    /// </summary>
    public void ShowSettingsPanel()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(true);

        // 隐藏所有主菜单按钮（Start / Difficulty / Exit / Info / Settings）
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);
        if (difficultyToggleButton != null) difficultyToggleButton.gameObject.SetActive(false);
        if (infoButton != null) infoButton.gameObject.SetActive(false);
        if (settingsButton != null) settingsButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 关闭设置面板（绑定到设置面板内的 Close 按钮）
    /// </summary>
    public void CloseSettingsPanel()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(false);

        // 恢复所有主菜单按钮显示
        if (startButton != null) startButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(true);
        if (difficultyToggleButton != null) difficultyToggleButton.gameObject.SetActive(true);
        if (infoButton != null) infoButton.gameObject.SetActive(true);
        if (settingsButton != null) settingsButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 全屏开关变化回调
    /// </summary>
    public void OnFullscreenToggleChanged(bool isOn)
    {
        Screen.fullScreen = isOn;
        PlayerPrefs.SetInt("Fullscreen", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 音量滑条变化回调
    /// </summary>
    public void OnVolumeSliderChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
    }
}
