using UnityEngine;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using System;

public class BuscadorPartidas : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI")]
    [SerializeField] private ListaPartidasUI listaPartidasUI;

    private NetworkRunner _sessionRunner;
    private List<SessionInfo> _sessions = new List<SessionInfo>();
    private bool _buscandoPartidas = false;

    // Callbacks de INetworkRunnerCallbacks (vacíos, solo necesitamos OnSessionListUpdated)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    void OnEnable()
    {
        // Buscar el componente si no está asignado
        if (listaPartidasUI == null)
        {
            listaPartidasUI = FindFirstObjectByType<ListaPartidasUI>();
            if (listaPartidasUI == null)
            {
                Debug.LogError("No se encontró ListaPartidasUI en la escena. Asegúrate de añadir el componente a algún GameObject.");
                return;
            }
        }

        // Cuando se activa el panel, buscar partidas
        BuscarPartidas();
    }

    void OnDisable()
    {
        // Limpiar el runner cuando se desactiva el panel
        if (_sessionRunner != null)
        {
            _sessionRunner.Shutdown();
            Destroy(_sessionRunner.gameObject);
            _sessionRunner = null;
        }
    }

    public async void BuscarPartidas()
    {
        if (_buscandoPartidas)
        {
            Debug.LogWarning("Ya se está buscando partidas...");
            return;
        }

        _buscandoPartidas = true;
        Debug.Log("Buscando partidas disponibles...");

        // Si ya existe un runner, destruirlo primero
        if (_sessionRunner != null)
        {
            await _sessionRunner.Shutdown();
            Destroy(_sessionRunner.gameObject);
            _sessionRunner = null;
        }

        // Pequeña espera para asegurar que se destruyó el anterior
        await System.Threading.Tasks.Task.Delay(100);

        // Configurar región EU forzada en PhotonAppSettings antes de crear el runner
        if (Fusion.Photon.Realtime.PhotonAppSettings.TryGetGlobal(out var photonSettings))
        {
            photonSettings.AppSettings.FixedRegion = "eu";
            Debug.Log("Configurada región fija: eu");
        }

        // Crear un runner temporal solo para buscar sesiones
        var runnerGO = new GameObject("SessionListRunner");
        _sessionRunner = runnerGO.AddComponent<NetworkRunner>();
        _sessionRunner.AddCallbacks(this);

        // Conectar al lobby de la región EU
        var result = await _sessionRunner.JoinSessionLobby(SessionLobby.ClientServer);

        if (!result.Ok)
        {
            Debug.LogError($"Error al buscar sesiones: {result.ErrorMessage}");
            if (listaPartidasUI != null)
            {
                listaPartidasUI.LimpiarLista();
            }
            _buscandoPartidas = false;
        }
        else
        {
            // Obtener región actual
            string region = _sessionRunner.SessionInfo.Region;
            Debug.Log($"Conectado al lobby en región: {region}, esperando lista de sesiones...");
            
            // Esperar más tiempo para asegurar que se recibe la lista completa
            await System.Threading.Tasks.Task.Delay(3000);
            _buscandoPartidas = false;
        }
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _sessions = sessionList;
        string currentRegion = runner.SessionInfo.Region;
        Debug.Log($"[Región: {currentRegion}] Lista actualizada: {sessionList.Count} partidas encontradas");
        
        // Mostrar detalles de cada sesión
        foreach (var session in sessionList)
        {
            Debug.Log($"  - Partida: {session.Name}, Región: {session.Region}, Jugadores: {session.PlayerCount}/{session.MaxPlayers}, IsOpen: {session.IsOpen}, IsVisible: {session.IsVisible}");
        }

        if (listaPartidasUI != null)
        {
            listaPartidasUI.ActualizarLista(sessionList, OnUnirseAPartida);
        }
        else
        {
            Debug.LogError("listaPartidasUI es null!");
        }
    }

    private async void OnUnirseAPartida(string nombrePartida)
    {
        Debug.Log($"Uniéndose a la partida: {nombrePartida}");
        
        // Destruir el runner de búsqueda completamente antes de cambiar de escena
        if (_sessionRunner != null)
        {
            await _sessionRunner.Shutdown();
            Destroy(_sessionRunner.gameObject);
            _sessionRunner = null;
            
            // Pequeña espera para asegurar que se destruyó completamente
            await System.Threading.Tasks.Task.Delay(200);
        }
        
        // Unirse a la partida y luego cargar Players
        await NetworkSessionStarter.UnirseAPartidaYCargarPlayers(nombrePartida);
    }

    // Método público para refrescar desde un botón
    public void RefrescarPartidas()
    {
        BuscarPartidas();
    }
}
