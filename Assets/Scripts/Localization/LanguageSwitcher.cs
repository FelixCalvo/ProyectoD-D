using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple: botones para cambiar idioma
/// </summary>
public class LanguageSwitcher : MonoBehaviour
{
    [SerializeField] private Button btnSpanish, btnEnglish, btnCatalan;

    void Start()
    {
        if (btnSpanish) btnSpanish.onClick.AddListener(() => SetLanguage("es"));
        if (btnEnglish) btnEnglish.onClick.AddListener(() => SetLanguage("en"));
        if (btnCatalan) btnCatalan.onClick.AddListener(() => SetLanguage("ca"));
    }

    void SetLanguage(string lang)
    {
        LocalizationManager.Instance.SetLanguage(lang);
    }
}
