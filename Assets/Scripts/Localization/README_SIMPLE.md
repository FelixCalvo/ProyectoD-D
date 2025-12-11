# 🌍 Sistema Simple de Localización para Fungus

Sistema minimalista de traducción para Fungus con archivos JSON.

## 🚀 Uso (SUPER SIMPLE)

### 1. No necesitas hacer nada especial
El sistema está integrado directamente en Fungus.

### 2. En cualquier comando Say de Fungus escribe:
```
Hola aventurero, <#WELCOME_MESSAGE>
```

**¡YA ESTÁ!** Se traduce automáticamente.

⚠️ **Usa `< >` en lugar de `{ }`** porque Fungus reserva las llaves para sus propios tags.

## 📁 Archivos JSON

En `Assets/Resources/Localization/`:
- `es.json` - Español
- `en.json` - Inglés  
- `ca.json` - Catalán

Formato:
```json
{
  "WELCOME_MESSAGE": "¡Bienvenido!",
  "START_QUEST": "Tu aventura comienza..."
}
```

## 📋 Archivos del Sistema

- `Say.cs` (modificado) - Traducción integrada en Fungus (10 líneas añadidas)
- `LocalizationManager.cs` - Carga JSONs (~130 líneas)
- `LanguageSwitcher.cs` - Botones de idioma (22 líneas)

## 🎮 Cambiar idioma

```csharp
LocalizationManager.Instance.SetLanguage("en");
```

O usa el componente `LanguageSwitcher` con botones.

## ✅ Total: 80 líneas de código
