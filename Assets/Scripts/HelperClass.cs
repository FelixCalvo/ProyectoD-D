/// <summary>
/// Clase centralizada para configuración global del juego.
/// Aquí se definen valores que usan múltiples scripts para mantener consistencia.
/// </summary>
public static class HelperClass
{
    /// <summary>
    /// Número máximo de jugadores permitidos en una partida multijugador.
    /// Cambia este valor según necesites: 2, 3, 4, 6, 8, etc.
    /// </summary>
    public const int MAX_PLAYERS = 4;
    
    /// <summary>
    /// Número mínimo de jugadores requeridos para iniciar una partida.
    /// Útil para pruebas (ej: 2 humanos + bots, o permitir iniciar con menos jugadores).
    /// </summary>
    public const int MIN_PLAYERS_TO_START = 4;
}
