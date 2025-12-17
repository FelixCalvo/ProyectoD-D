using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sistema de evitación inteligente para unidades RTS.
/// Las unidades con menor priority esperan cuando detectan congestión.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RTSUnitAvoidance : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Velocidad mínima para considerar que está bloqueada")]
    [SerializeField] private float blockedThreshold = 0.1f;
    
    [Tooltip("Tiempo para considerar que está atascada")]
    [SerializeField] private float blockedTimeThreshold = 1f;
    
    [Tooltip("Tiempo de espera cuando se detecta bloqueo")]
    [SerializeField] private float waitTime = 2f;
    
    private NavMeshAgent agent;
    private float blockedTimer = 0f;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private Vector3 lastDestination;
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    
    void Update()
    {
        // Si está esperando, contar tiempo
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            
            if (waitTimer <= 0f)
            {
                // Reiniciar movimiento
                ResumeMovement();
            }
            return;
        }
        
        // Si no tiene destino o está parado intencionalmente, resetear
        if (!agent.hasPath || agent.isStopped)
        {
            blockedTimer = 0f;
            return;
        }
        
        // Detectar si está bloqueada (velocidad muy baja pero debería moverse)
        if (agent.velocity.magnitude < blockedThreshold && agent.remainingDistance > agent.stoppingDistance)
        {
            blockedTimer += Time.deltaTime;
            
            // Si lleva bloqueada suficiente tiempo
            if (blockedTimer >= blockedTimeThreshold)
            {
                // Las unidades con priority más alta (valor más bajo) esperan menos
                // Priority 0 = no espera, Priority 50 = espera completo
                float priorityFactor = agent.avoidancePriority / 100f;
                
                // Solo esperar si no es la prioridad más alta
                if (agent.avoidancePriority > 0)
                {
                    StartWaiting(waitTime * priorityFactor);
                }
                
                blockedTimer = 0f;
            }
        }
        else
        {
            // Se está moviendo bien, resetear timer
            blockedTimer = 0f;
        }
    }
    
    /// <summary>
    /// Inicia período de espera
    /// </summary>
    private void StartWaiting(float time)
    {
        isWaiting = true;
        waitTimer = time;
        
        // Guardar destino actual
        if (agent.hasPath)
        {
            lastDestination = agent.destination;
        }
        
        // Detener agente temporalmente
        agent.isStopped = true;
        
        Debug.Log($"{gameObject.name} esperando {time:F1}s (Priority: {agent.avoidancePriority})");
    }
    
    /// <summary>
    /// Reanuda el movimiento después de esperar
    /// </summary>
    private void ResumeMovement()
    {
        isWaiting = false;
        agent.isStopped = false;
        
        // Reintentar ir al último destino
        if (lastDestination != Vector3.zero)
        {
            agent.SetDestination(lastDestination);
        }
        
        Debug.Log($"{gameObject.name} reanudando movimiento");
    }
}
