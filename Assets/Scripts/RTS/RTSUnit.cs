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
    [SerializeField] private float attackRange = 2.5f; // Debe ser > stoppingDistance (2.0f)
    [SerializeField] private float attackCooldown = 3.5f; // Mayor que la animación más larga (2.117s) para evitar loops
    [SerializeField] private LayerMask enemyLayer;
    
    // ===== ESTADO =====
    private bool _isSelected = false;
    private Vector3 _lastPosition;
    private float _lastAttackTime = -999f; // Inicializar en negativo para permitir primer ataque
    private RTSUnit _currentTarget = null;
    private float _attackAnimationDuration = 1f; // Duración de la animación Attack1
    
    /// <summary>
    /// Nombre de la unidad (para UI)
    /// </summary>
    public string UnitName => unitName;
    
    /// <summary>
    /// Si la unidad está seleccionada
    /// </summary>
    public bool IsSelected => _isSelected;
    
    void Awake()
    {
        // Obtener componentes
        _agent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<CapsuleCollider>();
        _animator = GetComponentInChildren<Animator>();
        
        if (_animator != null)
        {
            _visualModel = _animator.transform;
            // Guardar posición local original del modelo visual
            _visualModelOriginalLocalPosition = _visualModel.localPosition;
            
            // Obtener duración de la animación Attack1
            GetAttackAnimationDuration();
        }
        
        // Configurar NavMeshAgent
        _agent.updateRotation = false; // Rotamos manualmente el modelo visual
        _agent.speed = 5f;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance; // Desactivar evasión dinámica
        _agent.radius = 0.3f; // Radio para pathfinding estático
        _agent.stoppingDistance = 0.1f; // Distancia mínima por defecto para movimientos precisos
        
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
        if (_agent != null && _agent.isOnNavMesh)
        {
            // Verificar si el destino está ocupado por otra unidad
            if (!IsPositionOccupied(destination))
            {
                _agent.SetDestination(destination);
            }
            else
            {
                // Buscar posición libre cercana
                Vector3 freePosition = FindNearbyFreePosition(destination);
                _agent.SetDestination(freePosition);
            }
        }
    }
    
    /// <summary>
    /// Persigue un objetivo directamente sin verificar ocupación (para combate)
    /// </summary>
    private void ChaseTarget(Vector3 targetPosition)
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            // Ajustar stoppingDistance para mantener distancia de ataque
            _agent.stoppingDistance = 1.5f;
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
        
        // Comprobar si estamos en tiempo de ataque
        bool isPlayingAttack = (Time.time < _lastAttackTime + _attackAnimationDuration);
        
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
            
            // Si el objetivo está en rango de ataque
            if (distanceToTarget <= attackRange)
            {
                // Detener movimiento (NavMeshAgent debería haberlo hecho con stoppingDistance)
                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }
                
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
    /// Ejecuta un ataque (trigger de animación)
    /// </summary>
    public void Attack()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("Attack1");
        }
    }
    
    /// <summary>
    /// Obtiene la duración de la animación Attack1 del Animator
    /// </summary>
    private void GetAttackAnimationDuration()
    {
        if (_animator == null) return;
        
        RuntimeAnimatorController ac = _animator.runtimeAnimatorController;
        if (ac == null) return;
        
        // Buscar el clip de animación llamado "Attack1"
        foreach (AnimationClip clip in ac.animationClips)
        {
            if (clip.name.Contains("Attack1") || clip.name.Contains("attack1"))
            {
                _attackAnimationDuration = clip.length;
                return;
            }
        }
        
        // Si no se encuentra, usar valor por defecto
        _attackAnimationDuration = 1f;
    }
    
    /// <summary>
    /// Ordena atacar a un objetivo específico
    /// </summary>
    public void AttackTarget(RTSUnit target)
    {
        if (target == null || target == this) return;
        
        _currentTarget = target;
        // UpdateCombat() se encargará de perseguir y atacar automáticamente
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
            _animator.ResetTrigger("Attack1");
            // Forzar transición inmediata a Idle, interrumpiendo Attack1
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
        
        // Dibujar rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Dibujar línea hacia el objetivo
        if (_currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
        }
    }
}
