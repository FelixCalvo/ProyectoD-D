# Configuración del Sistema Multiplayer RTS

## Estado del Sistema

**✓ COMPLETADO:** Integración del sistema de combate RTS en multiplayer  
**Fecha:** 5 diciembre 2025

### Archivos Modificados

1. **`Assets/Scripts/NewGame/Player.cs`** - Controlador del jugador con NavMesh + combate + red
   - Sistema completo de combate dual (Attack1 melee, Attack2 ranged)
   - NavMeshAgent para pathfinding
   - Sincronización de red con Photon Fusion
   - Variables `[Networked]` para ataque, target, animaciones

2. **`Assets/Scripts/NewGame/NetworkInputData.cs`** - Estructura de datos de input
   - Movimiento WASD (`direction`)
   - Comandos de ataque (`attackCommand`, `targetPlayerId`)
   - Comandos de movimiento (`moveCommand`, `targetPosition`)

3. **`Assets/Scripts/NewGame/BasicSpawner.cs`** - Captura de input del ratón
   - Clic derecho en enemigo → ataque
   - Clic derecho en suelo → movimiento

---

## Pasos de Configuración

### 1. **Configurar NavMesh en la escena Multiplayer**

⚠️ **CRÍTICO:** Sin NavMesh, el sistema de pathfinding NO funcionará.

**Pasos:**
1. Abre la escena `Multiplayer`
2. Selecciona el suelo/terreno de la escena
3. Ve a **Window → AI → Navigation**
4. En la pestaña **Object**, marca el suelo como **Navigation Static**
5. Ve a la pestaña **Bake**
6. Ajusta configuración:
   - **Agent Radius:** 0.3
   - **Agent Height:** 2.0
   - **Max Slope:** 45
   - **Step Height:** 0.4
7. Haz clic en **Bake**
8. Verifica que aparezca una malla azul sobre el suelo navegable

---

### 2. **Configurar Layer Masks**

#### **A. Crear Layers necesarios:**
1. Ve a **Edit → Project Settings → Tags and Layers**
2. Crea los siguientes User Layers si no existen:
   - `Player` (ejemplo: Layer 6)
   - `Ground` (ejemplo: Layer 7)

#### **B. Asignar Layers a GameObjects:**

**En la escena Multiplayer:**
- **Suelo/Terreno:** Layer `Ground`
- **Prefabs de personajes:** Layer `Player`

**En cada prefab de personaje** (Paladin, Cirujano, Arquera, Bruja):
1. Abre el prefab
2. Selecciona el GameObject raíz
3. Asigna Layer `Player` (marca "Yes, change children" si pregunta)

---

### 3. **Configurar el componente `Player.cs` en cada prefab**

Para cada prefab de personaje (`Assets/Prefabs/Characters/`):

#### **A. Agregar NavMeshAgent**
1. Selecciona el prefab
2. Si no tiene **NavMeshAgent**, agrégalo: **Add Component → NavMeshAgent**
3. Configuración:
   - **Speed:** 5
   - **Angular Speed:** 120
   - **Acceleration:** 8
   - **Stopping Distance:** 0.1
   - **Auto Braking:** ✓ (activado)
   - **Radius:** 0.3
   - **Height:** 2
   - **Obstacle Avoidance:** None

#### **B. Configurar `Player.cs`**
1. Asegúrate que el prefab tiene el componente **Player.cs**
2. Configuración en el Inspector:

**Movement:**
- **Move Speed:** 5

**Combat:**
- **Melee Attack Range:** 2.5
- **Melee Stopping Distance:** 1.5
- **Ranged Attack Range:** 8
- **Ranged Stopping Distance:** 5
- **Attack Cooldown:** 3.5
- **Enemy Layer:** `Player` (selecciona el Layer Mask)

**Attack Type:**
- **Paladin:** `Has Ranged Attack` ✗ (solo melee)
- **Cirujano:** `Has Ranged Attack` ✗ (solo melee)
- **Arquera:** `Has Ranged Attack` ✓ (melee + ranged)
- **Bruja:** `Has Ranged Attack` ✓ (melee + ranged)

#### **C. Verificar Animator Controller**
1. Selecciona el **modelo visual hijo** (el que tiene el Animator)
2. Verifica que tiene el **Animator Controller** con:
   - **Walk** (bool)
   - **Attack1** (trigger)
   - **Attack2** (trigger) - solo para personajes con ranged attack
   - **Idle** (estado default)

---

### 4. **Configurar BasicSpawner en la escena**

1. Abre la escena **Multiplayer**
2. Selecciona el GameObject con **BasicSpawner**
3. Asegúrate que los prefabs en **`_characterPrefabs`** son los configurados en el paso 3

---

### 5. **Configurar Photon Fusion**

Si aún no está configurado:
1. Ve a **Fusion → Fusion Hub**
2. Asegúrate que tienes un **App Id** de Photon
3. Verifica configuración de red en **Edit → Project Settings → Photon**

---

## Pruebas

### **Test 1: Movimiento con NavMesh**
1. Inicia el juego en modo Host
2. Spawm un personaje
3. Clic derecho en el suelo → El personaje debe moverse con pathfinding
4. Verifica que la animación **Walk** se activa

### **Test 2: Ataque Melee (Paladin/Cirujano)**
1. Spawm 2 personajes
2. Clic derecho en el otro personaje
3. El atacante debe:
   - Perseguir al objetivo
   - Detenerse a **1.5m** de distancia
   - Activar animación **Attack1**
   - Atacar cada **3.5 segundos**

### **Test 3: Ataque Ranged (Arquera/Bruja)**
1. Spawm Arquera o Bruja
2. Spawm otro personaje a **6m** de distancia
3. Clic derecho en el objetivo
4. El atacante debe:
   - Perseguir hasta **5m** de distancia (ranged stopping distance)
   - Activar animación **Attack2**
5. Acércate a **2m** del objetivo:
   - Debe cambiar automáticamente a **Attack1** (melee)

### **Test 4: Sincronización en Red**
1. Inicia como **Host**
2. En otra instancia, únete como **Client**
3. Desde el Host, ataca al personaje del Client
4. Verifica que ambos clientes ven:
   - Animaciones sincronizadas
   - Movimiento del NavMeshAgent
   - Rotación hacia el objetivo

### **Test 5: Interrupción de Ataque**
1. Inicia un ataque contra un enemigo
2. Mientras ataca, clic derecho en el suelo lejos
3. Verifica:
   - Animación cambia a **Walk** inmediatamente
   - El personaje se mueve al punto clicado
   - No hay deslizamiento (sliding)

---

## Debugging

### **Problema:** Personaje no se mueve
- ✓ Verifica que hay NavMesh bakeado (malla azul visible en Scene view)
- ✓ Verifica que el personaje está sobre NavMesh (Gizmo de agente visible)
- ✓ Verifica que NavMeshAgent está activo en el prefab
- ✓ Verifica Layer `Ground` en el suelo

### **Problema:** Clic derecho no detecta suelo/enemigos
- ✓ Verifica Layers `Player` y `Ground` configurados
- ✓ Verifica que el suelo tiene Collider
- ✓ Verifica que los personajes tienen Collider
- ✓ Abre **BasicSpawner.cs** y revisa los LayerMasks en `OnInput()`

### **Problema:** Animaciones no se sincronizan
- ✓ Verifica que las variables `[Networked]` están declaradas en Player.cs
- ✓ Verifica que el Animator Controller tiene los triggers correctos
- ✓ Verifica que `Render()` está ejecutándose (agrega Debug.Log)

### **Problema:** Personaje se desliza durante ataque
- ✓ Verifica que `_agent.velocity = Vector3.zero` se ejecuta en `UpdateCombat()`
- ✓ Verifica que `attackCooldown` es mayor que la duración de la animación

### **Problema:** Ataque no cambia entre melee/ranged
- ✓ Verifica que `hasRangedAttack` está activado (Arquera/Bruja)
- ✓ Verifica que el Animator tiene el trigger `Attack2`
- ✓ Verifica que las animaciones Attack2 existen en el Animator Controller

---

## Sistema de Combate - Referencia Rápida

### **Variables Clave:**

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `meleeAttackRange` | 2.5 | Distancia máxima para Attack1 |
| `meleeStoppingDistance` | 1.5 | Distancia de parada para melee |
| `rangedAttackRange` | 8 | Distancia máxima para Attack2 |
| `rangedStoppingDistance` | 5 | Distancia de parada para ranged |
| `attackCooldown` | 3.5 | Tiempo entre ataques (segundos) |

### **Lógica de Ataque:**

```csharp
if (hasRangedAttack && distance > meleeAttackRange) {
    // Usar Attack2 (ranged)
    stoppingDistance = 5f;
    trigger = "Attack2";
} else {
    // Usar Attack1 (melee)
    stoppingDistance = 1.5f;
    trigger = "Attack1";
}
```

### **Flujo de Combate:**

1. **Clic derecho en enemigo** → `AttackTarget(Player target)`
2. **FixedUpdateNetwork()** → `UpdateCombat()`
3. **UpdateCombat()** decide ranged/melee → ajusta `_isUsingRangedAttack`
4. **ChaseTarget()** persigue con NavMesh → ajusta `stoppingDistance`
5. **Alcanza rango** → `Attack()` activa trigger
6. **Cooldown 3.5s** → repite desde paso 3

---

## Comparación Singleplayer vs Multiplayer

| Aspecto | Singleplayer (RTSUnit.cs) | Multiplayer (Player.cs) |
|---------|---------------------------|-------------------------|
| **Pathfinding** | ✓ NavMeshAgent local | ✓ NavMeshAgent sincronizado |
| **Combat** | ✓ Attack1 + Attack2 | ✓ Attack1 + Attack2 |
| **Animaciones** | ✓ Walk, Attack1, Attack2 | ✓ Walk, Attack1, Attack2 |
| **Input** | RTSController (clic directo) | BasicSpawner (NetworkInputData) |
| **Target** | RTSUnit reference | Player + NetworkId |
| **Sincronización** | No necesaria | `[Networked]` variables |
| **Rotación** | Transform local | VisualModel (hijo) |

---

## Próximos Pasos (Opcional)

- [ ] Implementar barra de vida para visualizar daño
- [ ] Agregar efectos de partículas en ataques
- [ ] Implementar sistema de equipos (Team 1 vs Team 2)
- [ ] Agregar UI de selección de múltiples unidades
- [ ] Implementar formaciones de grupo
- [ ] Agregar habilidades especiales por clase

---

## Contacto y Soporte

Para reportar bugs o pedir ayuda:
- Revisa `CHANGELOG.md` para historial de cambios
- Revisa `DEBUG_SOLUTIONS.md` para soluciones conocidas
- Contacta al equipo de desarrollo
