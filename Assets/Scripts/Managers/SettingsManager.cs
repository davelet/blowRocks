using UnityEngine;
using System.IO;

/// <summary>
/// 跨平台用户配置管理器：保存音量和语言设置到 Application.persistentDataPath。
/// 自动创建、跨场景持久化、修改即保存。
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [System.Serializable]
    private class SettingsData
    {
        public float volume = 0.5f;
        public string language = "en"; // "en" 或 "zh"
        public bool fullscreen = false;
    }

    private static SettingsData data = new();
    private static string FilePath => Application.persistentDataPath + "/settings.json";

    public static float Volume
    {
        get => data.volume;
        set
        {
            data.volume = Mathf.Clamp01(value);
            AudioListener.volume = data.volume;
            Instance?.Save();
        }
    }

    public static string Language
    {
        get => data.language;
        set
        {
            if (data.language == value) return;
            data.language = value;
            Instance?.Save();
        }
    }

    public static bool Fullscreen
    {
        get => data.fullscreen;
        set
        {
            if (data.fullscreen == value) return;
            data.fullscreen = value;
            if (value)
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.ExclusiveFullScreen);
            }
            else
            {
                Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            }
            Instance?.Save();
        }
    }

    /// <summary>
    /// 在场景加载前自动创建设置管理器，确保最先初始化。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            var go = new GameObject("SettingsManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SettingsManager>();
            Instance.Load();
            AudioListener.volume = data.volume;
            Screen.fullScreen = data.fullscreen;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Load()
    {
        if (!File.Exists(FilePath)) return;
        try
        {
            string json = File.ReadAllText(FilePath);
            var loaded = JsonUtility.FromJson<SettingsData>(json);
            if (loaded != null) data = loaded;
            Debug.Log($"[SettingsManager] Loaded: volume={data.volume}, language={data.language}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SettingsManager] Failed to load settings: {e.Message}");
        }
    }

    private void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[SettingsManager] Saved: volume={data.volume}, language={data.language}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SettingsManager] Failed to save settings: {e.Message}");
        }
    }
}
