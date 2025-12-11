# ✅ Sistema de Localización SIMPLIFICADO

## 📊 Resumen

**ANTES**: ~500 líneas de código complicado  
**AHORA**: 174 líneas totales

| Archivo | Líneas | Función |
|---------|--------|---------|
| `SayLocalized.cs` | 28 | Extiende Say de Fungus |
| `LocalizationManager.cs` | 109 | Carga JSONs |
| `LanguageSwitcher.cs` | 22 | Botones UI |
| `LocalizationTester.cs` | 15 | Testing |

## 🚀 Cómo Usar

### 1. Setup (una sola vez)
No se necesita nada especial. El sistema usa el comando Say normal de Fungus, pero ahora **reemplazado** por SayLocalized.

### 2. En Fungus, escribe:
```
Hola aventurero, <#WELCOME_MESSAGE>
```

⚠️ **Nota**: Usa `<#CLAVE>` (no `{#CLAVE}`) porque Fungus reserva las llaves.

### 3. Formato de JSON:
```json
{
  "WELCOME_MESSAGE": "¡Bienvenido al juego!",
  "START_QUEST": "Tu aventura comienza..."
}
```

### 4. Cambiar idioma:
```csharp
LocalizationManager.Instance.SetLanguage("en");
```

## 📁 Archivos

```
Assets/
├── Resources/Localization/
│   ├── es.json
│   ├── en.json
│   └── ca.json
│
└── Scripts/Localization/
    ├── SayLocalized.cs           ← Extiende comando Say
    ├── LocalizationManager.cs    ← Carga JSONs
    ├── LanguageSwitcher.cs       ← UI opcional
    └── LocalizationTester.cs     ← Testing opcional
```

## 🎯 Sintaxis

### En Fungus Say:
- `<#CLAVE>` - Se reemplaza por traducción
- Las claves deben ser MAYÚSCULAS con guiones bajos
- Ejemplos: `<#WELCOME_MESSAGE>`, `<#NPC_GREETING>`, `<#QUEST_COMPLETE>`

### Texto mixto:
```
Hola, mi nombre es <#NPC_NAME> y soy un <#NPC_PROFESSION>.
```

## ✅ Ventajas

- ✅ **Mucho más simple**: 174 vs ~500 líneas
- ✅ **Sin componentes extra**: No necesitas añadir nada a la escena
- ✅ **Funciona automáticamente**: Solo extiende el Say existente
- ✅ **Sintaxis clara**: `{#CLAVE}` es fácil de identificar
- ✅ **Parser robusto**: Funciona con JSON multi-línea
- ✅ **Zero configuración**: Detecta idioma del sistema automáticamente

## 🧪 Probar

Añade `LocalizationTester` a un GameObject y presiona **T** en Play mode.
