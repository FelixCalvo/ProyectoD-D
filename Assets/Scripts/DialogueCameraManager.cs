using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Gestiona el cambio de cámaras Cinemachine durante los diálogos y el player activo.
/// Funciona tanto para singleplayer como multiplayer usando GameObjects pre-colocados.
/// </summary>
public class DialogueCameraManager : MonoBehaviour
{
    public static DialogueCameraManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerCamera
    {
        public GameObject player;
        public CinemachineCamera cinemachineCamera;
    }

    [Header("Configuración de Prioridades")]
    [SerializeField] private int dialogueCameraPriority = 20;
    [SerializeField] private int activePlayerCameraPriority = 10;
    [SerializeField] private int inactivePlayerCameraPriority = 0;

    [Header("Cámaras de Players")]
    [Tooltip("Asigna los GameObjects de jugadores y sus cámaras Cinemachine")]
    [SerializeField] private PlayerCamera[] playerCameras;
    
    private CinemachineCamera currentDialogueCamera;
    private GameObject lastActivePlayer;

    /// <summary>
    /// True si hay una cámara de diálogo activa (hay un diálogo en curso)
    /// </summary>
    public bool IsDialogueActive => currentDialogueCamera != null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Update()
    {
        // MULTIPLAYER: Las cámaras de players se gestionan en Player.UpdateLocalPlayerCamera()
        // Este manager solo gestiona cámaras de diálogo
        // Solo actualizar en SINGLEPLAYER (cuando no hay NetworkRunner activo)
        if (!IsDialogueActive && !IsMultiplayerActive())
        {
            UpdatePlayerCameraPriorities();
        }
    }

    /// <summary>
    /// Verifica si estamos en modo multiplayer
    /// </summary>
    private bool IsMultiplayerActive()
    {
        // Buscar si hay un NetworkRunner activo (indica partida multiplayer)
        return Fusion.NetworkRunner.Instances.Count > 0;
    }

    void UpdatePlayerCameraPriorities()
    {
        GameObject activePlayer = HelperClass.ActivePlayer;

        // Solo actualizar si cambió el player activo
        if (activePlayer == lastActivePlayer) return;

        lastActivePlayer = activePlayer;

        if (activePlayer == null) return;

        foreach (PlayerCamera pc in playerCameras)
        {
            if (pc.cinemachineCamera == null) continue;

            if (pc.player == activePlayer)
            {
                pc.cinemachineCamera.Priority = activePlayerCameraPriority;
                //Debug.Log($"Cámara de {pc.player.name} activada (Prioridad: {activePlayerCameraPriority})");
            }
            else
            {
                pc.cinemachineCamera.Priority = inactivePlayerCameraPriority;
            }
        }
    }

    /// <summary>
    /// Activa una cámara virtual específica para diálogo (mayor prioridad que players).
    /// </summary>
    public void ActivateCamera(CinemachineCamera camera)
    {
        if (camera == null) return;

        // Desactivar cámara anterior si existe
        if (currentDialogueCamera != null)
        {
            currentDialogueCamera.Priority = inactivePlayerCameraPriority;
        }

        // Activar nueva cámara de diálogo (prioridad más alta)
        camera.Priority = dialogueCameraPriority;
        currentDialogueCamera = camera;
    }

    /// <summary>
    /// Vuelve a la cámara del player activo (desactiva cámara de diálogo).
    /// </summary>
    public void DeactivateDialogueCamera()
    {
        if (currentDialogueCamera != null)
        {
            currentDialogueCamera.Priority = inactivePlayerCameraPriority;
            currentDialogueCamera = null;
        }

        // Forzar actualización de cámaras de players
        lastActivePlayer = null;
        UpdatePlayerCameraPriorities();
    }
}
