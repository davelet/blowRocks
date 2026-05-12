using UnityEngine;
using TMPro;

/// <summary>
/// Attach to a TextMeshProUGUI GameObject to auto-localize its text.
/// Set the localizationKey in the Inspector (e.g. "pause.title").
/// Text updates automatically when language changes.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizeText : MonoBehaviour
{
    [Tooltip("Localization key, e.g. 'pause.resume', 'hud.score'")]
    public string localizationKey;

    [Tooltip("Optional format string. Use {0} for the localized text. Example: '{0}: 0'")]
    public string formatOverride;

    private TextMeshProUGUI tmpText;

    private void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    private void UpdateText()
    {
        if (tmpText == null || string.IsNullOrEmpty(localizationKey)) return;

        string localized = LocalizationManager.Get(localizationKey);
        if (!string.IsNullOrEmpty(formatOverride))
            tmpText.text = string.Format(formatOverride, localized);
        else
            tmpText.text = localized;
    }

    /// <summary>
    /// Change the key at runtime (e.g. from code).
    /// </summary>
    public void SetKey(string key)
    {
        localizationKey = key;
        UpdateText();
    }
}
