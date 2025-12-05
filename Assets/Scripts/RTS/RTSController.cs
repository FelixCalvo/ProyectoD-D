using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controlador principal del sistema RTS.
/// Gestiona selección de unidades y órdenes de movimiento mediante clicks.
/// </summary>
public class RTSController : MonoBehaviour
{
    // ===== CONFIGURACIÓN =====
    [Header("Units")]
    [SerializeField] private List<RTSUnit> controllableUnits = new List<RTSUnit>();
    
    [Header("Input Layers")]
    [SerializeField] private LayerMask groundLayer = ~0; // Todo por defecto
    [SerializeField] private LayerMask unitLayer = ~0;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject destinationMarkerPrefab;
    
    // ===== ESTADO =====
    private RTSUnit _selectedUnit;
    private Camera _mainCamera;
    private GameObject _currentDestinationMarker;
    
    /// <summary>
    /// Unidad actualmente seleccionada
    /// </summary>
    public RTSUnit SelectedUnit => _selectedUnit;
    
    void Start()
    {
        _mainCamera = Camera.main;
        
        // Auto-detectar unidades si la lista está vacía
        if (controllableUnits.Count == 0)
        {
            controllableUnits.AddRange(FindObjectsOfType<RTSUnit>());
            Debug.Log($"Auto-detectadas {controllableUnits.Count} unidades");
        }
        
        // Seleccionar primera unidad por defecto
        if (controllableUnits.Count > 0)
        {
            SelectUnit(controllableUnits[0]);
        }
    }
    
    void Update()
    {
        HandleMouseInput();
        HandleKeyboardShortcuts();
    }
    
    /// <summary>
    /// Maneja clicks del ratón para selección y movimiento
    /// </summary>
    private void HandleMouseInput()
    {
        // Click izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            // Primero verificar si clickeamos una unidad
            if (Physics.Raycast(ray, out hit, 1000f, unitLayer))
            {
                RTSUnit unit = hit.collider.GetComponent<RTSUnit>();
                if (unit != null && controllableUnits.Contains(unit))
                {
                    SelectUnit(unit);
                    return;
                }
            }
        }
        
        // Click derecho: mover o atacar
        if (Input.GetMouseButtonDown(1))
        {
            if (_selectedUnit != null)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                
                // Hacer ambos raycast para decidir cuál usar
                RaycastHit unitHit;
                RaycastHit groundHit;
                bool hitUnit = Physics.Raycast(ray, out unitHit, 1000f, unitLayer);
                bool hitGround = Physics.Raycast(ray, out groundHit, 1000f, groundLayer);
                
                // Prioridad: Si clickeamos una unidad diferente, atacar
                if (hitUnit)
                {
                    RTSUnit targetUnit = unitHit.collider.GetComponent<RTSUnit>();
                    
                    if (targetUnit != null && targetUnit != _selectedUnit)
                    {
                        // Atacar la unidad clickeada
                        _selectedUnit.AttackTarget(targetUnit);
                        Debug.Log($"[ATAQUE] {_selectedUnit.UnitName} ordenado atacar a {targetUnit.UnitName}");
                        return; // Salir para no procesar el movimiento
                    }
                }
                
                // Si no clickeamos una unidad enemiga, mover al suelo
                if (hitGround)
                {
                    Debug.Log($"[MOVIMIENTO] Clic en suelo detectado en {groundHit.point}");
                    _selectedUnit.ClearTarget(); // Cancelar cualquier ataque en curso
                    MoveSelectedUnitTo(groundHit.point);
                }
                else
                {
                    Debug.LogWarning($"[ERROR] Clic derecho no detectó ni unidad ni suelo. UnitLayer: {unitLayer.value}, GroundLayer: {groundLayer.value}");
                }
            }
        }
    }
    
    /// <summary>
    /// Maneja teclas 1-4 para selección rápida de unidades
    /// </summary>
    private void HandleKeyboardShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SelectUnitByIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SelectUnitByIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SelectUnitByIndex(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            SelectUnitByIndex(3);
        }
        
        // Tab para ciclar entre unidades
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SelectNextUnit();
        }
    }
    
    /// <summary>
    /// Selecciona una unidad específica
    /// </summary>
    private void SelectUnit(RTSUnit unit)
    {
        // Deseleccionar unidad anterior
        if (_selectedUnit != null)
        {
            _selectedUnit.SetSelected(false);
        }
        
        // Seleccionar nueva unidad
        _selectedUnit = unit;
        _selectedUnit.SetSelected(true);
        
        Debug.Log($"Unidad seleccionada: {unit.UnitName}");
        
        // Notificar a la UI (si existe)
        RTSUI ui = FindObjectOfType<RTSUI>();
        if (ui != null)
        {
            ui.UpdateSelectedUnit(_selectedUnit);
        }
    }
    
    /// <summary>
    /// Selecciona unidad por índice (0-3)
    /// </summary>
    private void SelectUnitByIndex(int index)
    {
        if (index >= 0 && index < controllableUnits.Count)
        {
            SelectUnit(controllableUnits[index]);
        }
    }
    
    /// <summary>
    /// Selecciona la siguiente unidad (rotación)
    /// </summary>
    private void SelectNextUnit()
    {
        if (controllableUnits.Count == 0) return;
        
        int currentIndex = controllableUnits.IndexOf(_selectedUnit);
        int nextIndex = (currentIndex + 1) % controllableUnits.Count;
        
        SelectUnit(controllableUnits[nextIndex]);
    }
    
    /// <summary>
    /// Mueve la unidad seleccionada a una posición
    /// </summary>
    private void MoveSelectedUnitTo(Vector3 destination)
    {
        if (_selectedUnit == null) return;
        
        _selectedUnit.MoveTo(destination);
        
        // Mostrar marcador visual de destino (opcional)
        ShowDestinationMarker(destination);
        
        Debug.Log($"{_selectedUnit.UnitName} moviéndose a {destination}");
    }
    
    /// <summary>
    /// Muestra un marcador temporal en el destino clickeado
    /// </summary>
    private void ShowDestinationMarker(Vector3 position)
    {
        if (destinationMarkerPrefab == null) return;
        
        // Destruir marcador anterior
        if (_currentDestinationMarker != null)
        {
            Destroy(_currentDestinationMarker);
        }
        
        // Crear nuevo marcador
        _currentDestinationMarker = Instantiate(destinationMarkerPrefab, position, Quaternion.identity);
        
        // Destruir después de 2 segundos
        Destroy(_currentDestinationMarker, 2f);
    }
    
    /// <summary>
    /// Añade una unidad a la lista de controlables
    /// </summary>
    public void AddUnit(RTSUnit unit)
    {
        if (!controllableUnits.Contains(unit))
        {
            controllableUnits.Add(unit);
        }
    }
    
    /// <summary>
    /// Remueve una unidad de la lista de controlables
    /// </summary>
    public void RemoveUnit(RTSUnit unit)
    {
        if (controllableUnits.Contains(unit))
        {
            controllableUnits.Remove(unit);
            
            // Si era la unidad seleccionada, seleccionar otra
            if (_selectedUnit == unit && controllableUnits.Count > 0)
            {
                SelectUnit(controllableUnits[0]);
            }
        }
    }
}
