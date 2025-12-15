using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema que captura automáticamente el texto de cualquier comando Menu de Fungus
/// y lo guarda para que LogSelectedMenu pueda recuperarlo.
/// NO necesitas usar un comando especial, funciona con Menu normal.
/// </summary>
public class MenuJournalTracker : MonoBehaviour
{
    private static MenuJournalTracker instance;
    public static MenuJournalTracker Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[MenuJournalTracker]");
                instance = go.AddComponent<MenuJournalTracker>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Diccionario estático para guardar el texto de cada opción de menú
    private Dictionary<Fungus.Block, string> menuTexts = new Dictionary<Fungus.Block, string>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Registra el texto de un menú asociado a un bloque de destino.
    /// </summary>
    public void RegisterMenuText(Fungus.Block targetBlock, string menuText)
    {
        if (targetBlock != null && !string.IsNullOrEmpty(menuText))
        {
            menuTexts[targetBlock] = menuText;
        }
    }

    /// <summary>
    /// Obtiene y elimina el texto del menú asociado a un bloque.
    /// </summary>
    public string GetAndClearMenuText(Fungus.Block block)
    {
        if (block != null && menuTexts.ContainsKey(block))
        {
            string text = menuTexts[block];
            menuTexts.Remove(block);
            return text;
        }
        return null;
    }
}
