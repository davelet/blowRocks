using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Lightweight localization manager for Chinese/English.
/// Singleton — persists across scene loads. Call LocalizationManager.SetLanguage() to switch.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public enum Language { English, Chinese }

    public static LocalizationManager Instance { get; private set; }

    public static Language CurrentLanguage { get; private set; } = Language.English;

    public static event System.Action OnLanguageChanged;

    private static readonly Dictionary<string, Dictionary<Language, string>> Strings = new()
    {
        // --- Pause Menu ---
        ["pause.title"] = new() {
            { Language.English, "PAUSED" },
            { Language.Chinese, "已暂停" }
        },
        ["pause.resume"] = new() {
            { Language.English, "Resume" },
            { Language.Chinese, "继续游戏" }
        },
        ["pause.settings"] = new() {
            { Language.English, "Settings" },
            { Language.Chinese, "设置" }
        },
        ["pause.quit"] = new() {
            { Language.English, "Quit" },
            { Language.Chinese, "退出游戏" }
        },

        // --- Settings ---
        ["settings.title"] = new() {
            { Language.English, "SETTINGS" },
            { Language.Chinese, "设置" }
        },
        ["settings.volume"] = new() {
            { Language.English, "Volume" },
            { Language.Chinese, "音量" }
        },
        ["settings.back"] = new() {
            { Language.English, "Back" },
            { Language.Chinese, "返回" }
        },
        ["settings.language"] = new() {
            { Language.English, "Language" },
            { Language.Chinese, "语言" }
        },

        // --- HUD ---
        ["hud.score"] = new() {
            { Language.English, "Score" },
            { Language.Chinese, "得分" }
        },
        ["hud.lives"] = new() {
            { Language.English, "Lives" },
            { Language.Chinese, "生命" }
        },

        // --- Game Over ---
        ["gameover.title"] = new() {
            { Language.English, "GAME OVER" },
            { Language.Chinese, "游戏结束" }
        },
        ["gameover.finalScore"] = new() {
            { Language.English, "Final Score" },
            { Language.Chinese, "最终得分" }
        },
        ["gameover.restart"] = new() {
            { Language.English, "Restart" },
            { Language.Chinese, "重新开始" }
        },

        // --- Language names (for the toggle button) ---
        ["lang.english"] = new() {
            { Language.English, "English" },
            { Language.Chinese, "英文" }
        },
        ["lang.chinese"] = new() {
            { Language.English, "中文" },
            { Language.Chinese, "中文" }
        },
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Get a localized string by key.
    /// </summary>
    public static string Get(string key)
    {
        if (Strings.TryGetValue(key, out var langs))
        {
            if (langs.TryGetValue(CurrentLanguage, out var text))
                return text;
            // Fallback to English
            if (langs.TryGetValue(Language.English, out var fallback))
                return fallback;
        }
        return key; // Return key itself as last resort
    }

    /// <summary>
    /// Switch language and notify all listeners.
    /// </summary>
    public static void SetLanguage(Language lang)
    {
        if (CurrentLanguage == lang) return;
        CurrentLanguage = lang;
        OnLanguageChanged?.Invoke();
        Debug.Log($"[Localization] Language changed to: {lang}");
    }

    /// <summary>
    /// Toggle between English and Chinese.
    /// </summary>
    public static void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == Language.English ? Language.Chinese : Language.English);
    }

    /// <summary>
    /// Check if current language is Chinese.
    /// </summary>
    public static bool IsChinese => CurrentLanguage == Language.Chinese;
}
