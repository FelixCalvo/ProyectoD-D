# 🎯 Referencia Rápida - Sistema Fungus

> Para documentación completa ver [FUNGUS_LOCALIZATION_JOURNAL.md](FUNGUS_LOCALIZATION_JOURNAL.md)

## ⚡ Inicio Rápido

### 1. Configurar Traducciones
Crear archivos en `Assets/Resources/Localization/`:

**es.json**:
```json
{
  "SALUDO_BARON": "Saludos, héroes.",
  "OPCION_1": "¿Por qué nosotros?",
  "RESPUESTA_1": "Mis capitanes ven enemigos en cada sombra..."
}
```

### 2. Crear Diálogo en Fungus
```
Block: Conversacion_Baron
┌──────────────────────────────┐
│ Say With Journal             │
│   Character: Baron           │
│   Text: <#SALUDO_BARON>     │
├──────────────────────────────┤
│ Menu                         │
│   Text: <#OPCION_1>         │
│   Target: Respuesta_1       │
└──────────────────────────────┘

Block: Respuesta_1
┌──────────────────────────────┐
│ Log Selected Menu            │ ← ¡IMPORTANTE!
├──────────────────────────────┤
│ Say With Journal             │
│   Character: Baron           │
│   Text: <#RESPUESTA_1>      │
└──────────────────────────────┘
```

### 3. Resultado en Diario
```
Baron
Saludos, héroes.

Jugador
→ ¿Por qué nosotros?

Baron
Mis capitanes ven enemigos en cada sombra...
```

---

## 📝 Comandos Disponibles

### Say With Journal
**Menú**: Narrative > Say With Journal

**Uso**: Reemplaza comando "Say" normal

**Función**: 
- Traduce `<#CLAVES>` automáticamente
- Muestra diálogo en pantalla
- Guarda en diario con nombre del speaker

**Campos**:
- Character: Seleccionar personaje (Baron, etc.)
- Text: `<#CLAVE_TRADUCCION>`

---

### Log Selected Menu
**Menú**: Narrative > Log Selected Menu

**Uso**: Primer comando en bloque destino de Menu

**Función**:
- Recupera el texto de la opción seleccionada
- Guarda en diario con prefijo "→"
- Marca como decisión del jugador

**⚠️ CRÍTICO**: Debe ser el **primer comando** del bloque destino

---

### Menu (sin cambios)
**Menú**: Narrative > Menu

**Uso**: Normal (ya traduce automáticamente)

**Función**:
- Traduce `<#CLAVES>` automáticamente
- Registra texto para LogSelectedMenu
- NO necesita comandos adicionales

**Campos**:
- Text: `<#CLAVE_OPCION>`
- Target Block: Bloque de respuesta

---

## 🔧 Configuración Inicial

### LocalizationManager
```
1. Crear GameObject "LocalizationManager" en escena inicial
2. Añadir script LocalizationManager.cs
3. Configurar idioma por defecto en Inspector
```

### AdventureJournal
```
1. Crear GameObject "AdventureJournal"
2. Añadir script AdventureJournal.cs
3. Marcar DontDestroyOnLoad
```

### JournalUI
```
1. Crear Canvas > Panel (fondo del diario)
2. Añadir TextMeshProUGUI hijo
3. Añadir script JournalUI.cs al Panel
4. Asignar TextMeshProUGUI al campo "journalText"
5. Desactivar Panel (se activa con OpenJournal())
```

---

## 🐛 Problemas Comunes

### "Clave no encontrada"
**Síntoma**: `[LocalizationManager] Clave 'MIKEY' no encontrada`

**Solución**:
1. ✅ Verificar que existe en JSON
2. ✅ Es case-insensitive: `MIKEY` = `mikey`
3. ✅ Archivo en `Resources/Localization/`

---

### "Opción no aparece en diario"
**Síntoma**: Menu se ejecuta pero no se guarda

**Solución**:
1. ✅ Verificar que **Log Selected Menu** es el primer comando
2. ✅ Confirmar que el bloque destino se ejecuta
3. ✅ Revisar logs de Unity Console

---

### "Solo primer Say se guarda"
**Solución**: Ya corregido en versión actual (Continue() en Menu.cs)

---

## 📊 Diagrama de Flujo

```
┌─────────────┐
│   USUARIO   │
│ Crea Dialog │
└──────┬──────┘
       │ usa <#CLAVES>
       ▼
┌──────────────────┐
│ Say With Journal │
│   Menu (normal)  │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ LocalizationMgr  │ → Traduce desde JSON
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Menu.cs registra │ → MenuJournalTracker
└──────┬───────────┘
       │
       ▼ (jugador hace clic)
┌──────────────────┐
│ LogSelectedMenu  │ → Recupera texto
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ AdventureJournal │ → Almacena
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│    JournalUI     │ → Muestra
└──────────────────┘
```

---

## 📁 Estructura de Archivos

### Scripts Propios
```
Assets/Scripts/
├── LocalizationManager.cs
└── DiarioAventuras/
    ├── AdventureJournal.cs
    ├── JournalEntry.cs
    ├── JournalUI.cs
    ├── SayWithJournal.cs
    ├── LogSelectedMenu.cs
    └── MenuJournalTracker.cs
```

### Scripts Modificados (Fungus)
```
Assets/Fungus/Fungus/Scripts/Commands/
├── Say.cs      [+50 líneas - traducción]
└── Menu.cs     [+60 líneas - traducción + registro]
```

### Recursos
```
Assets/Resources/Localization/
├── es.json
├── en.json
└── ca.json
```

---

## 💡 Tips y Buenas Prácticas

### Nomenclatura de Claves
```json
// ✅ BUENO
{
  "BARON_GREETING": "...",
  "BARON_OPTION1": "...",
  "BARON_ANSWER1": "..."
}

// ❌ EVITAR
{
  "texto1": "...",
  "a": "...",
  "respuesta": "..."
}
```

**Convención recomendada**: `{PERSONAJE}_{CONTEXTO}`

### Organización de Flowcharts
```
// Estructura recomendada:
Block: {Personaje}_Intro       → Primera conversación
Block: {Personaje}_Option1     → Respuestas a opciones
Block: {Personaje}_Option2
Block: {Personaje}_Conclusion  → Final de conversación
```

### Testing
1. **Probar sin red primero**: Verificar traducciones y diario
2. **Revisar Console logs**: Confirmar registro/recuperación
3. **Abrir diario**: Verificar formato y contenido
4. **Probar todos los paths**: Cada opción de menú

---

## 🚀 Próximos Pasos

Después de configurar el sistema básico:

1. **Añadir más idiomas**: Crear nuevos archivos JSON
2. **Serializar diario**: Guardar/cargar con PlayerPrefs
3. **Organizar por quests**: Añadir campo questName a JournalEntry
4. **Búsqueda en diario**: Implementar filtro por keyword
5. **Paginación UI**: Mostrar entradas de 10 en 10

---

## 📖 Documentación Completa

Para información detallada, ver:
- [FUNGUS_LOCALIZATION_JOURNAL.md](FUNGUS_LOCALIZATION_JOURNAL.md) - Guía completa (770 líneas)
- [CHANGELOG.md](CHANGELOG.md) - Cambios en v1.3.0
- [INDEX.md](INDEX.md) - Índice general de documentación

---

## ✅ Checklist de Implementación

**Configuración Inicial**:
- [ ] LocalizationManager en escena
- [ ] AdventureJournal en escena
- [ ] JournalUI configurado
- [ ] Archivos JSON creados con traducciones

**Por Cada Flowchart**:
- [ ] Usar `<#CLAVES>` en lugar de texto hardcoded
- [ ] Añadir claves al JSON
- [ ] Reemplazar Say con Say With Journal
- [ ] Añadir Log Selected Menu en bloques destino
- [ ] Asignar Character para speaker name
- [ ] Probar flujo completo
- [ ] Verificar diario

---

**Última actualización**: 14 de Diciembre de 2025
