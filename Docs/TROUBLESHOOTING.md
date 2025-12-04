# Guía de Troubleshooting

## 🔍 Problemas Comunes y Soluciones

### 1. El personaje se mueve en semicírculo al cambiar dirección

#### Síntomas
- Al presionar A y cambiar rápidamente a D, el personaje hace un arco
- La distancia recorrida varía entre frames
- El movimiento no es una línea recta

#### Causa
Pivot del modelo adelantado + rotación del transform raíz

#### Solución
✅ **Verificar jerarquía del prefab**:
```
Root (NetworkObject, Player.cs)
└── Visual Model (Animator, SkinnedMeshRenderer)
```

✅ **Verificar código en Player.cs**:
```csharp
// FixedUpdateNetwork
transform.position += movement;  // Solo mover raíz
MoveDirection = direction;

// Render
_visualModel.rotation = Quaternion.LookRotation(MoveDirection);  // Solo rotar hijo
```

✅ **Verificar que _visualModel se inicializa**:
```csharp
private void Awake()
{
    _animator = GetComponentInChildren<Animator>();
    _visualModel = _animator.transform;  // CRÍTICO
}
```

#### Testing
1. Probar en escena de test sin red (TestAnimator.cs)
2. Si funciona ahí pero no en multiplayer → problema de sincronización
3. Si falla también en test → problema de jerarquía/código

---

### 2. La animación Walk no se sincroniza en red

#### Síntomas
- El personaje camina en el servidor pero está quieto en el cliente
- O viceversa
- La animación Walk no se activa en otros clientes

#### Causa
- NetworkMecanimAnimator interfiriendo
- Variable IsWalking no sincronizada
- Animador no se actualiza en Render()

#### Solución
✅ **NO usar NetworkMecanimAnimator**:
```
Prefab Root
├── NetworkObject
├── NetworkTransform
├── Player.cs
└── NO NetworkMecanimAnimator ❌
```

✅ **Usar variable [Networked]**:
```csharp
[Networked] private NetworkBool IsWalking { get; set; }
```

✅ **Actualizar en Render (todos los clientes)**:
```csharp
public override void Render()
{
    if (_animator != null && _lastWalkState != IsWalking)
    {
        _lastWalkState = IsWalking;
        _animator.SetBool("Walk", IsWalking);
    }
}
```

✅ **Verificar Animator Controller**:
- Parámetro "Walk" existe (tipo Bool)
- Transiciones Idle ↔ Walk configuradas
- HasExitTime: false (para respuesta inmediata)

---

### 3. El personaje mira siempre en una dirección (no rota)

#### Síntomas
- En multiplayer, un cliente ve a otro siempre mirando adelante
- El modelo no rota hacia donde camina
- Funciona para el jugador local pero no para otros

#### Causa
- Rotación no sincronizada
- MoveDirection no se está usando en Render()
- _visualModel es null

#### Solución
✅ **Variable de red para dirección**:
```csharp
[Networked] private Vector3 MoveDirection { get; set; }
```

✅ **Actualizar en FixedUpdateNetwork**:
```csharp
if (HasStateAuthority && shouldWalk)
{
    MoveDirection = direction;  // Sincronizar dirección
}
```

✅ **Rotar en Render**:
```csharp
public override void Render()
{
    if (_visualModel != null && MoveDirection.magnitude > 0.1f)
    {
        _visualModel.rotation = Quaternion.LookRotation(MoveDirection);
    }
}
```

✅ **Verificar que _visualModel no es null**:
```csharp
private void Awake()
{
    _animator = GetComponentInChildren<Animator>();
    if (_animator != null)
    {
        _visualModel = _animator.transform;
        Debug.Log($"Visual Model: {_visualModel.name}");  // Debe aparecer en logs
    }
}
```

---

### 4. El personaje no aparece en el cliente

#### Síntomas
- Solo se ve un eje de coordenadas o nada
- El personaje aparece en el servidor/host pero no en clientes
- En logs: "Spawned" pero visualmente invisible

#### Causa
- Renderers desactivados
- LOD Group escondiendo el modelo
- Layer incorrecto
- Prefab mal configurado

#### Solución
✅ **Forzar visibilidad en Spawned()**:
```csharp
public override void Spawned()
{
    base.Spawned();
    
    // Activar todos los renderers
    var renderers = GetComponentsInChildren<Renderer>(true);
    foreach (var renderer in renderers)
    {
        renderer.enabled = true;
        renderer.gameObject.SetActive(true);
    }
    
    // Desactivar LOD
    var lodGroups = GetComponentsInChildren<LODGroup>(true);
    foreach (var lod in lodGroups)
    {
        lod.enabled = false;
    }
}
```

✅ **Verificar Layer del prefab**:
- Debe ser "Default" o un layer visible por la cámara

✅ **Verificar prefab en Assets**:
- SkinnedMeshRenderer activo
- Material asignado
- Mesh asignado

---

### 5. El personaje se teleporta o tiene lag

#### Síntomas
- Movimiento errático
- Saltos de posición
- Retraso visible entre input y movimiento

#### Causa
- NetworkTransform mal configurado
- Red lenta/inestable
- Interpolación desactivada
- Tick rate muy bajo

#### Solución
✅ **Verificar NetworkTransform**:
```
NetworkTransform
├── Sync Position: Yes
├── Interpolation: Default ✅
└── Auto AOI Override: Yes
```

✅ **Verificar conexión**:
- Presionar F2 en runtime → Ver RTT (debe ser < 100ms)
- Verificar Bandwidth
- Probar en red local primero

✅ **Ajustar Tick Rate** (en NetworkRunner):
```
Simulation Config
└── Tick Rate: 60 Hz (recomendado)
```

✅ **No modificar transform en Update()**:
```csharp
// ❌ INCORRECTO
void Update()
{
    transform.position += movement;
}

// ✅ CORRECTO
public override void FixedUpdateNetwork()
{
    if (HasStateAuthority)
    {
        transform.position += movement;
    }
}
```

---

### 6. Input no responde o retraso en controles

#### Síntomas
- Presionar WASD no mueve el personaje
- Retraso de ~1 segundo entre presionar tecla y movimiento
- Funciona en host pero no en cliente

#### Causa
- GetInput() devolviendo false
- InputAuthority no asignada correctamente
- OnInput() no implementado

#### Solución
✅ **Verificar OnInput en BasicSpawner**:
```csharp
public void OnInput(NetworkRunner runner, NetworkInput input)
{
    var data = new NetworkInputData();
    
    if (Input.GetKey(KeyCode.W)) data.direction += Vector3.forward;
    if (Input.GetKey(KeyCode.S)) data.direction += Vector3.back;
    if (Input.GetKey(KeyCode.A)) data.direction += Vector3.left;
    if (Input.GetKey(KeyCode.D)) data.direction += Vector3.right;
    
    input.Set(data);
}
```

✅ **Verificar GetInput en Player.cs**:
```csharp
if (GetInput(out NetworkInputData data))
{
    // Si esto nunca se ejecuta, hay problema de InputAuthority
    Debug.Log($"Input recibido: {data.direction}");
}
```

✅ **Verificar InputAuthority en spawning**:
```csharp
NetworkObject playerObject = runner.Spawn(
    prefab,
    position,
    rotation,
    player  // ← IMPORTANTE: asignar InputAuthority
);
```

✅ **Verificar logs en Spawned()**:
```csharp
Debug.Log($"InputAuthority: {Object.HasInputAuthority}");
// Debe ser true para el cliente que controla este personaje
```

---

### 7. Múltiples personajes con mismo prefab

#### Síntomas
- Ambos jugadores spawnean como el mismo personaje
- No respeta la selección de personaje
- Siempre spawns Paladin (o el prefab por defecto)

#### Causa
- PlayerCharacterSelection no guarda selección
- BasicSpawner no lee la selección correctamente
- Prefab cacheado incorrecto

#### Solución
✅ **Verificar PlayerCharacterSelection**:
```csharp
public static class PlayerCharacterSelection
{
    public static string SelectedCharacter { get; set; } = "Paladin";
}
```

✅ **Verificar spawning en BasicSpawner**:
```csharp
public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    if (runner.IsServer)
    {
        string character = PlayerCharacterSelection.SelectedCharacter;
        Debug.Log($"Spawning: {character} para {player}");
        
        NetworkObject prefab = GetPrefabForCharacter(character);
        runner.Spawn(prefab, spawnPos, Quaternion.identity, player);
    }
}
```

✅ **Verificar método GetPrefabForCharacter**:
```csharp
private NetworkObject GetPrefabForCharacter(string character)
{
    switch (character)
    {
        case "Paladin": return paladinPrefab;
        case "Bruja": return brujaPrefab;
        case "Arquera": return arqueraPrefab;
        case "Cirujano": return cirujanoPrefab;
        default:
            Debug.LogWarning($"Personaje desconocido: {character}");
            return paladinPrefab;
    }
}
```

---

## 🛠️ Herramientas de Debug

### Logs Útiles

#### En Awake
```csharp
Debug.Log($"[{name}] Animator: {_animator?.name ?? "NULL"} | " +
          $"Visual Model: {_visualModel?.name ?? "NULL"}");
```

#### En Spawned
```csharp
Debug.Log($"[{name}] Spawned | " +
          $"InputAuth: {Object.HasInputAuthority} | " +
          $"StateAuth: {Object.HasStateAuthority}");
```

#### En FixedUpdateNetwork
```csharp
Debug.Log($"[{name}] Input: {data.direction} | " +
          $"Position: {transform.position} | " +
          $"IsWalking: {IsWalking}");
```

#### En Render
```csharp
Debug.Log($"[{name}] Render | " +
          $"IsWalking: {IsWalking} | " +
          $"MoveDir: {MoveDirection}");
```

### Fusion Stats (F2 en Runtime)
- **RTT**: Round Trip Time (latencia)
- **Bandwidth**: Ancho de banda usado
- **Objects**: Cantidad de NetworkObjects
- **Tick Rate**: Velocidad de simulación

### Unity Inspector en Runtime
- Ver NetworkObject → State Authority
- Ver NetworkTransform → Position/Rotation actual
- Ver Player → Variables [Networked]

---

## 📋 Checklist de Verificación

### Antes de Testing

#### Prefab Configuration
- [ ] NetworkObject en raíz
- [ ] NetworkTransform en raíz
- [ ] Player.cs en raíz
- [ ] Animator en hijo
- [ ] SkinnedMeshRenderer en hijo
- [ ] NO NetworkMecanimAnimator

#### Animator Controller
- [ ] Parámetro "Walk" existe (Bool)
- [ ] Transiciones Idle ↔ Walk
- [ ] HasExitTime: false en transiciones

#### Script Configuration
- [ ] _visualModel se inicializa en Awake
- [ ] IsWalking es [Networked]
- [ ] MoveDirection es [Networked]
- [ ] FixedUpdateNetwork verifica HasStateAuthority
- [ ] Render actualiza Animator y rotación

#### Network Configuration
- [ ] BasicSpawner implementa OnInput
- [ ] NetworkRunner en escena
- [ ] Prefabs asignados en Inspector

### Durante Testing

#### Local (Sin Red)
- [ ] Funciona con TestAnimator.cs
- [ ] Movimiento en línea recta
- [ ] Rotación correcta
- [ ] Animación Walk activa/desactiva

#### Multiplayer (Con Red)
- [ ] Host se mueve correctamente
- [ ] Cliente se mueve correctamente
- [ ] Ambos ven movimiento del otro
- [ ] Animaciones sincronizadas
- [ ] Rotación sincronizada

---

## 🚨 Debugging Extremo

Si nada funciona, seguir estos pasos:

### 1. Aislar el Problema
```csharp
// Simplificar Player.cs al mínimo
public override void FixedUpdateNetwork()
{
    if (GetInput(out NetworkInputData data) && HasStateAuthority)
    {
        transform.position += Vector3.forward * 5f * Runner.DeltaTime;
        Debug.Log($"Moviendo: {transform.position}");
    }
}
```

Si esto funciona → problema en lógica compleja
Si esto NO funciona → problema de networking/autoridad

### 2. Verificar Authority
```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Debug.Log($"InputAuth: {Object.HasInputAuthority} | " +
                  $"StateAuth: {Object.HasStateAuthority}");
    }
}
```

### 3. Test Sin Fusion
Usar TestAnimator.cs en escena local. Si funciona ahí:
→ Problema es de networking
→ Revisar sección de networking

Si NO funciona:
→ Problema es de jerarquía/modelo
→ Revisar jerarquía del prefab

### 4. Comparar con Prefab Funcional
Si un personaje funciona y otro no:
1. Comparar jerarquías en Inspector
2. Comparar componentes
3. Verificar diferencias en modelos 3D

---

## 📞 Soporte

Si después de seguir esta guía el problema persiste:

1. **Revisar documentación**:
   - `README.md` → Visión general
   - `PIVOT_SOLUTION.md` → Problema de pivot
   - `NETWORK_ARCHITECTURE.md` → Arquitectura de red

2. **Logs completos**:
   - Incluir logs de Awake, Spawned, FixedUpdateNetwork
   - Screenshot de Inspector del prefab
   - Screenshot de Fusion Stats (F2)

3. **Información del entorno**:
   - Unity version
   - Fusion version
   - SO (Windows/Mac)
   - Red local o internet

4. **Pasos para reproducir**:
   - Detalle paso a paso
   - Comportamiento esperado vs actual
   - Screenshots/videos si es posible
