using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject aboutPanel;

    [Header("Pause Panel Texts")]
    [SerializeField] private TextMeshProUGUI pauseTitleText;
    [SerializeField] private TextMeshProUGUI resumeText;
    [SerializeField] private TextMeshProUGUI settingsText;
    [SerializeField] private TextMeshProUGUI aboutText;
    [SerializeField] private TextMeshProUGUI quitText;

    [Header("Settings Panel Texts")]
    [SerializeField] private TextMeshProUGUI settingsTitleText;
    [SerializeField] private TextMeshProUGUI volumeLabelText;
    [SerializeField] private TextMeshProUGUI volumeValueText;
    [SerializeField] private TextMeshProUGUI backText;
    [SerializeField] private TextMeshProUGUI langButtonText;
    [SerializeField] private TextMeshProUGUI fullscreenLabelText;

    [Header("About Panel Texts")]
    [SerializeField] private TextMeshProUGUI aboutTitleText;
    [SerializeField] private TextMeshProUGUI aboutVersionText;
    [SerializeField] private TextMeshProUGUI aboutBackText;

    [Header("Controls")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    private bool isPaused = false;
    private GameManager gameManager;
    private TextMeshProUGUI _fullscreenStateText; // 保存switch状态文本引用

    public System.Action<bool> OnPauseChanged;

    private void Awake()
    {
        Instance = this;
        gameManager = GameManager.Instance;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);

        AdjustUILayout();
        WireButtons();
        UpdateAllTexts();
    }

    private void AdjustUILayout()
    {
        // 1. 把暂停标题往上挪80像素，避免被按钮挡住
        if (pauseTitleText != null)
        {
            var rect = pauseTitleText.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 120f);
        }

        // 2. 把全屏复选框改成switch样式
        if (fullscreenToggle != null)
        {
            // 调整Toggle尺寸为switch大小
            var toggleRect = fullscreenToggle.GetComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(60f, 30f);

            // 隐藏默认的复选框图形
            var background = fullscreenToggle.transform.Find("Background");
            if (background != null) background.gameObject.SetActive(false);
            var checkmark = fullscreenToggle.transform.Find("Checkmark");
            if (checkmark != null) checkmark.gameObject.SetActive(false);

            // 添加switch背景
            var switchBg = new GameObject("SwitchBg");
            switchBg.transform.SetParent(fullscreenToggle.transform, false);
            var bgImage = switchBg.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            var bgRect = bgImage.rectTransform;
            bgRect.sizeDelta = toggleRect.sizeDelta;
            bgRect.anchoredPosition = Vector2.zero;

            // 添加switch滑块
            var switchKnob = new GameObject("SwitchKnob");
            switchKnob.transform.SetParent(fullscreenToggle.transform, false);
            var knobImage = switchKnob.AddComponent<Image>();
            knobImage.color = Color.white;
            var knobRect = knobImage.rectTransform;
            knobRect.sizeDelta = new Vector2(26f, 26f);
            knobRect.anchorMin = new Vector2(0f, 0.5f);
            knobRect.anchorMax = new Vector2(0f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);

            // 调大switch宽度，避免文字超出
            toggleRect.sizeDelta = new Vector2(70f, 30f);
            
            // 添加状态文本
            var stateText = new GameObject("StateText");
            stateText.transform.SetParent(fullscreenToggle.transform, false);
            _fullscreenStateText = stateText.AddComponent<TextMeshProUGUI>();
            _fullscreenStateText.fontSize = 12f; // 减小字号避免超出
            var textRect = _fullscreenStateText.rectTransform;
            textRect.sizeDelta = new Vector2(30f, 30f);

            // 保存引用，用于状态更新
            fullscreenToggle.onValueChanged.AddListener(isOn =>
            {
                knobRect.anchoredPosition = isOn ? new Vector2(52f, 0f) : new Vector2(18f, 0f);
                bgImage.color = isOn ? new Color(0f, 0.6f, 0.2f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);
                UpdateFullscreenStateText();
                
                // 文字放到没有滑块的那一边
                if (isOn)
                {
                    // 滑块在右边，文字放左边
                    _fullscreenStateText.alignment = TextAlignmentOptions.Left;
                    textRect.anchorMin = new Vector2(0f, 0.5f);
                    textRect.anchorMax = new Vector2(0f, 0.5f);
                    textRect.anchoredPosition = new Vector2(8f, 0f);
                }
                else
                {
                    // 滑块在左边，文字放右边
                    _fullscreenStateText.alignment = TextAlignmentOptions.Right;
                    textRect.anchorMin = new Vector2(1f, 0.5f);
                    textRect.anchorMax = new Vector2(1f, 0.5f);
                    textRect.anchoredPosition = new Vector2(-8f, 0f);
                }
            });

            // 初始化状态
            fullscreenToggle.isOn = SettingsManager.Fullscreen;
        }
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateAllTexts;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateAllTexts;
    }

    private void WireButtons()
    {
        if (pausePanel != null)
        {
            WireButton(pausePanel, "ResumeButton", Resume);
            WireButton(pausePanel, "SettingsButton", OpenSettings);
            WireButton(pausePanel, "AboutButton", OpenAbout);
            WireButton(pausePanel, "QuitButton", QuitGame);
        }
        if (settingsPanel != null)
        {
            WireButton(settingsPanel, "BackButton", CloseSettings);
            WireButton(settingsPanel, "LangButton", OnLanguageToggle);
        }

        if (aboutPanel != null)
        {
            WireButton(aboutPanel, "AboutBackButton", CloseAbout);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = SettingsManager.Fullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
    }

    private void WireButton(GameObject root, string buttonName, UnityEngine.Events.UnityAction action)
    {
        var t = FindDeepChild(root.transform, buttonName);
        if (t != null)
        {
            var btn = t.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(action);
        }
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void UpdateFullscreenStateText()
    {
        if (_fullscreenStateText != null && fullscreenToggle != null)
        {
            _fullscreenStateText.text = fullscreenToggle.isOn 
                ? LocalizationManager.Get("settings.on") 
                : LocalizationManager.Get("settings.off");
        }
    }

    private void UpdateAllTexts()
    {
        if (pauseTitleText != null) pauseTitleText.text = LocalizationManager.Get("pause.title");
        if (resumeText != null) resumeText.text = LocalizationManager.Get("pause.resume");
        if (settingsText != null) settingsText.text = LocalizationManager.Get("pause.settings");
        if (aboutText != null) aboutText.text = LocalizationManager.Get("pause.about");
        if (quitText != null) quitText.text = LocalizationManager.Get("pause.quit");
        if (settingsTitleText != null) settingsTitleText.text = LocalizationManager.Get("settings.title");
        if (volumeLabelText != null) volumeLabelText.text = LocalizationManager.Get("settings.volume");
        if (backText != null) backText.text = LocalizationManager.Get("settings.back");

        if (langButtonText != null)
        {
            langButtonText.text = LocalizationManager.IsChinese
                ? LocalizationManager.Get("lang.english")
                : LocalizationManager.Get("lang.chinese");
        }

        if (fullscreenLabelText != null)
            fullscreenLabelText.text = LocalizationManager.Get("settings.fullscreen");

        // 切换语言时更新switch状态文本
        UpdateFullscreenStateText();

        if (aboutTitleText != null)
            aboutTitleText.text = LocalizationManager.Get("about.title");
        if (aboutVersionText != null)
            aboutVersionText.text = $"blowRocks v{Application.version}";
        if (aboutBackText != null)
            aboutBackText.text = LocalizationManager.Get("about.back");
    }

    private void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = SettingsManager.Volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameManager != null && gameManager.IsGameOver) return;

            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else if (aboutPanel != null && aboutPanel.activeSelf)
            {
                CloseAbout();
            }
            else
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        OnPauseChanged?.Invoke(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
        OnPauseChanged?.Invoke(false);
    }

    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (volumeSlider != null) volumeSlider.value = SettingsManager.Volume;
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void OpenAbout()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(true);
    }

    public void CloseAbout()
    {
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    private void OnVolumeChanged(float value)
    {
        SettingsManager.Volume = value; // 内部会设置 AudioListener.volume 并保存
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    private void OnLanguageToggle()
    {
        LocalizationManager.ToggleLanguage();
    }

    private void OnFullscreenChanged(bool isOn)
    {
        SettingsManager.Fullscreen = isOn;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}