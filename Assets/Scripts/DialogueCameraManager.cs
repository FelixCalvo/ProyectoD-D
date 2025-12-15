using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Gestiona el cambio de cámaras Cinemachine durante los diálogos.
/// Simplemente cambia la prioridad de las cámaras virtuales.
/// </summary>
public class DialogueCameraManager : MonoBehaviour
{
    public static DialogueCameraManager Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private int dialogueCameraPriority = 20;
    [SerializeField] private int defaultCameraPriority = 10;

    private CinemachineCamera currentDialogueCamera;

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

    /// <summary>
    /// Activa una cámara virtual específica.
    /// </summary>
    public void ActivateCamera(CinemachineCamera camera)
    {
        if (camera == null) return;

        // Desactivar cámara anterior si existe
        if (currentDialogueCamera != null)
        {
            currentDialogueCamera.Priority = defaultCameraPriority;
        }

        // Activar nueva cámara
        camera.Priority = dialogueCameraPriority;
        currentDialogueCamera = camera;
    }

    /// <summary>
    /// Vuelve a la cámara por defecto (desactiva cámara de diálogo).
    /// </summary>
    public void DeactivateDialogueCamera()
    {
        if (currentDialogueCamera != null)
        {
            currentDialogueCamera.Priority = defaultCameraPriority;
            currentDialogueCamera = null;
        }
    }
}
