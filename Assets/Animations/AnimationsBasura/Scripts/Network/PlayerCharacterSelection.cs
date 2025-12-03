using System.Collections.Generic;

/// <summary>
/// Almacena las selecciones de personajes de todos los jugadores
/// </summary>
public static class PlayerCharacterSelection
{
    // Diccionario: nombreUsuario -> nombrePersonaje
    private static Dictionary<string, string> _selections = new Dictionary<string, string>();
    
    /// <summary>
    /// Registra la selección de un personaje para un usuario
    /// </summary>
    public static void SetSelection(string userName, string characterName)
    {
        _selections[userName] = characterName;
    }
    
    /// <summary>
    /// Obtiene el personaje seleccionado por un usuario
    /// </summary>
    public static string GetSelection(string userName)
    {
        if (_selections.TryGetValue(userName, out string characterName))
        {
            return characterName;
        }
        return null;
    }
    
    /// <summary>
    /// Obtiene todas las selecciones
    /// </summary>
    public static Dictionary<string, string> GetAllSelections()
    {
        return new Dictionary<string, string>(_selections);
    }
    
    /// <summary>
    /// Limpia todas las selecciones
    /// </summary>
    public static void Clear()
    {
        _selections.Clear();
    }
}
