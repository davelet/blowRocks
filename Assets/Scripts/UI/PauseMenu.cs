using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Pause Panel Texts")]
    [SerializeField] private TextMeshProUGUI pauseTitleText;
    [SerializeField] private TextMeshProUGUI resumeText;
    [SerializeField] private TextMeshProUGUI settingsText;
    [SerializeField] private TextMeshProUGUI quitText;

    [Header("Settings Panel Texts")]
    [SerializeField] private TextMeshProUGUI settingsTitleText;
    [SerializeField] private TextMeshProUGUI volumeLabelText;
    [SerializeField] private TextMeshProUGUI volumeValueText;
    [SerializeField] private TextMeshProUGUI backText;
    [SerializeField] private TextMeshProUGUI langButtonText;

    [Header("Controls")]
    [SerializeField] private Slider volumeSlider;

    private bool isPaused = false;
    private GameManager gameManager;

    public System.Action<bool> OnPauseChanged;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        WireButtons();
        UpdateAllTexts();
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
            WireButton(pausePanel, "QuitButton", QuitGame);
        }
        if (settingsPanel != null)
        {
            WireButton(settingsPanel, "BackButton", CloseSettings);
            WireButton(settingsPanel, "LangButton", OnLanguageToggle);
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

    private void UpdateAllTexts()
    {
        if (pauseTitleText != null) pauseTitleText.text = LocalizationManager.Get("pause.title");
        if (resumeText != null) resumeText.text = LocalizationManager.Get("pause.resume");
        if (settingsText != null) settingsText.text = LocalizationManager.Get("pause.settings");
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
    }

    private void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
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
        OnPauseChanged?.Invoke(false);
    }

    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (volumeSlider != null) volumeSlider.value = AudioListener.volume;
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    private void OnLanguageToggle()
    {
        LocalizationManager.ToggleLanguage();
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