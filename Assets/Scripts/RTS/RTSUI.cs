using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interfaz de usuario para el sistema RTS.
/// Muestra información de la unidad seleccionada y controles.
/// </summary>
public class RTSUI : MonoBehaviour
{
    // ===== UI ELEMENTS =====
    [Header("Unit Info")]
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private GameObject unitInfoPanel;
    
    [Header("Controls Help")]
    [SerializeField] private TextMeshProUGUI controlsText;
    
    void Start()
    {
        // Configurar texto de controles
        if (controlsText != null)
        {
            controlsText.text = 
                "<b>Controles:</b>\n" +
                "Click Izq: Seleccionar unidad\n" +
                "Click Der: Mover a posición\n" +
                "1-4: Selección rápida\n" +
                "Tab: Siguiente unidad";
        }
        
        // Ocultar panel por defecto
        if (unitInfoPanel != null)
        {
            unitInfoPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Actualiza la UI con información de la unidad seleccionada
    /// </summary>
    public void UpdateSelectedUnit(RTSUnit unit)
    {
        if (unit == null)
        {
            // Sin selección
            if (unitInfoPanel != null)
            {
                unitInfoPanel.SetActive(false);
            }
            return;
        }
        
        // Mostrar información de la unidad
        if (unitInfoPanel != null)
        {
            unitInfoPanel.SetActive(true);
        }
        
        if (unitNameText != null)
        {
            unitNameText.text = $"<b>{unit.UnitName}</b>";
        }
    }
    
    /// <summary>
    /// Muestra un mensaje temporal en pantalla
    /// </summary>
    public void ShowMessage(string message, float duration = 2f)
    {
        // TODO: Implementar sistema de mensajes temporales
        Debug.Log($"[UI] {message}");
    }
}
