using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestor simple de localización.
/// Carga archivos JSON desde Resources/Localization/
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    private static LocalizationManager _instance;
    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("LocalizationManager");
                _instance = go.AddComponent<LocalizationManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Configuración")]
    [Tooltip("Idioma actual (es, en, ca, etc.)")]
    [SerializeField] private string currentLanguage = "es";

    private Dictionary<string, string> localizedTexts = new Dictionary<string, string>();
    private bool isLoaded = false;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Cargar idioma guardado o usar el del sistema
        string savedLanguage = PlayerPrefs.GetString("Language", "");
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            currentLanguage = savedLanguage;
        }
        else
        {
            // Detectar idioma del sistema
            string systemLang = Application.systemLanguage.ToString().ToLower();
            if (systemLang.StartsWith("spanish"))
                currentLanguage = "es";
            else if (systemLang.StartsWith("catalan"))
                currentLanguage = "ca";
            else
                currentLanguage = "en";
        }

        LoadLanguage(currentLanguage);
    }

    public void LoadLanguage(string languageCode)
    {
        currentLanguage = languageCode;
        PlayerPrefs.SetString("Language", languageCode);

        localizedTexts.Clear();
        isLoaded = false;

        TextAsset jsonFile = Resources.Load<TextAsset>($"Localization/{languageCode}");

        if (jsonFile == null)
        {
            Debug.LogError($"❌ Archivo no encontrado: Resources/Localization/{languageCode}.json");
            return;
        }

        //Debug.Log($"📄 JSON cargado: {jsonFile.text}");

        // Parsear JSON línea por línea
        string jsonText = jsonFile.text;
        
        // Quitar llaves externas y espacios
        jsonText = jsonText.Replace("{", "").Replace("}", "").Trim();
        
        // Dividir por líneas y procesar cada una
        string[] lines = jsonText.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim().TrimEnd(',');
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;
            int colonIndex = trimmedLine.IndexOf(':');
            if (colonIndex > 0)
            {
                string key = trimmedLine.Substring(0, colonIndex).Trim().Trim('"').ToUpperInvariant();
                string value = trimmedLine.Substring(colonIndex + 1).Trim().Trim('"');
                localizedTexts[key] = value;
                //Debug.Log($"   ✓ [{key}] = {value}");
            }
        }

        isLoaded = true;
        //Debug.Log($"✅ Idioma {languageCode} cargado: {localizedTexts.Count} traducciones");
    }

    public string GetText(string key)
    {
        // Asegurar que esté cargado
        if (!isLoaded)
        {
            Debug.LogWarning($"⚠️ LocalizationManager no iniciado. Cargando {currentLanguage}...");
            LoadLanguage(currentLanguage);
        }

        string keyUpper = key.ToUpperInvariant();
        if (localizedTexts.TryGetValue(keyUpper, out string value))
        {
            //Debug.Log($"🔍 GetText('{key}') → '{value}'");
            return value;
        }

        Debug.LogWarning($"❌ Clave no encontrada: '{keyUpper}'. Claves disponibles: [" + string.Join(", ", localizedTexts.Keys) + "]");
        return $"[{key}]";
    }

    public void SetLanguage(string lang)
    {
        LoadLanguage(lang);
    }
}