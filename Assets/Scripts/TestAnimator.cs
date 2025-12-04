using UnityEngine;

/// <summary>
/// Script de testing para probar movimiento y animaciones SIN red.
/// Útil para debuggear problemas de movimiento/rotación antes de probar en multiplayer.
/// 
/// USO:
/// 1. Asignar a un prefab de personaje en escena de test
/// 2. Ejecutar escena
/// 3. Usar WASD para mover
/// 
/// NOTA: Implementa la misma lógica que Player.cs pero sin Fusion.
/// </summary>
public class TestAnimator : MonoBehaviour
{
    // ===== COMPONENTES =====
    private Animator _animator;
    private Transform _visualModel;
    
    // ===== CONFIGURACIÓN =====
    [SerializeField] private float moveSpeed = 5f;
    
    /// <summary>
    /// Inicialización: busca Animator y guarda referencia al modelo visual
    /// </summary>
    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        
        if (_animator == null)
        {
            Debug.LogError($"[{name}] No se encontró Animator en los hijos");
        }
        else
        {
            // El modelo visual es el transform del Animator (hijo del prefab)
            _visualModel = _animator.transform;
        }
    }
    
    /// <summary>
    /// Update de movimiento: Lee input WASD y mueve el personaje
    /// </summary>
    void Update()
    {
        // Recoger input WASD en espacio mundo
        Vector3 direction = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) direction += Vector3.back;
        if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
        if (Input.GetKey(KeyCode.D)) direction += Vector3.right;
        
        // Verificar si hay movimiento
        bool isWalking = direction.magnitude > 0.1f;
        
        // Actualizar animación Walk
        if (_animator != null)
        {
            _animator.SetBool("Walk", isWalking);
        }
        
        // Mover y rotar personaje
        if (isWalking)
        {
            direction = direction.normalized;
            
            // MOVER transform raíz (sin rotar)
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // ROTAR solo el modelo visual (hijo)
            if (_visualModel != null)
            {
                _visualModel.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}
