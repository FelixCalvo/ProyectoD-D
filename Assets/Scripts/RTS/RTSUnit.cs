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
    
    // ===== ESTADO =====
    private bool _isSelected = false;
    private Vector3 _lastPosition;
    
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
        }
        
        // Configurar NavMeshAgent
        _agent.updateRotation = false; // Rotamos manualmente el modelo visual
        _agent.speed = 5f;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance; // Desactivar evasión dinámica
        _agent.radius = 0.3f; // Radio para pathfinding estático
        
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
        
        // Verificar si se está moviendo
        bool isMoving = _agent.velocity.magnitude > 0.1f;
        
        _animator.SetBool("Walk", isMoving);
    }
    
    /// <summary>
    /// Rota el modelo visual hacia la dirección de movimiento
    /// </summary>
    private void UpdateRotation()
    {
        if (_visualModel == null) return;
        
        // Solo rotar si hay velocidad significativa
        if (_agent.velocity.magnitude > 0.1f)
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
    }
}
