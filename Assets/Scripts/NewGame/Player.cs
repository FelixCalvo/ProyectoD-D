using Fusion;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador del personaje jugador en red usando Photon Fusion + NavMesh + Sistema de Combate RTS.
/// Combina la sincronización de red con el sistema de combate y movimiento RTS.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Player : NetworkBehaviour
{
  // ===== COMPONENTES =====
  private NavMeshAgent _agent;
  private Animator _animator;
  private Transform _visualModel;
  private CapsuleCollider _collider;
  private Vector3 _visualModelOriginalLocalPosition;
  
  // ===== VARIABLES DE RED (sincronizadas automáticamente) =====
  [Networked] private NetworkBool IsWalking { get; set; }
  [Networked] private Vector3 MoveDirection { get; set; }
  [Networked] private Vector3 TargetPosition { get; set; } // Destino de movimiento sincronizado
  [Networked] private NetworkBool IsAttacking { get; set; }
  [Networked] private NetworkString<_16> CurrentAttackTrigger { get; set; } // "Attack1" o "Attack2"
  [Networked] private int TargetNetworkId { get; set; } = -1; // ID del objetivo de ataque
  
  // ===== CONFIGURACIÓN =====
  [Header("Movement")]
  [SerializeField] private float moveSpeed = 5f;
  
  [Header("Combat")]
  [SerializeField] private float meleeAttackRange = 2.5f;
  [SerializeField] private float meleeStoppingDistance = 1.5f;
  [SerializeField] private float rangedAttackRange = 8f;
  [SerializeField] private float rangedStoppingDistance = 5f;
  [SerializeField] private float attackCooldown = 3.5f;
  [SerializeField] private LayerMask enemyLayer;
  
  [Header("Attack Type")]
  [SerializeField] private bool hasRangedAttack = false;
  
  // ===== ESTADO LOCAL =====
  private bool _lastWalkState = false;
  private bool _lastAttackState = false;
  private string _lastAttackTrigger = "";
  private float _lastAttackTime = -999f;
  private float _lastAnimationDebugTime = 0f;
  private Player _currentTarget = null;
  private float _meleeAnimationDuration = 1f;
  private float _rangedAnimationDuration = 1f;
  private bool _isUsingRangedAttack = false;
  private int _framesSinceDestinationSet = 999; // Contador para evitar limpieza prematura
  
  // Propiedades calculadas
  private float CurrentAttackRange => _isUsingRangedAttack ? rangedAttackRange : meleeAttackRange;
  private float CurrentStoppingDistance => _isUsingRangedAttack ? rangedStoppingDistance : meleeStoppingDistance;
  private string CurrentAttackTriggerLocal => _isUsingRangedAttack ? "Attack2" : "Attack1";
  private float CurrentAttackDuration => _isUsingRangedAttack ? _rangedAnimationDuration : _meleeAnimationDuration;

  /// <summary>
  /// Inicialización: busca componentes y configura NavMeshAgent
  /// </summary>
  private void Awake()
  {
    // Obtener componentes
    _agent = GetComponent<NavMeshAgent>();
    _collider = GetComponent<CapsuleCollider>();
    _animator = GetComponentInChildren<Animator>();
    
    if (_agent == null)
    {
      Debug.LogError($"[{gameObject.name}] ❌ No se encontró NavMeshAgent! Agrega el componente al prefab.");
    }
    else
    {
      // CRÍTICO: Deshabilitar temporalmente para evitar error "Failed to create agent"
      // Se habilitará en Spawned() después de corregir posición
      _agent.enabled = false;
    }
    
    if (_animator == null)
    {
      Debug.LogWarning($"[{gameObject.name}] No se encontró Animator en los hijos");
    }
    else
    {
      _visualModel = _animator.transform;
      _visualModelOriginalLocalPosition = _visualModel.localPosition;
      
      // Obtener duraciones de animaciones
      GetAttackAnimationDurations();
    }
    
    // Configurar NavMeshAgent
    if (_agent != null)
    {
      _agent.updateRotation = false; // Rotamos manualmente el modelo visual
      _agent.speed = 5f;
      _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
      _agent.radius = 0.3f;
      _agent.stoppingDistance = 0.1f; // Por defecto para movimiento preciso
    }
    
    // Configurar collider como trigger
    if (_collider != null)
    {
      _collider.isTrigger = true;
    }
  }

  /// <summary>
  /// Llamado cuando el objeto de red es creado (spawned) en cada cliente
  /// </summary>
  public override void Spawned()
  {
    base.Spawned();
    
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
        }
      }
      else
      {
        Debug.LogError($"[{gameObject.name}] ❌ NO se encontró NavMesh en radio de 50f desde {transform.position}! Bake NavMesh en Window → AI → Navigation");
      }
      
      // Habilitar NavMeshAgent ahora que la posición es correcta
      _agent.enabled = true;
    }
    
    // Log de configuración de combate
    Debug.Log($"[{gameObject.name}] ⚔️ Configuración: hasRangedAttack={hasRangedAttack}, meleeRange={meleeAttackRange:F1}, rangedRange={rangedAttackRange:F1}");
    
    // Asegurar que el Layer sea correcto
    int playerLayer = LayerMask.NameToLayer("Player");
    if (playerLayer == -1)
    {
      Debug.LogError($"[{gameObject.name}] ❌ Layer 'Player' no existe! Debes crearlo en Edit → Project Settings → Tags and Layers");
    }
    else
    {
      gameObject.layer = playerLayer;
    }
    
    // Forzar visibilidad de todos los renderers (fix para modelos que no aparecen)
    var renderers = GetComponentsInChildren<Renderer>(true);
    foreach (var renderer in renderers)
    {
      renderer.enabled = true;
      renderer.gameObject.SetActive(true);
    }
    
    // Desactivar LOD que puede causar invisibilidad
    var lodGroups = GetComponentsInChildren<LODGroup>(true);
    foreach (var lod in lodGroups)
    {
      lod.enabled = false;
    }
    
    // CRÍTICO: Desactivar NetworkTransform para evitar conflicto con NavMeshAgent
    // NavMeshAgent controla la posición localmente, NetworkTransform causaría "rubber-banding"
    var networkTransform = GetComponent<NetworkTransform>();
    if (networkTransform != null)
    {
      networkTransform.enabled = false;
    }
  }

  /// <summary>
  /// Actualización de física en red (se ejecuta en ticks fijos de Fusion).
  /// Ejecutado por el jugador que controla este personaje (InputAuthority o StateAuthority para el host).
  /// </summary>
  public override void FixedUpdateNetwork()
  {
    // El HOST controla sus personajes con StateAuthority
    // Los CLIENTES controlan sus personajes con InputAuthority
    // Procesar input SOLO si tenemos autoridad
    if (HasStateAuthority || HasInputAuthority)
    {
      // Procesar input de red
      if (GetInput(out NetworkInputData data))
      {
        // Comando de interacción (tecla E)
        if (data.interactCommand)
        {
          TryInteract();
        }
        
        // Comando de ataque tiene máxima prioridad
        if (data.attackCommand && data.targetPlayerId >= 0)
        {
          Player targetPlayer = FindPlayerByNetworkId(data.targetPlayerId);
          if (targetPlayer != null && targetPlayer != this)
          {
            AttackTarget(targetPlayer);
          }
        }
        // Comando de movimiento cancela ataque
        else if (data.moveCommand)
        {
          ClearTarget();
          MoveToPosition(data.targetPosition);
        }
      }

      // Sistema de combate tiene prioridad sobre movimiento manual
      if (_currentTarget != null)
      {
        UpdateCombat();
      }
    }
    
    // IMPORTANTE: Actualizar IsWalking en TODOS los clientes (incluso sin autoridad)
    // para sincronizar animaciones de personajes remotos
    if (_agent != null && _agent.isOnNavMesh)
    {
      // Incrementar contador de frames desde que se estableció destino
      if (TargetPosition != Vector3.zero)
      {
        _framesSinceDestinationSet++;
      }
      
      // CRÍTICO: Verificar que el agente no esté detenido incorrectamente
      // Esto puede pasar en Build con latencia
      if (TargetPosition != Vector3.zero && _agent.isStopped && _framesSinceDestinationSet < 60)
      {
        Debug.LogWarning($"[{name}] ⚠️ Agent detenido con TargetPosition activo, reactivando... (frames={_framesSinceDestinationSet})");
        _agent.isStopped = false;
        if (Vector3.Distance(_agent.destination, TargetPosition) > 0.5f)
        {
          _agent.SetDestination(TargetPosition);
        }
      }
      
      // Actualizar IsWalking basado SOLO en la velocidad del NavMeshAgent
      // El NavMeshAgent toma el control completo del movimiento
      
      float velocity = _agent.velocity.magnitude;
      
      // Si hay velocidad, activar Walk
      // SIMPLIFICADO: Solo usar velocity, sin verificar hasPath/pathPending
      // Threshold más bajo (0.05) para activar, muy bajo (0.01) para desactivar
      if (velocity > 0.05f)
      {
        if (!IsWalking)
        {
          IsWalking = true;
          Debug.Log($"[{name}] ✓ Caminando (velocity={velocity:F2}, frames={_framesSinceDestinationSet})");
        }
      }
      else if (velocity < 0.01f)
      {
        if (IsWalking)
        {
          IsWalking = false;
          Debug.Log($"[{name}] ⏹ Detenido (velocity={velocity:F2}, frames={_framesSinceDestinationSet})");
        }
      }
    }
  }
  
  /// <summary>
  /// Actualización visual (se ejecuta cada frame en TODOS los clientes).
  /// Sincroniza animaciones, rotación y NavMeshAgent basado en variables [Networked].
  /// </summary>
  public override void Render()
  {
    // Sincronizar _currentTarget desde TargetNetworkId en TODOS los clientes
    if (TargetNetworkId > 0 && _currentTarget == null)
    {
      _currentTarget = FindPlayerByNetworkId(TargetNetworkId);
    }
    else if (TargetNetworkId <= 0 && _currentTarget != null)
    {
      _currentTarget = null;
    }
    
    // Sincronizar NavMeshAgent con TargetPosition en TODOS los clientes
    // NO aplicar si estamos en combate (UpdateCombat maneja el NavMesh)
    if (_agent != null && _agent.isOnNavMesh && _currentTarget == null)
    {
      // Si hay un destino sincronizado y no estamos ya cerca de él
      if (TargetPosition != Vector3.zero)
      {
        float distanceToTarget = Vector3.Distance(transform.position, TargetPosition);
        
        // Solo aplicar destino si estamos lejos Y el destino cambió
        if (distanceToTarget > 1f && Vector3.Distance(_agent.destination, TargetPosition) > 0.5f)
        {
          _agent.isStopped = false;
          _agent.SetDestination(TargetPosition);
          Debug.Log($"[{name}] Render: Aplicando SetDestination, distancia={distanceToTarget:F2}, frames={_framesSinceDestinationSet}");
        }
        // Si llegamos al destino Y el NavMesh ya no tiene path, limpiar TargetPosition
        // CRÍTICO: Esperar al menos 30 frames (0.5 segundos a 60fps) antes de permitir limpieza
        // Esto evita race conditions en Build donde el path tarda en calcularse
        else if (distanceToTarget <= _agent.stoppingDistance + 1f && !_agent.hasPath && !_agent.pathPending && _framesSinceDestinationSet > 30)
        {
          if (HasStateAuthority || HasInputAuthority)
          {
            Debug.Log($"[{name}] Render: Limpiando TargetPosition (distancia={distanceToTarget:F2}, hasPath={_agent.hasPath}, pathPending={_agent.pathPending}, frames={_framesSinceDestinationSet})");
            TargetPosition = Vector3.zero; // Limpiar para evitar reaplicaciones
            _framesSinceDestinationSet = 999; // Reset contador
          }
        }
        else if (TargetPosition != Vector3.zero && Time.frameCount % 60 == 0) // Log cada 60 frames para no saturar
        {
          Debug.Log($"[{name}] Render: TargetPosition activo, distancia={distanceToTarget:F2}, hasPath={_agent.hasPath}, pathPending={_agent.pathPending}, isStopped={_agent.isStopped}, frames={_framesSinceDestinationSet}");
        }
      }
    }
    
    // Sincronizar Walk basado en IsWalking (sincronizado por red)
    if (_animator != null && _lastWalkState != IsWalking)
    {
      _lastWalkState = IsWalking;
      _animator.SetBool("Walk", IsWalking);
    }
    
    // Sincronizar animaciones de ataque
    if (_animator != null && !string.IsNullOrEmpty(CurrentAttackTrigger.Value))
    {
      string triggerName = CurrentAttackTrigger.Value.ToString();
      if (triggerName != _lastAttackTrigger)
      {
        _lastAttackTrigger = triggerName;
        _animator.SetTrigger(triggerName);
      }
    }
    
    // Actualizar rotación del modelo visual
    UpdateRotation();
  }

  // =================== MÉTODOS DE COMBATE ===================
  
  /// <summary>
  /// Lógica principal de combate: perseguir objetivo y atacar cuando está en rango.
  /// </summary>
  private void UpdateCombat()
  {
    if (_currentTarget == null)
      return;

    float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);
    
    // Decidir si usar ataque a distancia o cuerpo a cuerpo
    bool wasUsingRanged = _isUsingRangedAttack;
    if (hasRangedAttack && distanceToTarget > meleeAttackRange)
    {
      _isUsingRangedAttack = true;
    }
    else
    {
      _isUsingRangedAttack = false;
    }
    
    // Log cuando cambia el tipo de ataque
    if (wasUsingRanged != _isUsingRangedAttack)
    {
      Debug.Log($"[{name}] Cambio de ataque: hasRangedAttack={hasRangedAttack}, distancia={distanceToTarget:F2}, meleeRange={meleeAttackRange:F2} → {(_isUsingRangedAttack ? "RANGED (Attack2)" : "MELEE (Attack1)")}");
    }
    
    // Si está en rango de ataque
    if (distanceToTarget <= CurrentAttackRange)
    {
      // Detener movimiento y atacar
      if (_agent != null && _agent.isOnNavMesh)
      {
        _agent.ResetPath();
        _agent.velocity = Vector3.zero; // Evitar deslizamiento
      }
      
      // Atacar si ha pasado el cooldown
      if (Time.time >= _lastAttackTime + attackCooldown)
      {
        _lastAttackTime = Time.time;
        Attack();
      }
    }
    else
    {
      // Perseguir objetivo
      ChaseTarget(_currentTarget.transform.position);
    }
  }

  /// <summary>
  /// Persigue un objetivo usando NavMeshAgent.
  /// </summary>
  private void ChaseTarget(Vector3 targetPosition)
  {
    if (_agent == null || !_agent.isOnNavMesh)
      return;
    
    // Ajustar stoppingDistance según el tipo de ataque
    _agent.stoppingDistance = CurrentStoppingDistance;
    _agent.SetDestination(targetPosition);
  }

  /// <summary>
  /// Ejecuta un ataque activando el trigger correspondiente.
  /// </summary>
  private void Attack()
  {
    if (_animator == null)
      return;
    
    // Decidir qué ataque usar basado en si es ataque a distancia y la distancia actual
    string attackTrigger;
    if (_isUsingRangedAttack && hasRangedAttack)
    {
      attackTrigger = "Attack2"; // Ataque a distancia
    }
    else
    {
      attackTrigger = "Attack1"; // Ataque melee (cuerpo a cuerpo)
    }
    
    // Log para diagnóstico
    float distToTarget = _currentTarget != null ? Vector3.Distance(transform.position, _currentTarget.transform.position) : 0f;
    Debug.Log($"[{name}] 🎯 Atacando: {attackTrigger} | hasRangedAttack={hasRangedAttack}, _isUsingRangedAttack={_isUsingRangedAttack}, distancia={distToTarget:F2}");
    
    // Sincronizar trigger en la red
    CurrentAttackTrigger = attackTrigger;
    IsAttacking = true;
    
    // Activar trigger localmente
    _animator.SetTrigger(attackTrigger);
  }

  /// <summary>
  /// Ordena atacar a un objetivo específico.
  /// </summary>
  public void AttackTarget(Player target)
  {
    if (target == null || target == this)
      return;
    
    _currentTarget = target;
    
    // Sincronizar target ID en la red
    if (target.Object != null)
    {
      TargetNetworkId = (int)target.Object.Id.Raw;
    }
    
    // Resetear triggers anteriores
    if (_animator != null)
    {
      // Resetear ambos triggers (como en RTSUnit.cs)
      _animator.ResetTrigger("Attack1");
      _animator.ResetTrigger("Attack2");
      _animator.Play("Idle", 0, 0f);
    }
    
    // Resetear tiempo de ataque para permitir nuevo ataque inmediato
    _lastAttackTime = -999f;
    
    // UpdateCombat() decidirá qué ataque usar según distancia
  }

  /// <summary>
  /// Cancela el ataque actual y vuelve a modo movimiento.
  /// </summary>
  public void ClearTarget()
  {
    _currentTarget = null;
    TargetNetworkId = 0;
    IsAttacking = false;
    CurrentAttackTrigger = "";
    
    if (_animator != null)
    {
      _animator.ResetTrigger("Attack1");
      // Solo resetear Attack2 si el personaje tiene ataque a distancia
      if (hasRangedAttack)
      {
        _animator.ResetTrigger("Attack2");
      }
      _animator.Play("Idle", 0, 0f);
    }
    
    if (_agent != null && _agent.isOnNavMesh)
    {
      _agent.ResetPath();
      _agent.stoppingDistance = 0.1f;
    }
    
    _lastAttackTime = -999f;
  }

  /// <summary>
  /// Actualiza la rotación del modelo visual hacia el objetivo o dirección de movimiento.
  /// </summary>
  private void UpdateRotation()
  {
    if (_visualModel == null)
      return;
    
    Vector3 lookDirection = Vector3.zero;
    
    // Prioridad 1: Mirar al objetivo de combate
    if (_currentTarget != null)
    {
      lookDirection = (_currentTarget.transform.position - transform.position);
    }
    // Prioridad 2: Mirar hacia donde se mueve
    else if (_agent != null && _agent.isOnNavMesh && _agent.velocity.magnitude > 0.1f)
    {
      lookDirection = _agent.velocity;
    }
    // Prioridad 3: Mirar en dirección de input
    else if (MoveDirection.magnitude > 0.1f)
    {
      lookDirection = MoveDirection;
    }
    
    if (lookDirection.magnitude > 0.1f)
    {
      lookDirection.y = 0;
      lookDirection.Normalize();
      
      Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
      _visualModel.rotation = Quaternion.Slerp(_visualModel.rotation, targetRotation, Time.deltaTime * 10f);
    }
    
    // Resetear posición local del modelo (fix pivot)
    _visualModel.localPosition = _visualModelOriginalLocalPosition;
  }

  /// <summary>
  /// Detecta automáticamente las duraciones de las animaciones de ataque.
  /// </summary>
  private void GetAttackAnimationDurations()
  {
    if (_animator == null)
      return;

    RuntimeAnimatorController ac = _animator.runtimeAnimatorController;
    if (ac == null)
      return;

    foreach (AnimationClip clip in ac.animationClips)
    {
      if (clip.name.Contains("Attack1") || clip.name.Contains("attack1"))
      {
        _meleeAnimationDuration = clip.length;
      }
      else if (clip.name.Contains("Attack2") || clip.name.Contains("attack2"))
      {
        _rangedAnimationDuration = clip.length;
      }
    }
  }

  /// <summary>
  /// Dibuja gizmos en el editor para visualizar rangos de ataque.
  /// </summary>
  private void OnDrawGizmosSelected()
  {
    // Rango de ataque cuerpo a cuerpo
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
    
    // Stopping distance cuerpo a cuerpo
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, meleeStoppingDistance);
    
    if (hasRangedAttack)
    {
      // Rango de ataque a distancia
      Gizmos.color = Color.blue;
      Gizmos.DrawWireSphere(transform.position, rangedAttackRange);
      
      // Stopping distance a distancia
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, rangedStoppingDistance);
    }
  }

  // =================== MÉTODOS AUXILIARES ===================
  
  /// <summary>
  /// Busca un Player en la escena por su NetworkId.
  /// </summary>
  private Player FindPlayerByNetworkId(int networkId)
  {
    Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
    foreach (Player player in allPlayers)
    {
      if (player.Object != null && player.Object.Id.Raw == networkId)
      {
        return player;
      }
    }
    return null;
  }

  /// <summary>
  /// Mueve el jugador a una posición específica usando NavMesh.
  /// </summary>
  private void MoveToPosition(Vector3 position)
  {
    if (_agent == null)
    {
      Debug.LogError($"[{name}] NavMeshAgent es null en MoveToPosition");
      return;
    }
    
    if (!_agent.isOnNavMesh)
    {
      Debug.LogError($"[{name}] NavMeshAgent NO está en NavMesh. Posición: {transform.position}");
      return;
    }
    
    if (!_agent.enabled)
    {
      Debug.LogWarning($"[{name}] NavMeshAgent está deshabilitado, habilitándolo...");
      _agent.enabled = true;
    }
    
    _agent.stoppingDistance = 0.1f;
    _agent.isStopped = false; // CRÍTICO: asegurar que no está detenido
    bool success = _agent.SetDestination(position);
    
    // Sincronizar destino por red para que TODOS los clientes muevan el NavMeshAgent
    TargetPosition = position;
    _framesSinceDestinationSet = 0; // Reset contador para nueva orden de movimiento
    // NO forzar IsWalking aquí - dejar que FixedUpdateNetwork lo active cuando haya velocidad real
    
    Debug.Log($"[{name}] ✓ NavMesh SetDestination({position}) = {success}, hasPath: {_agent.hasPath}, pathPending: {_agent.pathPending}, velocity: {_agent.velocity.magnitude:F2}, TargetPosition={TargetPosition}, isStopped={_agent.isStopped}");
  }

  /// <summary>
  /// Intenta interactuar con objetos cercanos (puertas, NPCs, etc.)
  /// </summary>
  private void TryInteract()
  {
    // Buscar DoorTrigger en un radio cercano
    Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 3f);
    
    DoorTrigger closestDoor = null;
    float closestDistance = float.MaxValue;
    
    foreach (Collider col in nearbyColliders)
    {
      DoorTrigger door = col.GetComponent<DoorTrigger>();
      if (door != null)
      {
        float distance = Vector3.Distance(transform.position, col.transform.position);
        if (distance < closestDistance)
        {
          closestDistance = distance;
          closestDoor = door;
        }
      }
    }
    
    if (closestDoor != null)
    {
      // Llamar al método público de la puerta para abrirla
      closestDoor.TryOpen();
      Debug.Log($"[{name}] 🚪 Abriendo puerta: {closestDoor.name}");
    }
    else
    {
      Debug.Log($"[{name}] ❌ No hay puertas cercanas para abrir");
    }
  }
}