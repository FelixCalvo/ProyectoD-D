using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class ListaPartidasUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject partidaItemPrefab;
    [SerializeField] private Transform contenedorPartidas; // El panel donde se generarán las partidas
    
    private List<GameObject> partidasInstanciadas = new List<GameObject>();

    // Método para actualizar la lista de partidas
    public void ActualizarLista(List<SessionInfo> sesiones, System.Action<string> onUnirseCallback)
    {
        // Limpiar partidas anteriores
        LimpiarLista();

        // Crear un item por cada sesión
        foreach (var sesion in sesiones)
        {
            GameObject itemGO = Instantiate(partidaItemPrefab, contenedorPartidas);
            PartidaItemUI itemUI = itemGO.GetComponent<PartidaItemUI>();
            
            if (itemUI != null)
            {
                itemUI.ConfigurarConInfo(sesion.Name, sesion.PlayerCount, sesion.MaxPlayers, onUnirseCallback);
            }

            partidasInstanciadas.Add(itemGO);
        }
        
        // Forzar reconstrucción del layout después de instanciar todos los items
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contenedorPartidas as RectTransform);
    }

    // Limpiar todas las partidas de la lista
    public void LimpiarLista()
    {
        foreach (var item in partidasInstanciadas)
        {
            if (item != null)
                Destroy(item);
        }
        partidasInstanciadas.Clear();
    }

    private void OnDestroy()
    {
        LimpiarLista();
    }
}
