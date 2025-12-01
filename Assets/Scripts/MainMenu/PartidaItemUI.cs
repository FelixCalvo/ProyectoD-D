using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PartidaItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nombrePartidaText;
    [SerializeField] private Button botonUnirse;

    private string nombrePartida;

    public void Configurar(string nombre, System.Action<string> callback)
    {
        nombrePartida = nombre;
        
        if (nombrePartidaText == null)
        {
            Debug.LogError("nombrePartidaText no está asignado en el prefab PartidaItemUI!");
            return;
        }
        
        nombrePartidaText.text = nombre;
        Debug.Log($"PartidaItemUI: Configurando nombre '{nombre}'");

        if (botonUnirse != null)
        {
            botonUnirse.onClick.RemoveAllListeners();
            botonUnirse.onClick.AddListener(() => callback(nombrePartida));
        }
    }

    public void ConfigurarConInfo(string nombre, int jugadoresActuales, int jugadoresMax, System.Action<string> callback)
    {
        nombrePartida = nombre;
        
        if (nombrePartidaText == null)
        {
            Debug.LogError("nombrePartidaText no está asignado en el prefab PartidaItemUI!");
            // Intentar encontrarlo automáticamente
            nombrePartidaText = GetComponentInChildren<TextMeshProUGUI>();
            if (nombrePartidaText == null)
            {
                Debug.LogError("No se pudo encontrar TextMeshProUGUI en los hijos del prefab!");
                return;
            }
        }
        
        string textoCompleto = $"{nombre} ({jugadoresActuales}/{jugadoresMax})";
        nombrePartidaText.text = textoCompleto;
        Debug.Log($"PartidaItemUI: Configurando '{textoCompleto}'");

        if (botonUnirse != null)
        {
            botonUnirse.onClick.RemoveAllListeners();
            botonUnirse.onClick.AddListener(() => callback(nombrePartida));
        }
        else
        {
            Debug.LogWarning("botonUnirse no está asignado en el prefab PartidaItemUI!");
        }
    }
}
