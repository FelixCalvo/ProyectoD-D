# 🌍 Sistema de Localización Multiidioma para Fungus

Sistema completo de traducción con JSON para integrar con Fungus.

## 📁 Estructura de Archivos

```
Assets/
├── Resources/
│   └── Localization/
│       ├── es.json  (Español)
│       ├── en.json  (English)
│       └── ca.json  (Català)
└── Scripts/
    └── Localization/
        ├── LocalizationManager.cs  (Gestor de idiomas)
        ├── SayLocalized.cs         (Comando Fungus personalizado)
        └── LanguageSwitcher.cs     (UI para cambiar idioma)
```

## 🎮 Cómo Usar

### 1. Añadir Textos al JSON

Edita los archivos en `Assets/Resources/Localization/`:

**es.json:**
```json
{
  "mi_dialogo": "Hola, ¿cómo estás?",
  "despedida": "Hasta luego"
}
```

**en.json:**
```json
{
  "mi_dialogo": "Hello, how are you?",
  "despedida": "See you later"
}
```

### 2. INSTALACIÓN

¡No necesitas instalar nada! El script `SayLocalized.cs` reemplaza automáticamente el comando Say de Fungus.

### 3. Usar en Fungus (¡SUPER FÁCIL!)

Usa el comando **Say normal** de Fungus y simplemente escribe:

```
#mi_dialogo
```

¡Eso es todo! Se traducirá automáticamente según el idioma.

**Ejemplos:**

**Texto completo traducido:**
```
#cocinero_saludo
```
→ "¡Bienvenido a mi cocina!" (en español)
→ "Welcome to my kitchen!" (en inglés)

**Texto mixto (español + traducciones):**
```
Hola, soy #npc_name y trabajo como #npc_profession
```
→ "Hola, soy Juan y trabajo como cocinero"

**Múltiples claves:**
```
#greeting Mi nombre es #character_name. #question
```
→ "Buenos días Mi nombre es María. ¿En qué puedo ayudarte?"

### 4. Cambiar Idioma desde el Menú

#### Opción A: Dropdown
1. Crea un Canvas con un Dropdown (TextMeshPro)
2. Añade el script `LanguageSwitcher` al Canvas
3. Arrastra el Dropdown al campo "Language Dropdown"
4. ¡Listo! El dropdown mostrará Español/English/Català

#### Opción B: Botones
1. Crea 3 botones: "Español", "English", "Català"
2. Añade `LanguageSwitcher` al Canvas
3. Arrastra los botones a los campos correspondientes
4. Los botones cambiarán automáticamente el idioma

#### Opción C: Desde código
```csharp
LocalizationManager.Instance.SetLanguage("es"); // Español
LocalizationManager.Instance.SetLanguage("en"); // English
LocalizationManager.Instance.SetLanguage("ca"); // Català
```

### 5. Usar en Scripts Personalizados

```csharp
// Obtener texto traducido
string texto = LocalizationManager.Instance.GetText("mi_dialogo");
Debug.Log(texto); // "Hola, ¿cómo estás?" en español

// Cambiar idioma
LocalizationManager.Instance.SetLanguage("en");

// Obtener idioma actual
string lang = LocalizationManager.Instance.GetCurrentLanguage();
```

## 🔧 Configuración Avanzada

### Añadir Más Idiomas

1. Crea un nuevo archivo JSON: `Assets/Resources/Localization/fr.json` (francés)
2. Añade el código de idioma a `LanguageSwitcher.cs`:
```csharp
[SerializeField] private string[] availableLanguages = { "es", "en", "ca", "fr" };
[SerializeField] private string[] languageNames = { "Español", "English", "Català", "Français" };
```

### Detección Automática de Idioma

El sistema detecta automáticamente el idioma del sistema operativo al iniciar.
Prioridad:
1. Idioma guardado en PlayerPrefs
2. Idioma del sistema
3. Español (por defecto)

### Guardar Preferencia del Usuario

El idioma seleccionado se guarda automáticamente en `PlayerPrefs.GetString("Language")`.

## 📝 Buenas Prácticas

1. **Usa claves descriptivas:**
   - ✅ `npc_merchant_greeting`
   - ❌ `texto1`

2. **Organiza por contexto:**
   ```json
   {
     "menu_start": "Iniciar",
     "menu_options": "Opciones",
     "npc_guard_hello": "Alto ahí",
     "quest_1_title": "La espada perdida"
   }
   ```

3. **Variables en textos:**
   Si necesitas variables (nombre del jugador, números, etc.), usa placeholders:
   ```json
   {
     "welcome_player": "Bienvenido, {0}"
   }
   ```
   Y en código:
   ```csharp
   string texto = LocalizationManager.Instance.GetText("welcome_player");
   texto = string.Format(texto, playerName);
   ```

## 🐛 Solución de Problemas

**"No se encontró el archivo de idioma"**
- Verifica que el JSON esté en `Assets/Resources/Localization/`
- El nombre debe ser exactamente `es.json`, `en.json`, etc. (minúsculas)

**"Clave de traducción no encontrada"**
- Verifica que la clave existe en el JSON
- Revisa que no haya espacios o mayúsculas incorrectas

**El texto no se actualiza al cambiar idioma**
- Usa el comando `SayLocalized` de Fungus
- O recarga la escena después de cambiar idioma

## 🎯 Ejemplo Completo

**JSON (es.json):**
```json
{
  "cocinero_saludo": "¡Bienvenido a mi cocina!",
  "cocinero_pregunta": "¿Qué te gustaría comer hoy?",
  "opcion_sopa": "Sopa de verduras",
  "opcion_carne": "Filete con patatas"
}
```

**En Fungus (comando Say normal):**
1. Block "Inicio"
   - Say → Story Text: `#cocinero_saludo`
   - Say → Story Text: `#cocinero_pregunta`
   - Menu:
     - Text: `#opcion_sopa` → Block "Sopa"
     - Text: `#opcion_carne` → Block "Carne"

**¡Así de simple!** Todo el diálogo cambiará automáticamente según el idioma.

### Ejemplo Avanzado con Texto Mixto:

**JSON:**
```json
{
  "player_name": "Héroe",
  "npc_merchant": "mercader",
  "greeting": "Hola",
  "question": "¿en qué puedo ayudarte?"
}
```

**En Say:**
```
#greeting #player_name, soy un #npc_merchant. #question
```

**Resultado:**
```
Hola Héroe, soy un mercader. ¿en qué puedo ayudarte?
```

## 📊 Ventajas

✅ Centralizado: todos los textos en un solo lugar
✅ Fácil de traducir: solo edita archivos JSON
✅ Detección automática de idioma del sistema
✅ Cambio de idioma en tiempo real
✅ Integración directa con Fungus
✅ Extensible a más idiomas sin cambiar código

---

**Creado por:** Sistema de Localización para ProyectoD&D
**Versión:** 1.0
