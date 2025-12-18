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
    private List<RTSUnit> _selectedUnits = new List<RTSUnit>();
    private Camera _mainCamera;
    private GameObject _currentDestinationMarker;
    
    // Selección por rectángulo
    private bool _isDragging = false;
    private Vector3 _dragStartPos;
    
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
            controllableUnits.AddRange(FindObjectsByType<RTSUnit>(FindObjectsSortMode.None));
            Debug.Log($"Auto-detectadas {controllableUnits.Count} unidades");
        }
        
        // Seleccionar primera unidad por defecto
        if (controllableUnits.Count > 0)
        {
            SelectSingleUnit(controllableUnits[0]);
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
        // Iniciar arrastre para selección múltiple
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _dragStartPos = Input.mousePosition;
        }
        
        // Soltar botón: finalizar selección
        if (Input.GetMouseButtonUp(0))
        {
            if (_isDragging)
            {
                _isDragging = false;
                
                // Si fue un click simple (sin arrastre), selección individual
                if (Vector3.Distance(_dragStartPos, Input.mousePosition) < 5f)
                {
                    HandleSingleSelection();
                }
                else
                {
                    // Selección múltiple por rectángulo
                    HandleBoxSelection();
                }
            }
        }
        
        // Click derecho: mover o atacar
        if (Input.GetMouseButtonDown(1))
        {
            // Bloquear si hay diálogo activo
            if (DialogueBlocker.Instance != null && DialogueBlocker.Instance.IsDialogueActive)
            {
                return;
            }
            
            // Si hay unidades seleccionadas (una o múltiples)
            if (_selectedUnits.Count > 0)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                
                // Usar RaycastAll para atravesar paredes transparentes
                RaycastHit[] allHits = Physics.RaycastAll(ray, 1000f);
                
                RaycastHit unitHit = default;
                RaycastHit groundHit = default;
                bool hitUnit = false;
                bool hitGround = false;
                
                // Buscar el primer hit de unidad y de suelo, ignorando objetos transparentes
                foreach (RaycastHit hit in allHits)
                {
                    // Ignorar objetos en capa "ObjetosTransparentes"
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ObjetosTransparentes"))
                        continue;
                    
                    // Detectar unidad
                    if (((1 << hit.collider.gameObject.layer) & unitLayer) != 0 && !hitUnit)
                    {
                        unitHit = hit;
                        hitUnit = true;
                    }
                    
                    // Detectar suelo
                    if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0 && !hitGround)
                    {
                        groundHit = hit;
                        hitGround = true;
                    }
                    
                    // Si ya encontramos ambos, salir
                    if (hitUnit && hitGround)
                        break;
                }
                
                // Prioridad: Si clickeamos una unidad diferente, atacar
                if (hitUnit)
                {
                    RTSUnit targetUnit = unitHit.collider.GetComponent<RTSUnit>();
                    
                    if (targetUnit != null && !_selectedUnits.Contains(targetUnit))
                    {
                        // Todas las unidades seleccionadas atacan el objetivo
                        foreach (RTSUnit unit in _selectedUnits)
                        {
                            unit.AttackTarget(targetUnit);
                        }
                        Debug.Log($"[ATAQUE] {_selectedUnits.Count} unidades ordenadas atacar a {targetUnit.UnitName}");
                        return;
                    }
                }
                
                // Si no clickeamos una unidad enemiga, mover al suelo
                if (hitGround)
                {
                    MoveSelectedUnitsTo(groundHit.point);
                }
                else
                {
                    Debug.LogWarning($"[ERROR] Clic derecho no detectó ni unidad ni suelo.");
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
    /// Maneja selección de una sola unidad con click
    /// </summary>
    private void HandleSingleSelection()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] allHits = Physics.RaycastAll(ray, 1000f);
        System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));
        
        foreach (RaycastHit hit in allHits)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ObjetosTransparentes"))
                continue;
            
            if (((1 << hit.collider.gameObject.layer) & unitLayer) != 0)
            {
                RTSUnit unit = hit.collider.GetComponent<RTSUnit>();
                if (unit != null && controllableUnits.Contains(unit))
                {
                    SelectSingleUnit(unit);
                    return;
                }
            }
        }
        
        // Si no se seleccionó nada, deseleccionar todo
        DeselectAll();
    }
    
    /// <summary>
    /// Maneja selección múltiple por rectángulo
    /// </summary>
    private void HandleBoxSelection()
    {
        DeselectAll();
        
        // Crear rectángulo sin invertir coordenadas
        Vector3 start = _dragStartPos;
        Vector3 end = Input.mousePosition;
        
        float minX = Mathf.Min(start.x, end.x);
        float maxX = Mathf.Max(start.x, end.x);
        float minY = Mathf.Min(start.y, end.y);
        float maxY = Mathf.Max(start.y, end.y);
        
        // Añadir margen de tolerancia para capturar mejor unidades parcialmente visibles
        float margin = 20f;
        Rect selectionRect = Rect.MinMaxRect(minX - margin, minY - margin, maxX + margin, maxY + margin);
        
        foreach (RTSUnit unit in controllableUnits)
        {
            // Usar punto un poco más arriba (cabeza aproximada) en lugar de los pies
            Vector3 worldPos = unit.transform.position + Vector3.up * 1f;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
            
            // Solo verificar que esté dentro del rectángulo (más tolerante)
            if (selectionRect.Contains(screenPos))
            {
                _selectedUnits.Add(unit);
                unit.SetSelected(true);
            }
        }
        
        // Actualizar player activo y lista global
        if (_selectedUnits.Count > 0)
        {
            _selectedUnit = _selectedUnits[0];
            HelperClass.ActivePlayer = _selectedUnit.gameObject;
            
            // Actualizar lista global de seleccionados
            List<GameObject> selectedObjects = new List<GameObject>();
            foreach (RTSUnit unit in _selectedUnits)
            {
                selectedObjects.Add(unit.gameObject);
            }
            HelperClass.SetSelectedPlayers(selectedObjects);
            
            Debug.Log($"{_selectedUnits.Count} unidades seleccionadas");
        }
    }
    
    /// <summary>
    /// Selecciona una sola unidad
    /// </summary>
    private void SelectSingleUnit(RTSUnit unit)
    {
        DeselectAll();
        
        _selectedUnit = unit;
        _selectedUnits.Add(unit);
        unit.SetSelected(true);
        
        HelperClass.ActivePlayer = unit.gameObject;
        HelperClass.SetSelectedPlayers(new List<GameObject> { unit.gameObject });
        
        Debug.Log($"Unidad seleccionada: {unit.UnitName}");
        
        // Notificar a la UI (si existe)
        RTSUI ui = FindFirstObjectByType<RTSUI>();
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
            SelectSingleUnit(controllableUnits[index]);
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
        
        SelectSingleUnit(controllableUnits[nextIndex]);
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
        
        //Debug.Log($"{_selectedUnit.UnitName} moviéndose a {destination}");
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
            _selectedUnits.Remove(unit);
            
            // Si era la unidad seleccionada, seleccionar otra
            if (_selectedUnit == unit && controllableUnits.Count > 0)
            {
                SelectSingleUnit(controllableUnits[0]);
            }
        }
    }
    
    /// <summary>
    /// Deselecciona todas las unidades
    /// </summary>
    private void DeselectAll()
    {
        foreach (RTSUnit unit in _selectedUnits)
        {
            unit.SetSelected(false);
        }
        _selectedUnits.Clear();
        HelperClass.SetSelectedPlayers(new List<GameObject>());
        _selectedUnit = null;
    }
    
    /// <summary>
    /// Mueve todas las unidades seleccionadas a una posición
    /// </summary>
    private void MoveSelectedUnitsTo(Vector3 destination)
    {
        if (_selectedUnits.Count == 0) return;
        
        // Si solo hay una unidad, moverla directamente
        if (_selectedUnits.Count == 1)
        {
            _selectedUnits[0].ClearTarget();
            _selectedUnits[0].MoveTo(destination);
        }
        else
        {
            // Para múltiples unidades, usar formación loose
            Vector3[] positions = CalculateFormationPositions(destination, _selectedUnits.Count);
            
            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                _selectedUnits[i].ClearTarget();
                _selectedUnits[i].MoveTo(positions[i]);
            }
        }
        
        ShowDestinationMarker(destination);
        //Debug.Log($"{_selectedUnits.Count} unidades moviéndose a {destination}");
    }
    
    /// <summary>
    /// Calcula posiciones en formación circular alrededor de un punto
    /// </summary>
    private Vector3[] CalculateFormationPositions(Vector3 center, int unitCount)
    {
        Vector3[] positions = new Vector3[unitCount];
        
        // Radio suficiente para evitar que se empujen (stopping distance + margen)
        float baseRadius = 1.5f;
        
        // Si son 2 unidades, ponerlas una al lado de la otra
        if (unitCount == 2)
        {
            positions[0] = center + new Vector3(-baseRadius * 0.7f, 0, 0);
            positions[1] = center + new Vector3(baseRadius * 0.7f, 0, 0);
        }
        // Si son 3 unidades, triángulo
        else if (unitCount == 3)
        {
            positions[0] = center + new Vector3(0, 0, baseRadius);
            positions[1] = center + new Vector3(-baseRadius * 0.866f, 0, -baseRadius * 0.5f);
            positions[2] = center + new Vector3(baseRadius * 0.866f, 0, -baseRadius * 0.5f);
        }
        // Si son 4 unidades, usar formación en cuadrado
        else if (unitCount == 4)
        {
            float offset = baseRadius * 0.7f;
            positions[0] = center + new Vector3(-offset, 0, -offset);
            positions[1] = center + new Vector3(offset, 0, -offset);
            positions[2] = center + new Vector3(-offset, 0, offset);
            positions[3] = center + new Vector3(offset, 0, offset);
        }
        // Formación circular para otras cantidades
        else
        {
            float angleStep = 360f / unitCount;
            
            for (int i = 0; i < unitCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = center.x + baseRadius * Mathf.Cos(angle);
                float z = center.z + baseRadius * Mathf.Sin(angle);
                positions[i] = new Vector3(x, center.y, z);
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// Crea un rectángulo en coordenadas de pantalla
    /// </summary>
    private Rect GetScreenRect(Vector3 screenPos1, Vector3 screenPos2)
    {
        screenPos1.y = Screen.height - screenPos1.y;
        screenPos2.y = Screen.height - screenPos2.y;
        
        Vector3 bottomLeft = Vector3.Min(screenPos1, screenPos2);
        Vector3 topRight = Vector3.Max(screenPos1, screenPos2);
        
        return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
    }
    
    /// <summary>
    /// Dibuja el rectángulo de selección en pantalla
    /// </summary>
    void OnGUI()
    {
        if (_isDragging)
        {
            Rect rect = GetScreenRect(_dragStartPos, Input.mousePosition);
            DrawScreenRect(rect, new Color(1f, 0f, 0f, 0.25f)); // Rojo semi-transparente
            DrawScreenRectBorder(rect, 2, new Color(1f, 0f, 0f)); // Borde rojo
        }
    }
    
    /// <summary>
    /// Dibuja un rectángulo lleno en pantalla
    /// </summary>
    private void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
    
    /// <summary>
    /// Dibuja el borde de un rectángulo en pantalla
    /// </summary>
    private void DrawScreenRectBorder(Rect rect, float thickness, Color color)
    {
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color); // Arriba
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color); // Izquierda
        DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color); // Derecha
        DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color); // Abajo
    }
}
