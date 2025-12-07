using Fusion;
using UnityEngine;

/// <summary>
/// Estructura de datos de input sincronizada en red.
/// Contiene movimiento y comandos de combate.
/// </summary>
public struct NetworkInputData : INetworkInput
{
  public Vector3 direction;           // Dirección de movimiento WASD
  public NetworkBool attackCommand;   // Comando de ataque (clic derecho en enemigo)
  public NetworkBool moveCommand;     // Comando de movimiento (clic derecho en suelo)
  public Vector3 targetPosition;      // Posición objetivo para movimiento
  public int targetPlayerId;          // ID del jugador objetivo para ataque (-1 si no hay)
}