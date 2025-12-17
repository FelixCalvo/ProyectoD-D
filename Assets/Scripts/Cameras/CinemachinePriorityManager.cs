using UnityEngine;
using Unity.Cinemachine;

public class CinemachinePriorityManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerCamera
    {
        public GameObject player;
        public CinemachineCamera cinemachineCamera;
    }

    [Header("Configuración de Cámaras")]
    public PlayerCamera[] playerCameras;

    [Header("Prioridades")]
    public int activePriority = 10;
    public int inactivePriority = 0;

    private GameObject lastActivePlayer;

    void Update()
    {
        // Comprobar si el player activo ha cambiado
        if (HelperClass.ActivePlayer != lastActivePlayer)
        {
            UpdateCameraPriorities();
            lastActivePlayer = HelperClass.ActivePlayer;
        }
    }

    void UpdateCameraPriorities()
    {
        GameObject activePlayer = HelperClass.ActivePlayer;

        if (activePlayer == null) return;

        foreach (PlayerCamera pc in playerCameras)
        {
            if (pc.cinemachineCamera == null) continue;

            if (pc.player == activePlayer)
            {
                // Dar máxima prioridad a la cámara del player activo
                pc.cinemachineCamera.Priority = activePriority;
                Debug.Log($"Cámara de {pc.player.name} activada (Prioridad: {activePriority})");
            }
            else
            {
                // Bajar prioridad de las demás cámaras
                pc.cinemachineCamera.Priority = inactivePriority;
            }
        }
    }

    // Método público para forzar actualización si se necesita
    public void ForceUpdate()
    {
        UpdateCameraPriorities();
    }
}
