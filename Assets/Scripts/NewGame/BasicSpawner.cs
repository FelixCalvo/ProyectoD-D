using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network")]
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    [Header("UI")]
    [SerializeField] private ListaPartidasUI listaPartidasUI;

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    //public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    //public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    //public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    //public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }


    private NetworkRunner _runner;
    private List<SessionInfo> _sessions = new List<SessionInfo>();

    void Start()
    {
        // Verificar si ya existe un NetworkRunner activo en cualquier escena
        NetworkRunner existingRunner = FindFirstObjectByType<NetworkRunner>();
        if (existingRunner != null && existingRunner.IsRunning)
        {
            Debug.LogWarning("Ya existe un NetworkRunner activo, no se creará otro");
            _runner = existingRunner;
            return;
        }

        string tipoPartida = PlayerPrefs.GetString("TipoPartida", "");
        string nombrePartida = PlayerPrefs.GetString("NombrePartida", "");

        Debug.Log($"BasicSpawner.Start() - Tipo: {tipoPartida}, Nombre: {nombrePartida}");

        if (tipoPartida == "Host")
        {
            StartGame(GameMode.Host, nombrePartida);
        }
        else if (tipoPartida == "Client" && !string.IsNullOrEmpty(nombrePartida))
        {
            // Unirse a una partida específica
            StartGame(GameMode.Client, nombrePartida);
        }
        else
        {
            // Si no hay datos, buscar partidas disponibles
            Debug.LogWarning("No se encontró información de partida, buscando partidas disponibles...");
            RefreshSessionList();
        }
    }

    async void StartGame(GameMode mode, string sessionName = null)
    {
        if (_runner != null)
        {
            Debug.LogWarning("Ya existe un NetworkRunner activo");
            return;
        }

        Debug.Log($"Iniciando juego... Modo: {mode}, Sesión: {sessionName}");

        // Configurar región EU forzada en PhotonAppSettings ANTES de crear el runner
        if (Fusion.Photon.Realtime.PhotonAppSettings.TryGetGlobal(out var photonSettings))
        {
            photonSettings.AppSettings.FixedRegion = "eu";
            Debug.Log("Configurada región fija: eu");
        }

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Conectar al lobby (ya configurado en región EU)
        var lobbyResult = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        
        if (!lobbyResult.Ok)
        {
            Debug.LogError($"Error al conectar al lobby EU: {lobbyResult.ErrorMessage}");
            Destroy(_runner);
            _runner = null;
            return;
        }

        Debug.Log("Conectado al lobby EU, esperando sincronización...");
        
        // Pequeño delay para asegurar que el lobby está completamente sincronizado
        await System.Threading.Tasks.Task.Delay(500);
        
        Debug.Log($"Iniciando partida '{sessionName}' en modo {mode}...");

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = sceneManager,
            PlayerCount = 4,  // Máximo de jugadores (cambia este valor según necesites: 2, 4, 6, 8, etc.)
            IsVisible = true,
            IsOpen = true,
            SessionProperties = new Dictionary<string, SessionProperty>()
            {
                // Propiedades personalizadas de la sesión (opcional)
                // Ejemplos: ["Map"] = "Dungeon1", ["Difficulty"] = "Normal", etc.
            }
        });

        if (!result.Ok)
        {
            string currentRegion = _runner != null && _runner.SessionInfo != null ? _runner.SessionInfo.Region : "desconocida";
            Debug.LogError($"Error al iniciar juego en modo {mode}, región {currentRegion}: {result.ErrorMessage}");
            Debug.LogError($"Sesión buscada: '{sessionName}' - ErrorCode: {result.ShutdownReason}");
            
            if (_runner != null)
            {
                await _runner.Shutdown();
                Destroy(_runner);
                _runner = null;
            }
        }
        else
        {
            string region = _runner.SessionInfo.Region;
            Debug.Log($"✓ Juego iniciado correctamente. Modo: {mode}, Sesión: {sessionName}, Región: {region}");

            // Limpiar PlayerPrefs después de usarlos para evitar reconexiones
            PlayerPrefs.DeleteKey("TipoPartida");
            PlayerPrefs.DeleteKey("NombrePartida");
            PlayerPrefs.Save();
        }
    }

    async void RefreshSessionList()
    {
        if (_runner != null)
        {
            Debug.LogWarning("Ya hay una sesión activa, no se puede buscar partidas");
            return;
        }

        Debug.Log("Creando runner temporal para buscar sesiones...");

        var tempRunner = gameObject.AddComponent<NetworkRunner>();
        tempRunner.name = "SessionListRunner";
        tempRunner.AddCallbacks(this);

        //forzamos a la eu European Union region
        var result = await tempRunner.JoinSessionLobby(SessionLobby.ClientServer, "eu");

        if (!result.Ok)
        {
            Debug.LogError($"Error al buscar sesiones: {result.ErrorMessage}");
            Destroy(tempRunner);
        }
        else
        {
            Debug.Log("✓ Conectado al lobby, esperando lista de sesiones...");

            // Destruir el runner temporal después de un tiempo para que actualice la lista
            await System.Threading.Tasks.Task.Delay(2000);
            if (tempRunner != null && _runner == null)
            {
                Destroy(tempRunner);
                Debug.Log("Runner temporal destruido");
            }
        }
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _sessions = sessionList;
        Debug.Log($"Lista de sesiones actualizada: {sessionList.Count} partidas encontradas");

        // Actualizar la UI con la lista de partidas
        if (listaPartidasUI != null)
        {
            listaPartidasUI.ActualizarLista(sessionList, OnUnirseAPartida);
        }
    }

    // Callback cuando el usuario hace clic en "Unirse" a una partida
    private void OnUnirseAPartida(string nombrePartida)
    {
        Debug.Log($"Intentando unirse a la partida: {nombrePartida}");
        StartGame(GameMode.Client, nombrePartida);
    }

    // Método público para refrescar manualmente la lista (puede ser llamado por un botón)
    public void RefrescarListaPartidas()
    {
        RefreshSessionList();
    }


    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Create a unique position for the player
            Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 10, 1, 20);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (Input.GetKey(KeyCode.W))
            data.direction += Vector3.forward;

        if (Input.GetKey(KeyCode.S))
            data.direction += Vector3.back;

        if (Input.GetKey(KeyCode.A))
            data.direction += Vector3.left;

        if (Input.GetKey(KeyCode.D))
            data.direction += Vector3.right;

        input.Set(data);
    }

}