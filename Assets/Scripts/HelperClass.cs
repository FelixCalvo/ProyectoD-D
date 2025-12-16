using UnityEngine;

/// <summary>
/// Clase centralizada para configuración global del juego.
/// Aquí se definen valores que usan múltiples scripts para mantener consistencia.
/// </summary>
public static class HelperClass
{
    /// <summary>
    /// Número máximo de jugadores permitidos en una partida multijugador.
    /// </summary>
    public const int MAX_PLAYERS = 4;
    
    /// <summary>
    /// Número mínimo de jugadores requeridos para iniciar una partida.
    /// </summary>
    public const int MIN_PLAYERS_TO_START = 4;
    
    /// <summary>
    /// Player actualmente activo/seleccionado en el juego.
    /// Accesible desde cualquier script, incluyendo Fungus.
    /// </summary>
    private static GameObject _activePlayer;
    
    /// <summary>
    /// Obtiene o establece el player activo.
    /// </summary>
    public static GameObject ActivePlayer
    {
        get { return _activePlayer; }
        set 
        { 
            _activePlayer = value;
            if (value != null)
            {
                Debug.Log($"[HelperClass] Player activo cambiado a: {value.name}");
            }
        }
    }
    
    /// <summary>
    /// Obtiene el nombre del player activo (útil para Fungus).
    /// </summary>
    public static string ActivePlayerName
    {
        get { return _activePlayer != null ? _activePlayer.name : "Ninguno"; }
    }
}

// Fin de HelperClass.cs   
