# Arquitectura de Red - Photon Fusion

## 🌐 Visión General
El proyecto usa **Photon Fusion 2.x** con arquitectura **Server-Authoritative** donde el servidor (Host) tiene autoridad sobre el estado del juego y los clientes envían inputs que el servidor procesa.

## 🏛️ Arquitectura General

### Modelo de Red
```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│  Cliente 1  │         │   Servidor  │         │  Cliente 2  │
│   (Host)    │◄────────┤   (Host)    ├────────►│   (Join)    │
└─────────────┘         └─────────────┘         └─────────────┘
      │                        │                        │
      ▼                        ▼                        ▼
 Input Authority          State Authority          Input Authority
 para Player 1           para TODOS               para Player 2
```

### Roles de Clients

#### Host (Cliente 1)
- **State Authority**: Autoridad sobre TODOS los objetos
- **Input Authority**: Solo sobre su propio personaje
- Ejecuta la simulación del servidor
- Procesa inputs de todos los clientes

#### Joined Client (Cliente 2+)
- **Input Authority**: Solo sobre su propio personaje
- Recibe estado sincronizado desde el Host
- Envía inputs al Host
- Renderiza el estado recibido

## 📦 Componentes de Red

### NetworkObject
Identifica un objeto como parte de la simulación de red. Lo tiene por ejemplo el padre de los player para indicar a fusión que los maneje. Mirar NetworkProjectConfig de fusión, ahí aparecen los prefabs.

**Configuración**:
```
NetworkObject
├── Object Interest: Auto
├── Allow State Authority Override: No
└── Destroy When State Authority Leaves: Yes
```

**Ubicación**: En la raíz de cada prefab de personaje.

###    
Sincroniza automáticamente posición y rotación.

**Configuración actual**:
```
NetworkTransform
├── Sync Position: Yes
├── Sync Rotation: No (rotamos el hijo manualmente)
├── Interpolation: Default
└── Auto AOI Override: Yes
```

**Importante**: Solo sincroniza el transform raíz(padre), NO EL HIJO VISUAL!!!.

### NetworkBehaviour
Clase base para scripts que necesitan funcionalidad de red.

**Métodos importantes**:
- `Spawned()`: Llamado cuando el objeto es creado en red
- `FixedUpdateNetwork()`: Update de física en ticks de Fusion
- `Render()`: Update visual cada frame
- `GetInput<T>()`: Obtener input del jugador

## 🔄 Flujo de Datos

### 1. Input Collection (Clientes)
```csharp
// BasicSpawner.cs - OnInput()
public void OnInput(NetworkRunner runner, NetworkInput input)
{
    var data = new NetworkInputData();
    
    // Recoger input WASD
    if (Input.GetKey(KeyCode.W)) data.direction += Vector3.forward;
    if (Input.GetKey(KeyCode.S)) data.direction += Vector3.back;
    if (Input.GetKey(KeyCode.A)) data.direction += Vector3.left;
    if (Input.GetKey(KeyCode.D)) data.direction += Vector3.right;
    
    input.Set(data);  // Enviar al servidor
}
```

### 2. Input Processing (Servidor)
```csharp
// Player.cs - FixedUpdateNetwork()
public override void FixedUpdateNetwork()
{
    // Solo el servidor/host con StateAuthority procesa
    if (GetInput(out NetworkInputData data) && HasStateAuthority)
    {
        Vector3 direction = data.direction.normalized;
        
        // Modificar estado
        transform.position += direction * speed * Runner.DeltaTime;
        MoveDirection = direction;
        IsWalking = true;
    }
}
```

### 3. State Synchronization (Automático)
Fusion sincroniza automáticamente:
- Variables con `[Networked]`
- Componentes `NetworkTransform`
- Estado de `NetworkObject`

### 4. Visual Update (Todos los Clientes)
```csharp
// Player.cs - Render()
public override void Render()
{
    // Se ejecuta en TODOS los clientes cada frame
    _animator.SetBool("Walk", IsWalking);
    _visualModel.rotation = Quaternion.LookRotation(MoveDirection);
}
```

## 🔐 Authority System

### State Authority
- **Quién**: Solo el Host
- **Qué controla**: Posición, variables [Networked], lógica de juego
- **Dónde**: `FixedUpdateNetwork()` con `if (HasStateAuthority)`

### Input Authority
- **Quién**: Cada cliente para su personaje
- **Qué controla**: Input del teclado/ratón
- **Dónde**: `OnInput()` en BasicSpawner

### Verificaciones Importantes
```csharp
// ✅ Correcto: Solo servidor modifica estado
if (HasStateAuthority)
{
    transform.position += movement;
    IsWalking = true;
}

// ✅ Correcto: Todos renderizan
_animator.SetBool("Walk", IsWalking);

// ❌ Incorrecto: Cliente modificando estado
transform.position += movement;  // Sin verificar HasStateAuthority
```

## 📊 Variables de Red

### Definición
```csharp
[Networked] private NetworkBool IsWalking { get; set; }
[Networked] private Vector3 MoveDirection { get; set; }
```

### Características
- Se sincronizan automáticamente a todos los clientes
- Solo el servidor/StateAuthority puede modificarlas
- Clientes leen valores sincronizados
- Consumen ancho de banda → usar solo lo necesario

### Best Practices
```csharp
// ✅ Bien: Variables necesarias
[Networked] private NetworkBool IsWalking { get; set; }
[Networked] private Vector3 MoveDirection { get; set; }

// ❌ Mal: Sincronizar cosas que se pueden calcular localmente
[Networked] private float CurrentSpeed { get; set; }  // Se puede calcular
[Networked] private bool IsMovingForward { get; set; }  // Redundante
```

## 🔧 Spawning System

### Flujo de Spawning
```
1. Cliente conecta → BasicSpawner.OnPlayerJoined()
2. Servidor lee selección de personaje
3. Servidor instancia prefab correcto
4. Fusion crea NetworkObject en todos los clientes
5. Llamada a Spawned() en todos
6. Cliente con InputAuthority controla ese personaje
```

### Código de Spawning
```csharp
// BasicSpawner.cs
public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    if (runner.IsServer)
    {
        // Obtener selección de personaje
        string selectedCharacter = PlayerCharacterSelection.SelectedCharacter;
        
        // Cargar prefab correcto
        NetworkObject prefab = GetPrefabForCharacter(selectedCharacter);
        
        // Spawn del personaje
        NetworkObject playerObject = runner.Spawn(
            prefab,
            spawnPosition,
            Quaternion.identity,
            player  // InputAuthority para este PlayerRef
        );
    }
}
```

## 🎮 Input System

### Estructura de Input
```csharp
public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;  // Dirección de movimiento WASD
}
```

### Recolección de Input
```csharp
// Se ejecuta cada tick en el cliente
public void OnInput(NetworkRunner runner, NetworkInput input)
{
    var data = new NetworkInputData();
    
    // Espacio mundo (no relativo a cámara)
    if (Input.GetKey(KeyCode.W)) data.direction += Vector3.forward;
    if (Input.GetKey(KeyCode.S)) data.direction += Vector3.back;
    if (Input.GetKey(KeyCode.A)) data.direction += Vector3.left;
    if (Input.GetKey(KeyCode.D)) data.direction += Vector3.right;
    
    input.Set(data);
}
```

### Consumo de Input
```csharp
// FixedUpdateNetwork en Player.cs
if (GetInput(out NetworkInputData data))
{
    // data contiene el input del jugador que tiene InputAuthority
    Vector3 direction = data.direction.normalized;
    // ...
}
```

## ⏱️ Timing y Updates

### Tipos de Updates
```
┌─────────────────────────────────────────────────┐
│  Unity Update Loop                              │
├─────────────────────────────────────────────────┤
│  Update()           │ Cada frame (~60 FPS)      │
│  FixedUpdate()      │ Física (~50 FPS)          │
├─────────────────────────────────────────────────┤
│  Fusion Update Loop                             │
├─────────────────────────────────────────────────┤
│  FixedUpdateNetwork() │ Ticks de simulación     │
│                       │ (configurable, ~60 Hz)  │
│  Render()            │ Cada frame visual        │
└─────────────────────────────────────────────────┘
```

### Uso Correcto
```csharp
// ✅ Lógica de juego/física → FixedUpdateNetwork
public override void FixedUpdateNetwork()
{
    transform.position += movement * Runner.DeltaTime;
}

// ✅ Actualización visual → Render
public override void Render()
{
    _animator.SetBool("Walk", IsWalking);
}

// ❌ NO usar Update() para lógica de red
void Update()
{
    // Evitar modificar estado de red aquí
}
```

### DeltaTime
```csharp
// ✅ En FixedUpdateNetwork
Runner.DeltaTime  // Tiempo entre ticks de Fusion

// ✅ En Update/Render
Time.deltaTime    // Tiempo entre frames

// ❌ Incorrecto
Time.deltaTime en FixedUpdateNetwork  // Inconsistente
```

## 🔍 Debugging

### Logs Útiles
```csharp
public override void Spawned()
{
    Debug.Log($"Spawned: {name} | " +
              $"InputAuth: {Object.HasInputAuthority} | " +
              $"StateAuth: {Object.HasStateAuthority}");
}
```

**Output esperado**:
```
Host (Player 1):    InputAuth: True  | StateAuth: True
Client (Player 1):  InputAuth: False | StateAuth: False
Client (Player 2):  InputAuth: True  | StateAuth: False
```

### Verificar Sincronización
```csharp
// Añadir en Render para ver si las variables se sincronizan
Debug.Log($"[{name}] IsWalking: {IsWalking} | Direction: {MoveDirection}");
```

### Inspector de Red
Usar el **Fusion Stats Panel** (F2 en Runtime):
- RTT (Round Trip Time)
- Bandwidth
- Object Count
- Simulation Stats

## ⚡ Optimización

### Reducir Ancho de Banda
```csharp
// ✅ Bien: Solo datos esenciales
[Networked] private NetworkBool IsWalking { get; set; }
[Networked] private Vector3 MoveDirection { get; set; }

// ❌ Mal: Sincronizar todo
[Networked] private float CurrentSpeed { get; set; }
[Networked] private Vector3 Velocity { get; set; }
[Networked] private float LastMoveTime { get; set; }
```

### Interest Management
NetworkTransform con **Auto AOI Override** activo:
- Solo sincroniza a jugadores cercanos
- Reduce carga en juegos grandes

### Interpolación
```csharp
// NetworkTransform maneja interpolación automáticamente
// Para movimiento custom, considerar:
Vector3 smoothPosition = Vector3.Lerp(
    currentPos,
    targetPos,
    Time.deltaTime * smoothSpeed
);
```

## 🎯 Best Practices

### 1. Separar Lógica
```csharp
// ✅ FixedUpdateNetwork: Modificar estado (solo servidor)
public override void FixedUpdateNetwork()
{
    if (HasStateAuthority)
    {
        transform.position += movement;
    }
}

// ✅ Render: Actualizar visuales (todos)
public override void Render()
{
    _animator.SetBool("Walk", IsWalking);
}
```

### 2. Verificar Authority
```csharp
// ✅ Siempre verificar antes de modificar estado
if (HasStateAuthority)
{
    IsWalking = true;
}
```

### 3. Minimizar Variables de Red
```csharp
// ✅ Solo lo necesario
[Networked] private Vector3 MoveDirection { get; set; }

// ❌ Evitar redundancia
[Networked] private float MoveDirectionX { get; set; }
[Networked] private float MoveDirectionY { get; set; }
[Networked] private float MoveDirectionZ { get; set; }
```

### 4. Usar NetworkTransform Cuando Sea Posible
```csharp
// ✅ Dejar que NetworkTransform sincronice posición
transform.position += movement;  // Se sincroniza automáticamente

// ❌ Sincronización manual innecesaria
[Networked] private Vector3 Position { get; set; }
```

## 📚 Referencias

- [Photon Fusion Documentation](https://doc.photonengine.com/fusion)
- [Fusion 100 Series Tutorial](https://doc.photonengine.com/fusion/current/tutorials/fusion-100/fusion-intro)
- [Network Object Documentation](https://doc.photonengine.com/fusion/current/manual/network-object)
- [Networked Properties](https://doc.photonengine.com/fusion/current/manual/network-behaviour/networked-properties)
