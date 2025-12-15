using UnityEngine;

/// <summary>
/// Bloquea el movimiento de unidades RTS cuando hay un diálogo activo.
/// Usa el DialogueCameraManager para detectar si hay una cámara de diálogo activa.
/// </summary>
public class DialogueBlocker : MonoBehaviour
{
    public static DialogueBlocker Instance { get; private set; }

    /// <summary>
    /// True si hay un diálogo activo (hay una cámara de NPC activa)
    /// </summary>
    public bool IsDialogueActive
    {
        get
        {
            if (DialogueCameraManager.Instance == null)
                return false;
            
            return DialogueCameraManager.Instance.IsDialogueActive;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}
