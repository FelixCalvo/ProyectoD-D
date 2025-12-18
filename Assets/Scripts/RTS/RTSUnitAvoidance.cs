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
    [SerializeField] private float waitTime = 1f;
    
    [Tooltip("Tiempo mínimo de espera incluso para priority 0")]
    [SerializeField] private float minWaitTime = 0.3f;
    
    [Tooltip("Distancia extra para considerar que llegó al destino")]
    [SerializeField] private float arrivalTolerance = 0.2f;
    
    private NavMeshAgent agent;
    private float blockedTimer = 0f;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private Vector3 lastDestination;
    private Vector3 previousDestination;
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    
    void Update()
    {
        // DETECTAR NUEVO DESTINO INMEDIATAMENTE (incluso si está parado o esperando)
        if (agent.hasPath)
        {
            Vector3 currentDestination = agent.destination;
            
            // Si el destino cambió significativamente
            if (Vector3.Distance(currentDestination, previousDestination) > 0.5f)
            {
                // Reactivar agente inmediatamente
                if (agent.isStopped)
                {
                    agent.isStopped = false;
                    Debug.Log($"{gameObject.name} nuevo destino, reactivando agente");
                }
                
                // Cancelar espera si estaba esperando
                if (isWaiting)
                {
                    isWaiting = false;
                    Debug.Log($"{gameObject.name} nuevo destino, cancelando espera");
                }
                
                previousDestination = currentDestination;
                lastDestination = currentDestination;
                blockedTimer = 0f;
            }
        }
        
        // Si está esperando, solo contar tiempo
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            
            if (waitTimer <= 0f)
            {
                ResumeMovement();
            }
            return;
        }
        
        // Si no tiene destino, resetear
        if (!agent.hasPath)
        {
            blockedTimer = 0f;
            previousDestination = Vector3.zero;
            return;
        }
        
        // Verificar si ya llegó al destino
        float distanceToDestination = agent.remainingDistance;
        bool hasArrived = distanceToDestination <= (agent.stoppingDistance + arrivalTolerance);
        
        // Si llegó al destino, detener completamente
        if (hasArrived && !agent.isStopped)
        {
            agent.isStopped = true;
            blockedTimer = 0f;
            return;
        }
        
        // Si está parado intencionalmente (ya llegó), no hacer más
        if (agent.isStopped && hasArrived)
        {
            blockedTimer = 0f;
            return;
        }
        
        // Detectar si está bloqueada (velocidad muy baja pero debería moverse)
        if (agent.velocity.magnitude < blockedThreshold && !hasArrived)
        {
            blockedTimer += Time.deltaTime;
            
            // Si lleva bloqueada suficiente tiempo
            if (blockedTimer >= blockedTimeThreshold)
            {
                // Calcular tiempo de espera basado en prioridad
                // Priority 0 = minWaitTime, Priority 50+ = waitTime completo
                float priorityFactor = Mathf.Clamp01(agent.avoidancePriority / 100f);
                float calculatedWait = Mathf.Lerp(minWaitTime, waitTime, priorityFactor);
                
                StartWaiting(calculatedWait);
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
