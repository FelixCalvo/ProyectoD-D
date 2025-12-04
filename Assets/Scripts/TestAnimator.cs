using UnityEngine;

public class TestAnimator : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private float moveSpeed = 5f;
    
    // Transform del modelo visual (hijo) que rotaremos
    private Transform _visualModel;
    
    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        
        if (_animator == null)
        {
            Debug.LogError("No se encontró Animator");
        }
        else
        {
            Debug.Log($"✅ Animator encontrado: {_animator.name}");
            // El modelo visual es el transform del Animator (el hijo)
            _visualModel = _animator.transform;
            Debug.Log($"📦 Modelo visual: {_visualModel.name}");
        }
    }
    
    void Update()
    {
        // Recoger input WASD
        Vector3 direction = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) direction += Vector3.back;
        if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
        if (Input.GetKey(KeyCode.D)) direction += Vector3.right;
        
        // Verificar si hay movimiento
        bool isWalking = direction.magnitude > 0.1f;
        
        // Actualizar animación
        if (_animator != null)
        {
            _animator.SetBool("Walk", isWalking);
        }
        
        // Mover el personaje
        if (isWalking)
        {
            direction = direction.normalized;
            
            // MOVER el transform RAÍZ (sin rotar)
            Vector3 movement = direction * moveSpeed * Time.deltaTime;
            transform.position += movement;
            
            // ROTAR solo el modelo VISUAL (el hijo)
            if (_visualModel != null)
            {
                _visualModel.rotation = Quaternion.LookRotation(direction);
            }
            
            Debug.Log($"🎯 Dirección: {direction} | Posición raíz: {transform.position} | Rotación visual: {_visualModel.eulerAngles.y:F1}°");
        }
    }
}
