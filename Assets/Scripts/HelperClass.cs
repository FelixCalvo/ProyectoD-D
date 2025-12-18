using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Clase centralizada para configuración global del juego.
/// Aquí se definen valores que usan múltiples scripts para mantener consistencia.
/// </summary>
public static class HelperClass
{
    /// <summary>
    /// Número máximo de jugadores permitidos en una partida multijugador.
    /// </summary>
    public const int MAX_PLAYERS = 2;
    
    /// <summary>
    /// Número mínimo de jugadores requeridos para iniciar una partida.
    /// </summary>
    public const int MIN_PLAYERS_TO_START = 2;
    
    /// <summary>
    /// Player actualmente activo/seleccionado en el juego.
    /// Accesible desde cualquier script, incluyendo Fungus.
    /// </summary>
    private static GameObject _activePlayer;
    
    /// <summary>
    /// Lista de todos los players seleccionados actualmente (para selección múltiple).
    /// </summary>
    private static List<GameObject> _selectedPlayers = new List<GameObject>();
    
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
    
    /// <summary>
    /// Obtiene la lista de players seleccionados (solo lectura).
    /// </summary>
    public static List<GameObject> SelectedPlayers
    {
        get { return new List<GameObject>(_selectedPlayers); }
    }
    
    /// <summary>
    /// Establece la lista de players seleccionados.
    /// </summary>
    public static void SetSelectedPlayers(List<GameObject> players)
    {
        _selectedPlayers = new List<GameObject>(players);
        Debug.Log($"[HelperClass] {_selectedPlayers.Count} players seleccionados");
    }
    
    /// <summary>
    /// Obtiene el número de players seleccionados.
    /// </summary>
    public static int SelectedPlayersCount
    {
        get { return _selectedPlayers.Count; }
    }
}

// Fin de HelperClass.cs   
