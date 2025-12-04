using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
  private Animator _animator;
  private Transform _visualModel; // Transform del modelo visual (hijo) que rotaremos
  
  // Variable de red para sincronizar el estado de caminar
  [Networked] private NetworkBool IsWalking { get; set; }
  
  // Variable de red para sincronizar la dirección de movimiento
  [Networked] private Vector3 MoveDirection { get; set; }
  
  // Variable local para evitar setear el Animator innecesariamente
  private bool _lastWalkState = false;
  
  [SerializeField] private float moveSpeed = 5f;

  private void Awake()
  {
    _animator = GetComponentInChildren<Animator>();
    
    if (_animator == null)
    {
      Debug.LogWarning($"⚠️ [{gameObject.name}] No se encontró Animator en los hijos");
    }
    else
    {
      Debug.Log($"✅ [{gameObject.name}] Animator encontrado: {_animator.name}");
      // El modelo visual es el transform del Animator (el hijo)
      _visualModel = _animator.transform;
    }
  }

  public override void Spawned()
  {
    base.Spawned();
    
    Debug.Log($"🎮 [{gameObject.name}] Spawned en cliente. IsLocal: {Object.HasInputAuthority} | StateAuthority: {Object.HasStateAuthority}");
    
    // Forzar visibilidad de todos los renderers
    var renderers = GetComponentsInChildren<Renderer>(true);
    foreach (var renderer in renderers)
    {
      renderer.enabled = true;
      renderer.gameObject.SetActive(true);
    }
    
    // Desactivar LOD si existe (puede causar invisibilidad en clientes)
    var lodGroups = GetComponentsInChildren<LODGroup>(true);
    foreach (var lod in lodGroups)
    {
      lod.enabled = false;
    }
  }

  public override void FixedUpdateNetwork()
  {
    if (GetInput(out NetworkInputData data))
    {
      // Verificar si hay movimiento
      bool shouldWalk = data.direction.magnitude > 0.1f;
      
      // Solo el jugador con StateAuthority mueve el personaje
      if (HasStateAuthority && shouldWalk)
      {
        // Normalizar la dirección del input
        Vector3 direction = data.direction.normalized;
        
        // Guardar la dirección en variable de red para sincronizar
        MoveDirection = direction;
        
        // MOVER el transform RAÍZ (sin rotar) - NetworkTransform sincronizará esto
        Vector3 movement = direction * moveSpeed * Runner.DeltaTime;
        transform.position += movement;
      }
      
      // Actualizar el estado de caminar
      if (HasStateAuthority && IsWalking != shouldWalk)
      {
        IsWalking = shouldWalk;
        
        // Si dejamos de caminar, resetear dirección
        if (!shouldWalk)
        {
          MoveDirection = Vector3.zero;
        }
      }
    }
    else if (HasStateAuthority && IsWalking)
    {
      // Si no hay input, detener la animación
      IsWalking = false;
      MoveDirection = Vector3.zero;
    }
  }
  
  public override void Render()
  {
    // Sincronizar el Animator con el estado de red en TODOS los clientes
    if (_animator != null && _lastWalkState != IsWalking)
    {
      _lastWalkState = IsWalking;
      _animator.SetBool("Walk", IsWalking);
    }
    
    // ROTAR el modelo visual según la dirección sincronizada (en TODOS los clientes)
    if (_visualModel != null && MoveDirection.magnitude > 0.1f)
    {
      _visualModel.rotation = Quaternion.LookRotation(MoveDirection);
    }
  }
}