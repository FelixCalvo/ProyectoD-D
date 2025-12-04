using Fusion;
using UnityEngine;

/// <summary>
/// Controlador del personaje jugador en red usando Photon Fusion.
/// Maneja movimiento, rotación y sincronización de animaciones en multiplayer.
/// 
/// SOLUCIÓN AL PROBLEMA DEL PIVOT:
/// - El transform raíz se mueve en línea recta SIN rotar
/// - El modelo visual (hijo) rota hacia la dirección de movimiento
/// - Esto evita que un pivot adelantado cause movimiento en semicírculo
/// </summary>
public class Player : NetworkBehaviour
{
  // ===== COMPONENTES =====
  private Animator _animator;
  private Transform _visualModel; // Transform del hijo que contiene el modelo 3D
  
  // ===== VARIABLES DE RED (sincronizadas automáticamente) =====
  [Networked] private NetworkBool IsWalking { get; set; }
  [Networked] private Vector3 MoveDirection { get; set; }
  
  // ===== VARIABLES LOCALES =====
  private bool _lastWalkState = false;
  
  // ===== CONFIGURACIÓN =====
  [SerializeField] private float moveSpeed = 5f;

  /// <summary>
  /// Inicialización: busca el Animator y guarda referencia al modelo visual
  /// </summary>
  private void Awake()
  {
    _animator = GetComponentInChildren<Animator>();
    
    if (_animator == null)
    {
      Debug.LogWarning($"[{gameObject.name}] No se encontró Animator en los hijos");
    }
    else
    {
      // El modelo visual es el transform que contiene el Animator (hijo del prefab)
      _visualModel = _animator.transform;
    }
  }

  /// <summary>
  /// Llamado cuando el objeto de red es creado (spawned) en cada cliente
  /// </summary>
  public override void Spawned()
  {
    base.Spawned();
    
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
  }

  /// <summary>
  /// Actualización de física en red (se ejecuta en ticks fijos de Fusion).
  /// Solo el cliente con StateAuthority modifica posición y dirección.
  /// </summary>
  public override void FixedUpdateNetwork()
  {
    if (GetInput(out NetworkInputData data))
    {
      bool shouldWalk = data.direction.magnitude > 0.1f;
      
      // Solo el cliente con autoridad mueve el personaje
      if (HasStateAuthority && shouldWalk)
      {
        Vector3 direction = data.direction.normalized;
        
        // Sincronizar dirección para que todos los clientes sepan hacia dónde rota
        MoveDirection = direction;
        
        // Mover SOLO el transform raíz (sin rotar)
        // NetworkTransform sincronizará automáticamente la posición
        Vector3 movement = direction * moveSpeed * Runner.DeltaTime;
        transform.position += movement;
      }
      
      // Actualizar estado de animación
      if (HasStateAuthority && IsWalking != shouldWalk)
      {
        IsWalking = shouldWalk;
        
        if (!shouldWalk)
        {
          MoveDirection = Vector3.zero;
        }
      }
    }
    else if (HasStateAuthority && IsWalking)
    {
      // Sin input, detener animación
      IsWalking = false;
      MoveDirection = Vector3.zero;
    }
  }
  
  /// <summary>
  /// Actualización visual (se ejecuta cada frame en TODOS los clientes).
  /// Sincroniza animaciones y rotación del modelo visual.
  /// </summary>
  public override void Render()
  {
    // Actualizar parámetro Walk del Animator
    if (_animator != null && _lastWalkState != IsWalking)
    {
      _lastWalkState = IsWalking;
      _animator.SetBool("Walk", IsWalking);
    }
    
    // Rotar el modelo visual (hijo) hacia la dirección de movimiento
    // IMPORTANTE: Solo rota el hijo, NO la raíz
    if (_visualModel != null && MoveDirection.magnitude > 0.1f)
    {
      _visualModel.rotation = Quaternion.LookRotation(MoveDirection);
    }
  }
}