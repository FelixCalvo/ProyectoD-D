# Proyecto D&D - Documentación

## 📋 Descripción General
Juego multijugador de rol basado en D&D o Aquelarre, aún por decidir, desarrollado con Unity y Photon Fusion. Los jugadores pueden elegir entre diferentes personajes (Paladin, Bruja, Arquera, Cirujano Barbero) y moverse por un mundo compartido.

## 🎮 Características Principales
- **Multijugador**: Hasta 4 jugadores simultáneos usando Photon Fusion
- **Selección de Personajes**: 4 clases jugables con modelos 3D animados
- **Movimiento Sincronizado**: Sistema de movimiento con animaciones Walk sincronizadas
- **Input System**: Control mediante WASD en espacio mundo
- **Sistema de Diálogos**: Integración con Fungus + localización automática
- **Diario de Aventuras**: Sistema que registra conversaciones y decisiones del jugador
- **Localización Multiidioma**: Español, Inglés y Catalán con sistema `<#CLAVES>`

## 🏗️ Arquitectura del Proyecto

### Estructura de Carpetas
```
ProyectoD&D-Aquelarre/
├── Assets/
│   ├── Animations/          # Animaciones y Animator Controllers
│   ├── Materials/           # Materiales para modelos
│   ├── Meshes/             # Modelos 3D de personajes
│   ├── Photon/             # SDK de Photon Fusion
│   ├── Prefabs/            # Prefabs de personajes y objetos
│   ├── Scenes/             # Escenas del juego
│   ├── Scripts/            # Scripts principales
│   └── Textures/           # Texturas
├── Docs/                   # Documentación del proyecto
└── ProjectSettings/        # Configuración de Unity
```

### Scripts Principales

#### 1. `Player.cs`
**Ubicación**: `Assets/Animations/AnimationsBasura/Scripts/NewGame/Player.cs`

**Responsabilidad**: Controlador principal del personaje en red.

**Componentes clave**:
- Movimiento del transform raíz (solo posición, sin rotación)
- Rotación del modelo visual hijo
- Sincronización de animaciones
- Manejo de input en red

**Variables de Red**:
- `IsWalking`: Estado de animación Walk
- `MoveDirection`: Dirección de movimiento para sincronizar rotación

#### 2. `BasicSpawner.cs`
**Ubicación**: `Assets/Photon/Fusion/Scripts/BasicSpawner.cs`

**Responsabilidad**: Gestión de conexión a red y spawning de jugadores.

**Funcionalidades**:
- Conexión a servidor Photon
- Spawning de personajes según selección
- Recolección de input (WASD)
- Gestión de sesión multijugador

#### 3. `NetworkInputData.cs`
**Responsabilidad**: Estructura de datos para input en red.

```csharp
struct NetworkInputData : INetworkInput
{
    public Vector3 direction;
}
```

#### 4. `PlayerCharacterSelection.cs`
**Responsabilidad**: Gestión de selección de personaje en lobby.

**Personajes disponibles**:
- Paladin
- Bruja
- Arquera
- Cirujano Barbero

#### 5. `TestAnimator.cs`
**Ubicación**: `Assets/Scripts/TestAnimator.cs`

**Responsabilidad**: Script de testing para probar movimiento sin red.

## 🎯 Solución al Problema del Pivot

### Problema Original
Los modelos de Mixamo tienen el pivot adelantado, no en el centro del personaje. Cuando se rotaba el transform raíz con `Quaternion.LookRotation()`, el modelo orbitaba alrededor del pivot creando un efecto de semicírculo al cambiar de dirección.

### Solución Implementada
**Separación de movimiento y rotación**:

1. **Transform Raíz (Padre)**:
   - Solo se mueve en línea recta
   - NUNCA rota
   - NetworkTransform sincroniza su posición

2. **Modelo Visual (Hijo)**:
   - Solo rota hacia la dirección de movimiento
   - El pivot adelantado ya no afecta porque no se mueve
   - Todos los clientes calculan la rotación localmente

**Diagrama de jerarquía**:
```
Prefab Root (NetworkObject, NetworkTransform, Player.cs)
    ├── transform.position += movement  // SOLO MOVIMIENTO
    └── Visual Model (Animator, SkinnedMeshRenderer)
        └── transform.rotation = LookRotation(direction)  // SOLO ROTACIÓN
```

### Código Clave
```csharp
// FixedUpdateNetwork (solo servidor con StateAuthority)
transform.position += direction * speed * deltaTime;  // Mover raíz
MoveDirection = direction;  // Sincronizar dirección

// Render (todos los clientes)
_visualModel.rotation = Quaternion.LookRotation(MoveDirection);  // Rotar hijo
```

## 🔧 Configuración de Prefabs

### Estructura de un Prefab de Personaje
```
Paladin (por ejemplo)
├── NetworkObject
├── NetworkTransform (sincroniza posición/rotación de raíz)
├── Player.cs (script de control)
└── Paladin_Model (hijo)
    ├── Animator (con parámetro Walk bool)
    ├── SkinnedMeshRenderer
    └── mixamorig:Hips (esqueleto)
```

### Componentes Requeridos
- **NetworkObject**: Identifica el objeto en red
- **NetworkTransform**: Sincroniza automáticamente posición
- **Player.cs**: Lógica de movimiento y animación
- **Animator**: Controlador de animaciones con parámetro "Walk"

### Configuración de NetworkTransform
- **Auto AOI Override**: Activado
- **Interpolation**: Default
- **Sync Parent**: Ninguno

 

### 1. Input
```
Cliente Local → OnInput() → NetworkInputData → Servidor
```

### 2. Procesamiento (Servidor)
```
FixedUpdateNetwork():
  - Lee NetworkInputData
  - Calcula movimiento
  - Actualiza transform.position
  - Guarda MoveDirection
  - Actualiza IsWalking
```

### 3. Sincronización
```
Fusion sincroniza automáticamente:
  - transform.position (vía NetworkTransform)
  - IsWalking (variable [Networked])
  - MoveDirection (variable [Networked])
```

### 4. Renderizado (Todos los Clientes)
```
Render():
  - Lee IsWalking → Actualiza Animator
  - Lee MoveDirection → Rota modelo visual
```

## 🎨 Sistema de Animaciones

### Animator Controller
**Ubicación**: `Assets/Animations/`

**Estados**:
- **Idle**: Animación por defecto
- **Walk**: Activado cuando `Walk = true`

**Transiciones**:
- Idle → Walk: `Walk = true`, HasExitTime: false
- Walk → Idle: `Walk = false`, HasExitTime: false

### Sincronización
- **NO usar NetworkMecanimAnimator** (conflictos con control manual)
- Usar variable `[Networked] NetworkBool IsWalking`
- Actualizar Animator localmente en `Render()`

## 🚀 Cómo Ejecutar

### 1. Testing Local (sin red)
1. Abrir escena: `Assets/Scenes/TestingPlayers`
2. Asignar `TestAnimator.cs` al personaje
3. Play → Usar WASD para mover

### 2. Testing Multijugador
1. Abrir escena: `Assets/Scenes/MainGame`
2. Build del proyecto (File → Build Settings → Build)
3. Ejecutar Build (cliente 1)
4. Play en Editor (cliente 2)
5. Ambos se conectan automáticamente

### Input
- **W**: Adelante (Vector3.forward)
- **S**: Atrás (Vector3.back)
- **A**: Izquierda (Vector3.left)
- **D**: Derecha (Vector3.right)

## 🐛 Problemas Resueltos

### 1. Movimiento en Semicírculo
**Causa**: Pivot adelantado + rotación del transform raíz
**Solución**: Separar movimiento (raíz) y rotación (hijo)

### 2. Animación No Sincroniza
**Causa**: NetworkMecanimAnimator con control manual
**Solución**: Variable `[Networked] NetworkBool` + actualización en `Render()`

### 3. Cliente No Rota Correctamente
**Causa**: Rotación solo en servidor
**Solución**: Variable `[Networked] Vector3 MoveDirection` + rotación en `Render()`

### 4. Teleporting al Cambiar Dirección
**Causa**: Suavizado de dirección creaba velocidad variable
**Solución**: Movimiento directo sin Lerp/Slerp

## 📝 Notas de Desarrollo

### Convenciones de Código
- Variables de red: PascalCase con `[Networked]`
- Variables privadas: `_camelCase`
- Variables públicas/serializadas: `camelCase`
- Métodos: PascalCase

### Debug
- Logs mínimos en producción
- Usar `Debug.LogWarning()` para errores no críticos
- Usar `Debug.LogError()` para errores críticos

### Performance
- Usar `Runner.DeltaTime` en lugar de `Time.deltaTime` en FixedUpdateNetwork
- Minimizar variables `[Networked]` (consumen ancho de banda)
- Evitar operaciones costosas en `Render()` (se ejecuta cada frame)

## 🔮 Próximas Características
- [ ] Sistema de combate
- [ ] Inventario
- [ ] Chat de texto
- [ ] Más animaciones (correr, atacar, saltar)
- [ ] Sistema de cámara mejorado
- [ ] UI de HUD

## 👥 Créditos
- **Engine**: Unity 6.0 (6000.0.58f2)
- **Networking**: Photon Fusion 2.0.8 (estable)
- **Modelos**: Mixamo
- **Desarrollador**: Felix & Claude Sonnet
