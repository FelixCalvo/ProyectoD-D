# Sistema de Localización y Diario de Aventuras para Fungus

## 📋 Resumen

Este proyecto implementa un sistema completo de **localización automática** y **diario de aventuras** para Fungus (Unity). El sistema permite:

- ✅ Traducción automática usando claves `<#KEY>` en comandos Say y Menu
- ✅ Localización case-insensitive (CLAVE = clave = Clave)
- ✅ Diario que guarda automáticamente diálogos y opciones del jugador
- ✅ Formato con speaker names en negrita
- ✅ Sistema completamente automático (sin configuración manual por opción)

---

## 🗂️ Estructura de Archivos

### **Archivos Modificados (Fungus Core)**

#### 1. `Say.cs` (Fungus)
**Ubicación**: `Assets/Fungus/Fungus/Scripts/Commands/Say.cs`

**Cambios realizados**:
- Añadido campo `protected string translatedText` para exponer el texto traducido a subclases
- Regex que reemplaza claves `<#KEY>` con traducciones del LocalizationManager
- Default text cambiado a `"<#>"` para nuevos comandos Say

**Fragmento clave**:
```csharp
protected string translatedText = "";

public override void OnEnter()
{
    // ... código original ...
    
    // 🌍 LOCALIZACIÓN: Traducir <#CLAVE>
    var locManagerType = System.Type.GetType("LocalizationManager, Assembly-CSharp");
    // ... búsqueda por reflexión ...
    
    displayText = System.Text.RegularExpressions.Regex.Replace(displayText, @"<#([A-Za-z0-9_]+)>", match =>
    {
        string key = match.Groups[1].Value;
        // ... invocar GetText ...
        return translation;
    });
    
    translatedText = displayText; // Guardar para subclases
    
    // ... resto del código original ...
}
```

**Por qué se modificó**: 
- Evita tener que crear un comando completamente nuevo
- Mantiene compatibilidad con todo el sistema Fungus existente
- `translatedText` permite que SayWithJournal acceda al texto ya traducido

---

#### 2. `Menu.cs` (Fungus)
**Ubicación**: `Assets/Fungus/Fungus/Scripts/Commands/Menu.cs`

**Cambios realizados**:
- Misma lógica de traducción regex que Say.cs
- Registro automático del texto traducido en MenuJournalTracker
- Sistema de reflexión para evitar dependencias de assembly

**Fragmento clave**:
```csharp
public override void OnEnter()
{
    // ... código original ...
    
    string displayText = flowchart.SubstituteVariables(text);
    
    // 🌍 LOCALIZACIÓN: Traducir claves <#CLAVE>
    // ... mismo sistema regex que Say.cs ...
    
    // 📝 JOURNAL: Registrar automáticamente el texto traducido
    if (targetBlock != null && !string.IsNullOrEmpty(displayText))
    {
        // Buscar MenuJournalTracker por reflexión
        System.Type trackerType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            trackerType = assembly.GetType("MenuJournalTracker");
            if (trackerType != null) break;
        }
        
        if (trackerType != null)
        {
            // Invocar RegisterMenuText(targetBlock, displayText)
            // ...
        }
    }
    
    menuDialog.AddOption(displayText, interactable, hideOption, targetBlock);
    Continue();
}
```

**Por qué se modificó**:
- Traduce automáticamente todas las opciones de menú
- Registra el texto para que LogSelectedMenu lo recupere cuando el jugador hace clic
- No requiere comandos adicionales por cada opción de menú

---

### **Scripts Nuevos (Sistema de Diario)**

#### 3. `LocalizationManager.cs`
**Ubicación**: `Assets/Scripts/LocalizationManager.cs`

**Función**: Singleton que carga archivos JSON de localización y proporciona traducciones case-insensitive.

**Características**:
- Carga JSON desde `Resources/Localization/{es|en|ca}.json`
- Almacena todas las keys en mayúsculas: `ToUpperInvariant()`
- Método `GetText(key)` normaliza keys a mayúsculas para búsqueda
- Logs de debug cuando faltan claves

**Estructura JSON esperada**:
```json
{
  "CLAVE1": "Texto traducido",
  "CLAVE2": "Otro texto"
}
```

**Código clave**:
```csharp
public string GetText(string key)
{
    string upperKey = key.ToUpperInvariant();
    if (localizedTexts.ContainsKey(upperKey))
    {
        return localizedTexts[upperKey];
    }
    Debug.LogWarning($"[LocalizationManager] Clave '{key}' no encontrada");
    return $"<#{key}>";
}
```

---

#### 4. `AdventureJournal.cs`
**Ubicación**: `Assets/Scripts/DiarioAventuras/AdventureJournal.cs`

**Función**: Singleton que almacena todas las entradas del diario.

**Estructura de datos**:
```csharp
public class JournalEntry
{
    public string speaker;
    public string text;
    public string sceneName;
}

public List<JournalEntry> entries = new List<JournalEntry>();
```

**Método principal**:
```csharp
public void AddEntry(string speaker, string text)
{
    entries.Add(new JournalEntry
    {
        speaker = speaker,
        text = text,
        sceneName = SceneManager.GetActiveScene().name
    });
}
```

---

#### 5. `JournalUI.cs`
**Ubicación**: `Assets/Scripts/DiarioAventuras/JournalUI.cs`

**Función**: Muestra las entradas del diario en un TextMeshProUGUI.

**Formato de salida**:
```
<b>Baron</b>
Texto del diálogo...

<b>Jugador</b>
→ Opción seleccionada

<b>Baron</b>
Respuesta del NPC...
```

**Métodos públicos**:
- `OpenJournal()`: Activa el GameObject y refresca contenido
- `CloseJournal()`: Desactiva el GameObject
- `Refresh()`: Reconstruye el texto desde AdventureJournal.Instance

---

#### 6. `SayWithJournal.cs`
**Ubicación**: `Assets/Scripts/DiarioAventuras/SayWithJournal.cs`

**Función**: Comando Fungus que extiende Say y guarda el texto en el diario.

**Uso en Flowchart**: Reemplaza comando "Say" normal con "Say With Journal"

**Código completo**:
```csharp
[CommandInfo("Narrative", "Say With Journal", "Say localizado que guarda el diálogo en el diario de aventuras")]
public class SayWithJournal : Say
{
    public override void OnEnter()
    {
        // Ejecuta TODO el Say normal (localización, UI, wait, etc.)
        base.OnEnter();

        if (AdventureJournal.Instance == null) return;

        string speakerName = character != null ? character.NameText : "Narrador";
        string finalText = translatedText; // Accede al texto traducido del Say padre

        if (!string.IsNullOrEmpty(finalText))
        {
            AdventureJournal.Instance.AddEntry(speakerName, finalText);
        }
    }
}
```

**Por qué funciona**:
- Hereda de Say, por lo que tiene acceso a `translatedText`
- `base.OnEnter()` hace toda la traducción y muestra el diálogo
- Luego simplemente guarda en el diario

---

#### 7. `MenuJournalTracker.cs` (archivo MenuWithJournal.cs)
**Ubicación**: `Assets/Scripts/DiarioAventuras/MenuWithJournal.cs`

**Función**: Singleton que rastrea el texto de cada opción de menú asociada a su Block de destino.

**Estructura interna**:
```csharp
private Dictionary<Fungus.Block, string> menuTexts = new Dictionary<Fungus.Block, string>();
```

**Métodos**:
```csharp
public void RegisterMenuText(Fungus.Block targetBlock, string menuText)
{
    menuTexts[targetBlock] = menuText;
}

public string GetAndClearMenuText(Fungus.Block block)
{
    if (block != null && menuTexts.ContainsKey(block))
    {
        string text = menuTexts[block];
        menuTexts.Remove(block); // Elimina después de recuperar
        return text;
    }
    return null;
}
```

**Flujo**:
1. Menu.cs registra: `RegisterMenuText(Baron_whyus, "¿Por qué nosotros?")`
2. Jugador hace clic → se ejecuta bloque Baron_whyus
3. LogSelectedMenu recupera: `GetAndClearMenuText(Baron_whyus)` → "¿Por qué nosotros?"

---

#### 8. `LogSelectedMenu.cs`
**Ubicación**: `Assets/Scripts/DiarioAventuras/LogSelectedMenu.cs`

**Función**: Comando Fungus que guarda la opción de menú seleccionada en el diario.

**Uso en Flowchart**: Colocar como **primer comando** en cada bloque destino de un Menu.

**Código completo**:
```csharp
[CommandInfo("Narrative", "Log Selected Menu", "Guarda en el diario la opción de menú que el jugador acaba de seleccionar")]
public class LogSelectedMenu : Command
{
    public override void OnEnter()
    {
        Block currentBlock = ParentBlock;
        string menuText = MenuJournalTracker.Instance.GetAndClearMenuText(currentBlock);

        if (!string.IsNullOrEmpty(menuText) && AdventureJournal.Instance != null)
        {
            AdventureJournal.Instance.AddEntry("Jugador", $"→ {menuText}");
        }

        Continue();
    }
}
```

**Por qué como primer comando**:
- Menu.cs ya registró el texto cuando mostró las opciones
- Cuando el jugador hace clic, el bloque destino se ejecuta
- LogSelectedMenu recupera el texto inmediatamente antes de que se ejecuten otros comandos

---

## 🔄 Flujo Completo del Sistema

### **Ejemplo: Conversación con Barón**

#### **Paso 1: Mostrar menú inicial**
```
Block: "Baron_Start"
Commands:
  - Say With Journal: "<#BARON_GREETING>" (Speaker: Baron)
  - Menu: "<#BARON_OPTION1>" → Target: "Baron_whyus"
  - Menu: "<#BARON_OPTION2>" → Target: "Baron_stakes"
  - Menu: "<#BARON_OPTION3>" → Target: "Baron_reward"
```

**Lo que pasa internamente**:
1. Say With Journal traduce "BARON_GREETING" → "Hola, héroes..."
2. Muestra el diálogo
3. Guarda en diario: `[Baron] Hola, héroes...`
4. Menu traduce cada opción:
   - "BARON_OPTION1" → "¿Por qué nosotros?"
   - "BARON_OPTION2" → "¿Qué está en juego?"
   - "BARON_OPTION3" → "Hablemos de recompensa"
5. Menu.cs registra cada texto en MenuJournalTracker:
   ```
   RegisterMenuText(Baron_whyus, "¿Por qué nosotros?")
   RegisterMenuText(Baron_stakes, "¿Qué está en juego?")
   RegisterMenuText(Baron_reward, "Hablemos de recompensa")
   ```
6. Se muestran los 3 botones al jugador

---

#### **Paso 2: Jugador selecciona "¿Por qué nosotros?"**
```
Block: "Baron_whyus"
Commands:
  - Log Selected Menu  ← CRÍTICO: debe ser el primer comando
  - Say With Journal: "<#BARON_ANSWER1>" (Speaker: Baron)
  - Say With Journal: "<#BARON_ANSWER2>" (Speaker: Baron)
  - Call: "Baron_Start" (para volver al menú)
```

**Lo que pasa internamente**:
1. MenuDialog ejecuta el bloque "Baron_whyus"
2. **Log Selected Menu** se ejecuta primero:
   - Recupera: `GetAndClearMenuText(Baron_whyus)` → "¿Por qué nosotros?"
   - Guarda en diario: `[Jugador] → ¿Por qué nosotros?`
3. Say With Journal traduce y guarda: `[Baron] Mis capitanes ven enemigos...`
4. Say With Journal traduce y guarda: `[Baron] Vosotros estáis fuera de la disputa...`

---

#### **Paso 3: Jugador abre el diario (tecla J o botón UI)**
```csharp
JournalUI.OpenJournal() → Refresh()
```

**Resultado visualizado**:
```
Baron
Hola, héroes...

Jugador
→ ¿Por qué nosotros?

Baron
Mis capitanes ven enemigos en cada sombra; mis clérigos ven culpa en cada aliento.

Baron
Vosotros estáis fuera de la disputa: paladín, bruja, barbero-cirujano, arquero… neutrales ante mis casas y el reino vecino.
```

---

## ⚙️ Configuración Paso a Paso

### **1. Archivos JSON de Localización**

Crear archivos en: `Assets/Resources/Localization/`

**es.json**:
```json
{
  "BARON_GREETING": "Saludos, héroes.",
  "BARON_OPTION1": "¿Por qué nosotros?",
  "BARON_OPTION2": "¿Qué está en juego?",
  "BARON_ANSWER1": "Mis capitanes ven enemigos en cada sombra..."
}
```

**en.json**:
```json
{
  "BARON_GREETING": "Greetings, heroes.",
  "BARON_OPTION1": "Why us?",
  "BARON_OPTION2": "What's at stake?",
  "BARON_ANSWER1": "My captains see enemies in every shadow..."
}
```

---

### **2. Configurar LocalizationManager**

Crear GameObject en la escena inicial:
1. Crear Empty GameObject: "LocalizationManager"
2. Añadir script LocalizationManager.cs
3. Configurar idioma por defecto en el Inspector

**O** llamar desde código:
```csharp
LocalizationManager.Instance.ChangeLanguage("es");
```

---

### **3. Configurar Diario en Unity**

#### **AdventureJournal**:
1. Crear Empty GameObject: "AdventureJournal"
2. Añadir script AdventureJournal.cs
3. Marcar como DontDestroyOnLoad

#### **JournalUI**:
1. Crear Canvas → Panel (para el fondo)
2. Añadir TextMeshProUGUI para el contenido
3. Añadir script JournalUI.cs al Panel
4. Asignar el TextMeshProUGUI al campo `journalText`
5. Desactivar el Panel por defecto (se activa con OpenJournal)

#### **Botón para abrir diario**:
```csharp
public class InputHandler : MonoBehaviour
{
    public JournalUI journalUI;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            journalUI.OpenJournal();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            journalUI.CloseJournal();
        }
    }
}
```

---

### **4. Usar en Flowcharts de Fungus**

#### **Para diálogos**:
- Usar comando: **Say With Journal** (en lugar de Say)
- Text: `<#CLAVE_DIALOGO>`
- Character: Seleccionar personaje (Baron, etc.)

#### **Para menús con diario**:
1. Usar comando normal: **Menu**
   - Text: `<#CLAVE_OPCION>`
   - Target Block: Seleccionar bloque destino
2. En el bloque destino, **primer comando**: **Log Selected Menu**
3. Luego los Say With Journal de respuesta

**Ejemplo visual**:
```
Block: Baron_Conversation
┌─────────────────────────────────────┐
│ Say With Journal                    │
│   Character: Baron                  │
│   Text: <#BARON_GREETING>          │
├─────────────────────────────────────┤
│ Menu                                │
│   Text: <#BARON_OPTION1>           │
│   Target: Baron_Option1_Response   │
├─────────────────────────────────────┤
│ Menu                                │
│   Text: <#BARON_OPTION2>           │
│   Target: Baron_Option2_Response   │
└─────────────────────────────────────┘

Block: Baron_Option1_Response
┌─────────────────────────────────────┐
│ Log Selected Menu                   │ ← ¡Primer comando!
├─────────────────────────────────────┤
│ Say With Journal                    │
│   Character: Baron                  │
│   Text: <#BARON_ANSWER1>           │
├─────────────────────────────────────┤
│ Call                                │
│   Target: Baron_Conversation        │
└─────────────────────────────────────┘
```

---

## 🐛 Troubleshooting

### **Problema: "Clave no encontrada"**
**Síntoma**: Log warning `[LocalizationManager] Clave 'MIKEY' no encontrada`

**Soluciones**:
1. Verificar que la clave existe en el JSON
2. Recordar que es case-insensitive: `MIKEY` = `mikey` = `MiKey`
3. Asegurarse de que el archivo JSON está en `Resources/Localization/`

---

### **Problema: "Menú no se guarda en diario"**
**Síntoma**: La opción del jugador no aparece en el diario

**Soluciones**:
1. ✅ Verificar que **Log Selected Menu** es el primer comando del bloque destino
2. ✅ Asegurarse de que el bloque destino se ejecuta (añadir un debug Say temporal)
3. ✅ Verificar logs en consola Unity para ver si se registró y recuperó el texto

---

### **Problema: "Solo se guarda el primer Say"**
**Síntoma**: Solo aparece una entrada de Baron en el diario

**Causa**: Menu.cs tenía un `Continue()` que hacía que el flujo continuara sin esperar

**Solución**: Ya corregido en la versión actual de Menu.cs

---

### **Problema: "MenuJournalTracker no se encuentra"**
**Síntoma**: Warning sobre reflexión fallando

**Solución**: MenuJournalTracker se crea automáticamente la primera vez que se accede a `.Instance`. No requiere GameObject en la escena.

---

## 📊 Diagrama de Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│                     FUNGUS FLOWCHART                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Block: Conversation                                        │
│  ┌────────────────────────────────┐                        │
│  │ Say With Journal               │                        │
│  │   ↓ extends Say.cs             │                        │
│  │   ↓ usa translatedText         │                        │
│  │   ↓ guarda en AdventureJournal │                        │
│  └────────────────────────────────┘                        │
│  ┌────────────────────────────────┐                        │
│  │ Menu (modificado)              │                        │
│  │   ↓ traduce <#KEY>             │                        │
│  │   ↓ registra en                │                        │
│  │     MenuJournalTracker         │                        │
│  └────────────────────────────────┘                        │
│                   │                                         │
│                   └─────→ Target: Response_Block           │
│                                                             │
│  Block: Response_Block                                     │
│  ┌────────────────────────────────┐                        │
│  │ Log Selected Menu              │                        │
│  │   ↓ recupera texto de          │                        │
│  │     MenuJournalTracker         │                        │
│  │   ↓ guarda en AdventureJournal │                        │
│  └────────────────────────────────┘                        │
│  ┌────────────────────────────────┐                        │
│  │ Say With Journal               │                        │
│  └────────────────────────────────┘                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                          │
                          ↓
        ┌─────────────────────────────────────┐
        │   LocalizationManager (Singleton)   │
        │                                     │
        │  • Carga es.json / en.json / ca.json│
        │  • GetText(key) case-insensitive    │
        │  • ToUpperInvariant() para keys     │
        └─────────────────────────────────────┘
                          │
                          ↓
        ┌─────────────────────────────────────┐
        │  MenuJournalTracker (Singleton)     │
        │                                     │
        │  • Dictionary<Block, string>        │
        │  • RegisterMenuText()               │
        │  • GetAndClearMenuText()            │
        └─────────────────────────────────────┘
                          │
                          ↓
        ┌─────────────────────────────────────┐
        │   AdventureJournal (Singleton)      │
        │                                     │
        │  • List<JournalEntry> entries       │
        │  • AddEntry(speaker, text)          │
        └─────────────────────────────────────┘
                          │
                          ↓
        ┌─────────────────────────────────────┐
        │         JournalUI                   │
        │                                     │
        │  • TextMeshProUGUI journalText      │
        │  • Refresh() / OpenJournal()        │
        │  • Formatea con <b>speaker</b>      │
        └─────────────────────────────────────┘
```

---

## 🎯 Ventajas del Sistema

### **1. Totalmente Automático**
- No necesitas configurar nada por cada opción de menú
- Menu.cs registra automáticamente todas las opciones
- Solo añades LogSelectedMenu una vez al inicio de cada bloque de respuesta

### **2. Case-Insensitive**
- `<#BARON_GREETING>` = `<#baron_greeting>` = `<#Baron_Greeting>`
- Menos errores de tipeo

### **3. Compatible con Fungus Vanilla**
- Say.cs y Menu.cs solo extienden funcionalidad
- No rompe comportamientos existentes
- Comandos antiguos siguen funcionando

### **4. Fácil de Localizar**
- Todos los textos en archivos JSON centralizados
- Cambiar idioma es solo `ChangeLanguage("en")`
- Fácil para traductores (solo editan JSON)

### **5. Diario Persistente**
- AdventureJournal persiste con DontDestroyOnLoad
- Se puede guardar/cargar desde SaveGame (añadir serialización)

---

## 🚀 Mejoras Futuras

### **1. Serialización del Diario**
```csharp
[System.Serializable]
public class JournalSaveData
{
    public List<JournalEntry> entries;
}

public void SaveJournal()
{
    var data = new JournalSaveData { entries = entries };
    string json = JsonUtility.ToJson(data);
    PlayerPrefs.SetString("Journal", json);
}

public void LoadJournal()
{
    string json = PlayerPrefs.GetString("Journal");
    var data = JsonUtility.FromJson<JournalSaveData>(json);
    entries = data.entries;
}
```

### **2. Organización por Quests**
```csharp
public class JournalEntry
{
    public string speaker;
    public string text;
    public string questName; // Añadir campo
    public string sceneName;
}
```

### **3. Búsqueda en Diario**
```csharp
public List<JournalEntry> SearchEntries(string keyword)
{
    return entries.Where(e => 
        e.text.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
        e.speaker.Contains(keyword, StringComparison.OrdinalIgnoreCase)
    ).ToList();
}
```

### **4. Paginación en UI**
```csharp
public int entriesPerPage = 10;
public int currentPage = 0;

public void ShowPage(int page)
{
    var pageEntries = entries
        .Skip(page * entriesPerPage)
        .Take(entriesPerPage);
    // Mostrar solo esas entradas
}
```

---

## 📝 Checklist de Implementación

**Para un nuevo Flowchart con diario**:

- [ ] Crear bloques de conversación en Fungus
- [ ] Usar `<#CLAVES>` en lugar de texto hardcodeado
- [ ] Añadir claves al archivo JSON de localización
- [ ] Reemplazar Say con **Say With Journal**
- [ ] Añadir **Log Selected Menu** como primer comando en bloques destino de Menu
- [ ] Asignar Character al Say para nombre de speaker
- [ ] Probar conversación completa
- [ ] Abrir diario y verificar que todo se guardó correctamente

---

## 🔧 Comandos Fungus Disponibles

| Comando | Ubicación en Menú | Función |
|---------|------------------|---------|
| Say With Journal | Narrative > Say With Journal | Say + guardar en diario |
| Log Selected Menu | Narrative > Log Selected Menu | Guardar opción de menú seleccionada |
| Menu | Narrative > Menu | Menu normal (ya traduce automáticamente) |

---

## 📞 Soporte y Debugging

### **Activar Logs Detallados**

Si necesitas debuggear, puedes re-añadir logs en:

**Menu.cs**:
```csharp
UnityEngine.Debug.Log($"[Menu] Registrado: {displayText} → {targetBlock.BlockName}");
```

**LogSelectedMenu.cs**:
```csharp
Debug.Log($"[LogSelectedMenu] Texto recuperado: {menuText}");
```

**MenuJournalTracker.cs**:
```csharp
Debug.Log($"[MenuJournalTracker] Registrando: {targetBlock.BlockName} = {menuText}");
Debug.Log($"[MenuJournalTracker] Recuperando: {block.BlockName} = {text}");
```

---

## ✅ Resumen Final

Este sistema proporciona:
- **Localización automática** para Say y Menu usando `<#CLAVES>`
- **Diario de aventuras** con formato bonito
- **Registro automático** de opciones de menú sin configuración manual
- **Sistema de cámaras Cinemachine** integrado con Fungus
- **Sistema modular** que extiende Fungus sin romper compatibilidad

**Archivos creados**: 11 scripts
**Archivos modificados**: 2 scripts de Fungus (Say.cs, Menu.cs)
**Archivos eliminados**: RegisterMenuChoice.cs (obsoleto)

---

## 🎥 Sistema de Cámaras Cinemachine

### Comandos Fungus Personalizados

#### **Activate Dialogue Camera**
**Ubicación**: `Assets/Scripts/FungusCommands/ActivateDialogueCamera.cs`

**Propósito**: Cambia a una cámara Cinemachine específica durante un diálogo.

**Uso en Fungus**:
1. En Flowchart, añade el comando: **Camera → Activate Dialogue Camera**
2. Arrastra la CinemachineCamera del NPC al campo "Target Camera"
3. El comando cambia automáticamente la prioridad de la cámara a 20

**Ejemplo**:
```
[Bloque: Baron_Start]
→ Activate Dialogue Camera (Target: BaronCamera)
→ Say With Journal: <#BARON_GREETING>
→ Menu: <#PLAYER_RESPONSE>
```

---

#### **Deactivate Dialogue Camera**
**Ubicación**: `Assets/Scripts/FungusCommands/DeactivateDialogueCamera.cs`

**Propósito**: Vuelve a la cámara principal al finalizar el diálogo.

**Uso en Fungus**:
1. Al **final del último bloque** de diálogo
2. Añade: **Camera → Deactivate Dialogue Camera**
3. Restaura la prioridad de la cámara de diálogo a 10

**Ejemplo**:
```
[Bloque: Baron_End]
→ Say With Journal: <#BARON_FAREWELL>
→ Deactivate Dialogue Camera
→ Stop Flowchart
```

---

### **DialogueCameraManager**
**Ubicación**: `Assets/Scripts/DialogueCameraManager.cs`

**Propósito**: Singleton que gestiona las prioridades de las cámaras Cinemachine.

**Configuración en Unity**:
1. Añadir el script a un GameObject (ej: "Controller")
2. **Dialogue Camera Priority**: 20 (cámaras de diálogo activas)
3. **Default Camera Priority**: 10 (cámaras inactivas)

**Cómo funciona**:
- Cinemachine usa **prioridades** para decidir qué cámara mostrar
- La cámara con mayor Priority se activa automáticamente
- `ActivateCamera()` sube la Priority a 20
- `DeactivateDialogueCamera()` la baja a 10

**Requisitos**:
- Unity Cinemachine 3.x (`com.unity.cinemachine`)
- Main Camera con `CinemachineBrain` component
- CinemachineCamera en cada NPC con Priority inicial = 10

---

### **Flujo Completo de Diálogo con Cámara**

```
[Bloque Inicio - "Talk_Archivero"]
1. Activate Dialogue Camera → ArchiveroCamera
2. Say With Journal: <#ARCHIVERO_INTRO>
3. Menu con opciones...
4. Call → Bloques según elección

[Bloques Intermedios]
- Say With Journal (múltiples diálogos)
- Lógica de juego

[Bloque Final - "Archivero_End"]
1. Say With Journal: <#ARCHIVERO_BYE>
2. Deactivate Dialogue Camera
3. Stop Flowchart
```

---

### **Troubleshooting Cámaras**

**La cámara no cambia:**
- Verifica que Main Camera tenga `CinemachineBrain`
- Confirma que DialogueCameraManager está en la escena
- Comprueba que Target Camera está asignado en el comando Fungus
- Revisa prioridades: Main Camera ≤ 10, diálogo = 20

**Cambio brusco de cámara:**
- En CinemachineCamera → añade `CinemachinePositionComposer`
- Ajusta "Damping" para transiciones suaves

**No vuelve a Main Camera:**
- Asegúrate de usar `Deactivate Dialogue Camera` al final
- Verifica que Main Camera tiene Priority = 10

---

## 🎨 Personalización de UI de Fungus

### **MenuDialog Personalizado**

**Problema**: El MenuDialog original de Fungus aparecía muy arriba en la pantalla, tapando los rostros de los NPCs durante los diálogos.

**Solución implementada**:

1. **Duplicar el prefab original**:
   - Ubicación original: `Assets/Fungus/Fungus/Prefabs/MenuDialog`
   - Duplicado (opcional): Crear copia para respaldo

2. **Ajustar posición en escena**:
   - Arrastrar el prefab MenuDialog al Canvas de la escena
   - Ajustar posición Y para que no tape los personajes
   - **IMPORTANTE**: Desactivar el GameObject (checkbox en Inspector)
   - Dejar en la escena permanentemente

3. **Cómo funciona**:
   - Fungus busca automáticamente MenuDialog en la escena
   - Si lo encuentra, usa ese en lugar de instanciar el prefab
   - Al estar desactivado, no se ve hasta que un comando Menu lo activa
   - Todos los comandos Menu del Flowchart usan automáticamente esta versión

**Estructura en Hierarchy**:
```
Canvas
  ├── MenuDialog (inactive) ← Versión personalizada con Y ajustada
  ├── SayDialog (opcional, mismo proceso si es necesario)
  └── (resto de UI)
```

**Ventajas**:
- ✅ No modifica los assets originales de Fungus
- ✅ No requiere cambiar comandos existentes
- ✅ Fácil de ajustar en tiempo real
- ✅ Se mantiene tras actualizaciones de Fungus

**Nota**: El mismo proceso se puede aplicar a `SayDialog` si es necesario ajustar su posición.

---

¡Todo listo para producción! 🎉
