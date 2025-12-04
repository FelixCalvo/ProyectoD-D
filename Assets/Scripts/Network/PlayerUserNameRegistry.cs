using Fusion;
using System.Collections.Generic;

/// <summary>
/// Almacena el mapeo entre PlayerRef y nombres de usuario en red
/// </summary>
public class PlayerUserNameRegistry : NetworkBehaviour
{
    // Diccionario: PlayerID (int) -> nombreUsuario
    [Networked, Capacity(4)]
    private NetworkDictionary<int, NetworkString<_32>> playerNames => default;
    
    private static PlayerUserNameRegistry _instance;
    
    public static PlayerUserNameRegistry Instance => _instance;
    
    public override void Spawned()
    {
        base.Spawned();
        
        // Hacer que persista entre escenas
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            // Ya existe una instancia, destruir esta
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Registra el nombre de usuario para un PlayerRef (solo Host)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RegisterPlayer(int playerId, NetworkString<_32> userName)
    {
        if (!playerNames.ContainsKey(playerId))
        {
            playerNames.Add(playerId, userName);
        }
        else
        {
            playerNames.Remove(playerId);
            playerNames.Add(playerId, userName);
        }
        UnityEngine.Debug.Log($"📝 Player {playerId}: {userName.Value}");
    }
    
    /// <summary>
    /// Obtiene el nombre de usuario de un PlayerRef
    /// </summary>
    public string GetUserName(PlayerRef player)
    {
        if (playerNames.TryGet(player.PlayerId, out NetworkString<_32> userName))
        {
            return userName.Value;
        }
        return null;
    }
}
