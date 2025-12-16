using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private bool onlyOnce = false;
    [SerializeField] private bool requireKeyPress = true;
    [SerializeField] private KeyCode openKey = KeyCode.E;
    [SerializeField] private float disableBlockerDelay = 0.5f;
    
    private Animator doorAnimator;
    private Collider doorBlocker;
    private BoxCollider triggerCollider;
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
    }

    void Update()
    {
        if (requireKeyPress && playerInRange && Input.GetKeyDown(openKey))
        {
            OpenDoor();
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

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(openTriggerName);
            hasBeenOpened = true;
            
            // Desactivar el collider bloqueante después de un delay
            if (doorBlocker != null)
            {
                Invoke(nameof(DisableBlocker), disableBlockerDelay);
            }
        }
    }
    
    private void DisableBlocker()
    {
        if (doorBlocker != null)
        {
            doorBlocker.enabled = false;
        }
    }
}
