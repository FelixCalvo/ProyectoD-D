using UnityEngine;
using Fusion;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private bool onlyOnce = false;
    [SerializeField] private bool requireKeyPress = true;
    [SerializeField] private KeyCode openKey = KeyCode.E;
    [SerializeField] private float disableBlockerDelay = 0.5f;
    
    // Variable sincronizada de red
    [Networked] private NetworkBool IsOpen { get; set; }
    
    private Animator doorAnimator;
    private Collider doorBlocker;
    private BoxCollider triggerCollider;
    private UnityEngine.AI.NavMeshObstacle navMeshObstacle;
    private bool hasBeenOpened = false;
    private bool playerInRange = false;

    void Start()
    {
        // Buscar Animator en este GameObject
        doorAnimator = GetComponent<Animator>();
        if (doorAnimator == null)
        {
            Debug.LogError($"[{gameObject.name}] No se encontró Animator. Añade uno al mismo GameObject.");
        }
        
        // Buscar colliders: uno para trigger, otro para bloquear
        Collider[] colliders = GetComponents<Collider>();
        
        foreach (Collider col in colliders)
        {
            if (col.isTrigger && col is BoxCollider)
            {
                triggerCollider = col as BoxCollider;
            }
            else if (!col.isTrigger)
            {
                doorBlocker = col;
            }
        }
        
        if (triggerCollider == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No hay BoxCollider marcado como Trigger. Añade uno para detectar al jugador.");
        }
        
        if (doorBlocker == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No hay Collider sólido. Añade uno para bloquear el paso.");
        }
        
        // Buscar o añadir NavMeshObstacle para bloquear el NavMeshAgent
        navMeshObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (navMeshObstacle == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Añadiendo NavMeshObstacle para bloquear el paso del NavMeshAgent.");
            navMeshObstacle = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            navMeshObstacle.carving = true;
            navMeshObstacle.carveOnlyStationary = false;
        }
    }

    void Update()
    {
        if (requireKeyPress && playerInRange && Input.GetKeyDown(openKey))
        {
            OpenDoor();
        }
        
        // Sincronizar el estado de la puerta en todos los clientes (solo en multiplayer)
        if (Object != null && Object.IsValid && IsOpen && !hasBeenOpened)
        {
            ApplyDoorOpenState();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        RTSUnit unit = other.GetComponent<RTSUnit>();
        if (unit != null)
        {
            playerInRange = true;
            
            if (!requireKeyPress)
            {
                OpenDoor();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        RTSUnit unit = other.GetComponent<RTSUnit>();
        if (unit != null)
        {
            playerInRange = false;
        }
    }

    private void OpenDoor()
    {
        if (onlyOnce && hasBeenOpened)
        {
            return;
        }

        // Marcar como abierta en la red (se sincroniza automáticamente)
        if (Object != null && Object.IsValid && Object.HasStateAuthority)
        {
            IsOpen = true;
            Debug.Log($"[{gameObject.name}] 🚪 Puerta abierta por jugador con autoridad");
        }
        else
        {
            // En singleplayer o si no hay red
            ApplyDoorOpenState();
        }
    }
    
    /// <summary>
    /// Aplica el estado de puerta abierta (animación y colliders)
    /// </summary>
    private void ApplyDoorOpenState()
    {
        if (doorAnimator != null && !hasBeenOpened)
        {
            doorAnimator.SetTrigger(openTriggerName);
            hasBeenOpened = true;
            
            // Desactivar el collider bloqueante después de un delay
            if (doorBlocker != null)
            {
                Invoke(nameof(DisableBlocker), disableBlockerDelay);
            }
            
            Debug.Log($"[{gameObject.name}] ✅ Estado de puerta abierta aplicado");
        }
    }

    /// <summary>
    /// Método público para abrir la puerta desde scripts externos (como Player en multiplayer)
    /// </summary>
    public void TryOpen()
    {
        OpenDoor();
    }
    
    /// <summary>
    /// Verifica si la puerta está abierta (para TransparentarParedes)
    /// </summary>
    public bool IsDoorOpen()
    {
        // Verificar estado local primero
        if (hasBeenOpened) return true;
        
        // En multiplayer, verificar estado sincronizado
        if (Object != null && Object.IsValid)
        {
            return IsOpen;
        }
        
        return false;
    }
    
    private void DisableBlocker()
    {
        if (doorBlocker != null)
        {
            doorBlocker.enabled = false;
        }
        
        // Desactivar NavMeshObstacle para permitir paso
        if (navMeshObstacle != null)
        {
            navMeshObstacle.enabled = false;
        }
    }
}
