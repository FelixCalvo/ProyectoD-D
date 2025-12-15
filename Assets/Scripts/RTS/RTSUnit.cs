using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Componente de cada unidad RTS (personaje controlable).
/// Maneja movimiento con NavMesh, animaciones y estado de selección.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RTSUnit : MonoBehaviour
{
    // ===== COMPONENTES =====
    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _visualModel;
    private CapsuleCollider _collider;
    private Vector3 _visualModelOriginalLocalPosition;

    // ===== CONFIGURACIÓN =====
    [Header("Unit Info")]
    [SerializeField] private string unitName = "Unit";

    [Header("Selection")]
    [SerializeField] private GameObject selectionIndicator;

    [Header("Combat")]
    [SerializeField] private float meleeAttackRange = 2.5f; // Rango de ataque cuerpo a cuerpo (Attack1)
    [SerializeField] private float meleeStoppingDistance = 1.5f; // Distancia de parada para melee
    [SerializeField] private float rangedAttackRange = 8f; // Rango de ataque a distancia (Attack2)
    [SerializeField] private float rangedStoppingDistance = 5f; // Distancia de parada para ranged
    [SerializeField] private float attackCooldown = 3.5f; // Mayor que la animación más larga (2.117s) para evitar loops
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Attack Type")]
    [SerializeField] private bool hasRangedAttack = false; // Si tiene Attack2 (arco, hechizos). Todos tienen Attack1 (melee)

    // ===== ESTADO =====
    private bool _isSelected = false;
    private Vector3 _lastPosition;
    private float _lastAttackTime = -999f; // Inicializar en negativo para permitir primer ataque
    private RTSUnit _currentTarget = null;
    private float _meleeAnimationDuration = 1f; // Duración de Attack1
    private float _rangedAnimationDuration = 1f; // Duración de Attack2
    private bool _isUsingRangedAttack = false; // Qué ataque está usando actualmente
    
    // Propiedades calculadas según el ataque que se va a usar
    private float CurrentAttackRange => _isUsingRangedAttack ? rangedAttackRange : meleeAttackRange;
    private float CurrentStoppingDistance => _isUsingRangedAttack ? rangedStoppingDistance : meleeStoppingDistance;
    private string CurrentAttackTrigger => _isUsingRangedAttack ? "Attack2" : "Attack1";
    private float CurrentAttackDuration => _isUsingRangedAttack ? _rangedAnimationDuration : _meleeAnimationDuration;

    /// <summary>
    /// Nombre de la unidad (para UI)
    /// </summary>
    public string UnitName => unitName;

    /// <summary>
    /// Si la unidad está seleccionada
    /// </summary>
    public bool IsSelected => _isSelected;

    // Componente opcional de obstáculo
    private NavMeshObstacle _obstacle;

    void Awake()
    {
        // Obtener componentes
        _agent = GetComponent<NavMeshAgent>();
        _obstacle = GetComponent<NavMeshObstacle>();
        _collider = GetComponent<CapsuleCollider>();
        _animator = GetComponentInChildren<Animator>();

        if (_agent == null)
        {
            Debug.LogError($"[{gameObject.name}] ❌ No se encontró NavMeshAgent! Agrega el componente al prefab.");
            return;
        }
        
        // Desactivar NavMeshObstacle si existe (no pueden estar activos simultáneamente)
        if (_obstacle != null)
        {
            _obstacle.enabled = false;
        }
        
        // CRÍTICO: Deshabilitar temporalmente para evitar error "Failed to create agent"
        // Se habilitará en Start() después de corregir posición
        _agent.enabled = false;

        if (_animator != null)
        {
            _visualModel = _animator.transform;
            // Guardar posición local original del modelo visual
            _visualModelOriginalLocalPosition = _visualModel.localPosition;
            
            // Obtener duración de ambas animaciones de ataque
            GetAttackAnimationDurations();
        }
        
        // Configurar NavMeshAgent (mientras está deshabilitado)
        _agent.updateRotation = false; // Rotamos manualmente el modelo visual
        _agent.speed = 5f;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; // Evasión activa de otras unidades
        _agent.radius = 0.5f; // Radio de colisión con otras unidades
        _agent.stoppingDistance = 0.1f; // Distancia mínima por defecto para movimientos precisos
        _agent.avoidancePriority = 50; // Prioridad media (0-99, menor = mayor prioridad)

        // Configurar collider como trigger (no bloquea físicamente)
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }

        // Desactivar indicador por defecto
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }

        _lastPosition = transform.position;
    }
    
    void Start()
    {
        // CRÍTICO: Asegurar que el NavMeshAgent esté sobre el NavMesh
        if (_agent != null)
        {
            // Buscar el punto más cercano del NavMesh (radio ampliado a 50f para mayor tolerancia)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 50f, NavMesh.AllAreas))
            {
                // Mover al punto más cercano del NavMesh si es necesario
                if (Vector3.Distance(transform.position, hit.position) > 0.1f)
                {
                    transform.position = hit.position;
                    Debug.Log($"[{gameObject.name}] ✅ Reposicionado sobre NavMesh: {hit.position}");
                }
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] ❌ NO se encontró NavMesh en radio de 50f desde {transform.position}! Bake NavMesh en Window → AI → Navigation");
            }
            
            // Habilitar NavMeshAgent ahora que la posición es correcta
            _agent.enabled = true;
            //Debug.Log($"[{gameObject.name}] ✅ NavMeshAgent habilitado. isOnNavMesh={_agent.isOnNavMesh}");
        }
    }

    void Update()
    {
        UpdateCombat(); // Primero combate (puede modificar movimiento)
        UpdateAnimation();
        UpdateRotation();
    }

    /// <summary>
    /// Ordena a la unidad moverse a una posición
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (_agent == null)
        {
            Debug.LogError($"[{gameObject.name}] ❌ NavMeshAgent es null en MoveTo");
            return;
        }
        
        if (!_agent.isOnNavMesh)
        {
            Debug.LogError($"[{gameObject.name}] ❌ NavMeshAgent NO está en NavMesh. Posición: {transform.position}");
            return;
        }
        
        if (!_agent.enabled)
        {
            Debug.LogWarning($"[{gameObject.name}] NavMeshAgent está deshabilitado, habilitándolo...");
            _agent.enabled = true;
        }
        
        // Verificar si el destino está ocupado por otra unidad
        if (!IsPositionOccupied(destination))
        {
            _agent.SetDestination(destination);
            //Debug.Log($"[{gameObject.name}] 🎯 Moviendo a {destination}");
        }
        else
        {
            // Buscar posición libre cercana
            Vector3 freePosition = FindNearbyFreePosition(destination);
            _agent.SetDestination(freePosition);
            Debug.Log($"[{gameObject.name}] 🎯 Posición ocupada, moviendo a posición alternativa {freePosition}");
        }
    }

    /// <summary>
    /// Persigue un objetivo directamente sin verificar ocupación (para combate)
    /// </summary>
    private void ChaseTarget(Vector3 targetPosition)
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            // Ajustar stoppingDistance según tipo de ataque
            _agent.stoppingDistance = CurrentStoppingDistance;
            _agent.SetDestination(targetPosition);
        }
    }

    /// <summary>
    /// Verifica si una posición está ocupada por otra unidad
    /// </summary>
    private bool IsPositionOccupied(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f);
        foreach (Collider col in colliders)
        {
            RTSUnit otherUnit = col.GetComponent<RTSUnit>();
            if (otherUnit != null && otherUnit != this)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Encuentra una posición libre cerca del destino original
    /// </summary>
    private Vector3 FindNearbyFreePosition(Vector3 targetPosition)
    {
        // Intentar posiciones en círculo alrededor del objetivo
        for (int angle = 0; angle < 360; angle += 45)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * 1.5f;
            Vector3 testPosition = targetPosition + offset;

            if (!IsPositionOccupied(testPosition))
            {
                return testPosition;
            }
        }

        // Si no encuentra libre, devolver el original
        return targetPosition;
    }

    /// <summary>
    /// Selecciona o deselecciona la unidad
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;

        // Mostrar/ocultar indicador visual
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }
    }

    /// <summary>
    /// Actualiza la animación Walk según si se está moviendo
    /// </summary>
    private void UpdateAnimation()
    {
        if (_animator == null) return;

        // Comprobar si estamos en tiempo de ataque (usar duración del ataque actual)
        bool isPlayingAttack = (Time.time < _lastAttackTime + CurrentAttackDuration);

        // Solo actualizar Walk si NO estamos en animación de ataque
        if (!isPlayingAttack)
        {
            // Verificar si se está moviendo
            bool isMoving = _agent.velocity.magnitude > 0.1f;
            _animator.SetBool("Walk", isMoving);
        }
        else
        {
            // Si estamos atacando, asegurar que Walk está desactivado
            _animator.SetBool("Walk", false);
        }
    }

    /// <summary>
    /// Rota el modelo visual hacia la dirección de movimiento
    /// </summary>
    private void UpdateRotation()
    {
        if (_visualModel == null) return;

        // Si estamos atacando, rotar hacia el objetivo
        if (_currentTarget != null)
        {
            Vector3 directionToTarget = (_currentTarget.transform.position - transform.position).normalized;
            directionToTarget.y = 0;
            if (directionToTarget != Vector3.zero)
            {
                _visualModel.rotation = Quaternion.LookRotation(directionToTarget);
            }
        }
        // Si estamos moviéndonos, rotar hacia la dirección de movimiento
        else if (_agent.velocity.magnitude > 0.1f)
        {
            Vector3 direction = _agent.velocity.normalized;
            _visualModel.rotation = Quaternion.LookRotation(direction);
        }

        // CRÍTICO: Resetear posición local para evitar deriva por pivot descentrado
        _visualModel.localPosition = _visualModelOriginalLocalPosition;
    }

    /// <summary>
    /// Detiene el movimiento de la unidad
    /// </summary>
    public void Stop()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }
    }

    /// <summary>
    /// Verifica si la unidad está actualmente en movimiento
    /// </summary>
    public bool IsMoving()
    {
        return _agent != null && _agent.velocity.magnitude > 0.1f;
    }

    /// <summary>
    /// Sistema de combate - busca y ataca enemigos cercanos
    /// </summary>
    private void UpdateCombat()
    {
        // Si tenemos objetivo, verificar si sigue válido
        if (_currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

            // DECISIÓN: ¿Qué ataque usar según la distancia?
            if (hasRangedAttack && distanceToTarget > meleeAttackRange)
            {
                // Está lejos y tiene Attack2 → Usar ataque a distancia
                _isUsingRangedAttack = true;
            }
            else
            {
                // Está cerca O no tiene Attack2 → Usar ataque melee (Attack1)
                _isUsingRangedAttack = false;
            }

            // Si el objetivo está en rango de ataque (del tipo de ataque elegido)
            if (distanceToTarget <= CurrentAttackRange)
            {
                // Detener movimiento completamente para evitar patineo
                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }
                
                // CRÍTICO: Forzar velocidad a cero para evitar inercia/patineo
                _agent.velocity = Vector3.zero;

                // Atacar solo si ha pasado el cooldown
                if (Time.time >= _lastAttackTime + attackCooldown)
                {
                    _lastAttackTime = Time.time; // Actualizar ANTES de Attack() para evitar múltiples llamadas
                    Attack();
                }
            }
            else
            {
                // Perseguir al objetivo continuamente (actualizar destino cada frame)
                ChaseTarget(_currentTarget.transform.position);
            }
        }
    }

    /// <summary>
    /// Ejecuta un ataque (trigger de animación según tipo de personaje)
    /// </summary>
    public void Attack()
    {
        if (_animator != null)
        {
            // Usar Attack1 (melee) o Attack2 (ranged) según configuración
            _animator.SetTrigger(CurrentAttackTrigger);
        }
    }

    /// <summary>
    /// Obtiene la duración de ambas animaciones de ataque (Attack1 y Attack2 si existe)
    /// </summary>
    private void GetAttackAnimationDurations()
    {
        if (_animator == null) return;

        RuntimeAnimatorController ac = _animator.runtimeAnimatorController;
        if (ac == null) return;

        // Buscar Attack1 (melee - todos lo tienen)
        foreach (AnimationClip clip in ac.animationClips)
        {
            if (clip.name.Contains("Attack1") || clip.name.Contains("attack1"))
            {
                _meleeAnimationDuration = clip.length;
                break;
            }
        }

        // Buscar Attack2 (ranged - solo algunos lo tienen)
        if (hasRangedAttack)
        {
            foreach (AnimationClip clip in ac.animationClips)
            {
                if (clip.name.Contains("Attack2") || clip.name.Contains("attack2"))
                {
                    _rangedAnimationDuration = clip.length;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Ordena atacar a un objetivo específico
    /// </summary>
    public void AttackTarget(RTSUnit target)
    {
        if (target == null || target == this) return;

        _currentTarget = target;

        // Interrumpir cualquier animación de ataque anterior
        if (_animator != null)
        {
            // Resetear ambos triggers por si acaso
            _animator.ResetTrigger("Attack1");
            _animator.ResetTrigger("Attack2");
            _animator.Play("Idle", 0, 0f);
        }

        // Resetear tiempo de ataque para permitir nuevo ataque inmediato
        _lastAttackTime = -999f;

        // UpdateCombat() decidirá qué ataque usar según distancia
    }

    /// <summary>
    /// Cancela el objetivo actual y detiene cualquier persecución
    /// </summary>
    public void ClearTarget()
    {
        _currentTarget = null;

        // Resetear tiempo de ataque para permitir animación Walk inmediatamente
        _lastAttackTime = -999f;

        // Interrumpir animación de ataque forzando estado Idle
        if (_animator != null)
        {
            // Resetear ambos triggers por si acaso
            _animator.ResetTrigger("Attack1");
            _animator.ResetTrigger("Attack2");
            // Forzar transición inmediata a Idle, interrumpiendo ataque
            _animator.Play("Idle", 0, 0f);
        }

        // Restaurar stoppingDistance normal para movimientos precisos
        if (_agent != null)
        {
            _agent.stoppingDistance = 0.1f;

            // Detener cualquier movimiento de persecución activo
            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar el camino del NavMeshAgent en el editor
        if (_agent != null && _agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = _agent.path.corners;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }

        // Dibujar rango de ataque (rojo para el rango activo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, CurrentAttackRange);
        
        // Dibujar círculo de stopping distance (amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, CurrentStoppingDistance);

        // Dibujar línea hacia el objetivo
        if (_currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
        }
    }
}
