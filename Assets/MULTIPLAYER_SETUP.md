# Configuración de Multiplayer con Jugadores Pre-colocados

## 🎯 Concepto
En lugar de spawnear prefabs dinámicamente, los 4 jugadores YA EXISTEN en la escena NewGame, desactivados por defecto. Cuando un jugador se conecta, simplemente se activa el GameObject correspondiente.

---

## 📋 Pasos de Configuración

### 1. Escena NewGame - Colocar Jugadores

Coloca los 4 GameObjects de jugadores en la escena:

```
NewGame (Scene)
├── Player_Paladin
│   ├── NetworkObject (componente)
│   ├── Player (componente)
│   ├── NavMeshAgent
│   ├── RTSUnit
│   ├── RTSUnitAvoidance
│   └── CM_PaladinCamera (hijo con CinemachineCamera)
│
├── Player_Bruja
│   └── CM_BrujaCamera
│
├── Player_Arquera
│   └── CM_ArqueraCamera
│
└── Player_CirujanoBarero
    └── CM_CirujanoCamera
```

**IMPORTANTE:**
- ✅ Los 4 jugadores deben estar **DESACTIVADOS** en la escena (checkbox desactivado)
- ✅ Cada uno debe tener **NetworkObject** configurado así:
  - `Allow State Authority`: ✅ Activado
  - `Destroy When State Authority Leaves`: ❌ Desactivado (para no destruirlo al desconectar)
  - **Networking Type**: Debe ser `Scene Object` (por defecto para objetos en la escena)
  - Fusion los registrará automáticamente cuando se activen
- ✅ Cada uno debe tener su **CinemachineCamera** como hijo
- ✅ Las posiciones en la escena son donde aparecerán al conectarse

**NOTA:** Los NetworkObjects en la escena son automáticamente registrados por Fusion cuando:
1. El GameObject está activo
2. El NetworkRunner está ejecutándose
Por eso solo necesitamos activarlos y asignar InputAuthority, NO spawnerlos manualmente.

---

### 2. BasicSpawner - Asignar Referencias

En el GameObject que tenga `BasicSpawner.cs`:

1. **Preplaced Players** (array de 4):
   - Slot 0: Player_Paladin
   - Slot 1: Player_Bruja
   - Slot 2: Player_Arquera
   - Slot 3: Player_CirujanoBarero

2. **Lista Partidas UI**: Asigna tu UI de lista de partidas

---

### 3. DialogueCameraManager - Asignar Cámaras

En el GameObject que tenga `DialogueCameraManager.cs`:

1. **Player Cameras** (array de 4):
   ```
   Element 0:
     - Player: Player_Paladin
     - Cinemachine Camera: CM_PaladinCamera
   
   Element 1:
     - Player: Player_Bruja
     - Cinemachine Camera: CM_BrujaCamera
   
   Element 2:
     - Player: Player_Arquera
     - Cinemachine Camera: CM_ArqueraCamera
   
   Element 3:
     - Player: Player_CirujanoBarero
     - Cinemachine Camera: CM_CirujanoCamera
   ```

---

### 4. RTSController - Detectar Jugadores

`RTSController` ya auto-detecta todas las unidades con `RTSUnit` en la escena.

**NO NECESITAS** asignarlas manualmente si están en la escena (activos o no).

---

## 🔄 Flujo de Conexión

### Cuando un jugador se conecta:

1. **BasicSpawner.OnPlayerJoined()** se ejecuta
2. Se llama a **ActivatePlayer(PlayerRef)**
3. Se determina qué GameObject usar basándose en:
   - Selección de personaje del jugador
   - Matching por nombre (Paladin, Bruja, etc.)
4. Se **activa el GameObject** (`SetActive(true)`)
5. Se asigna **InputAuthority** al jugador conectado
6. **RTSController** detecta automáticamente el nuevo jugador activo
7. **DialogueCameraManager** usa las referencias ya asignadas

### Cuando un jugador se desconecta:

1. **BasicSpawner.OnPlayerLeft()** se ejecuta
2. Se **desactiva el GameObject** (`SetActive(false)`)
3. El GameObject permanece en la escena (puede reconectar)
4. **RTSController** lo ignora automáticamente (está inactivo)

---

## ✅ Ventajas del Sistema

✅ **Reutiliza TODO el código de singleplayer**
- RTSController funciona igual
- DialogueCameraManager funciona igual
- TransparentarParedes funciona igual
- NPCs pueden interactuar con jugadores aunque no haya nadie conectado

✅ **Configuración visual**
- Ves las posiciones de spawn en el Editor
- Cámaras ya asignadas en el Inspector
- Fácil de debuggear

✅ **Sin código dinámico complejo**
- No necesita registro de cámaras
- No necesita búsqueda de referencias en runtime
- No necesita sincronización de spawns

✅ **Persistencia**
- Los jugadores desconectados simplemente se desactivan
- Pueden reconectar al mismo GameObject
- El estado del juego se mantiene

---

## 🔧 Testing

### En el Editor:

1. **Singleplayer**: Activa los 4 jugadores manualmente → funciona como siempre
2. **Multiplayer Local**: 
   - Deja los 4 jugadores desactivados
   - Inicia Host
   - Inicia Client en otra ventana
   - Verifica que se activan según los jugadores conectados

### En Build:

1. Construye el juego
2. Ejecuta instancia Host
3. Ejecuta instancia Client
4. Verifica que cada cliente controla su jugador
5. Verifica que las cámaras siguen al jugador activo

---

## 🐛 Troubleshooting

### "Los jugadores no se activan"
- Verifica que están asignados en `BasicSpawner.preplacedPlayers`
- Verifica que tienen componente `NetworkObject`
- Verifica que `Allow State Authority` está activado

### "Las cámaras no funcionan"
- Verifica que están asignadas en `DialogueCameraManager.playerCameras`
- Verifica que las CinemachineCamera están como hijos de los jugadores
- Verifica que `HelperClass.ActivePlayer` se está actualizando

### "RTSController no detecta los jugadores"
- Los jugadores desactivados NO se detectan (correcto)
- Cuando se activen, deberían detectarse automáticamente
- Verifica que tienen componente `RTSUnit`

### "InputAuthority no funciona"
- Solo el Server puede asignar InputAuthority
- Verifica que `runner.IsServer` es true
- Verifica logs de "InputAuthority asignada"

---

## 📝 Notas Adicionales

- **NetworkTransform**: Debe estar desactivado (NavMeshAgent controla la posición)
- **Layer**: Los jugadores deben estar en layer "Player"
- **NavMesh**: Asegúrate de que las posiciones de spawn están sobre el NavMesh
- **Colliders**: Los jugadores necesitan colliders para clicks y detección

---

## 🎮 Orden de Jugadores

El sistema intenta hacer match entre:
1. Selección de personaje del jugador (PlayerCharacterSelection)
2. Nombre del GameObject en la escena

Si no hay match, asigna el primer slot disponible.

Para forzar un orden específico, modifica `GetPlayerIndex()` en `BasicSpawner.cs`.

---

**✨ ¡Sistema listo para multiplayer sin complicaciones! ✨**
