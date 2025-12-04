using UnityEngine;

/// <summary>
/// Indicador visual de selección para unidades RTS.
/// Puede ser un círculo, anillo o cualquier efecto visual bajo el personaje.
/// </summary>
public class SelectionIndicator : MonoBehaviour
{
    // ===== CONFIGURACIÓN =====
    [Header("Visual")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    // ===== COMPONENTES =====
    private Renderer _renderer;
    private Vector3 _originalScale;
    private Vector3 _originalLocalPosition;
    private Transform _parent;
    private float _pulseTimer = 0f;
    
    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _originalScale = transform.localScale;
        _originalLocalPosition = transform.localPosition;
        _parent = transform.parent;
        
        // Aplicar color
        if (_renderer != null)
        {
            _renderer.material.color = selectedColor;
        }
    }
    
    void LateUpdate()
    {
        // IMPORTANTE: Mantener posición local fija
        transform.localPosition = _originalLocalPosition;
        
        // CRÍTICO: Cancelar la rotación heredada del padre
        // El padre (personaje) rota, pero el indicator debe mantener rotación world estable
        if (_parent != null)
        {
            // Rotación propia (solo Y) independiente del padre
            float currentYRotation = transform.eulerAngles.y + (rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
        }
        
        // Efecto de pulsación (escala)
        _pulseTimer += Time.deltaTime * pulseSpeed;
        float scale = 1f + Mathf.Sin(_pulseTimer) * pulseAmount;
        transform.localScale = _originalScale * scale;
    }
}
