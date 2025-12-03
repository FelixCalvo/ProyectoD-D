using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network")]
    [SerializeField] private NetworkPrefabRef _playerPrefab; // Prefab genérico (deprecated)
    
    [Header("Prefabs de Personajes")]
    [SerializeField] private NetworkPrefabRef _paladinPrefab;
    [SerializeField] private NetworkPrefabRef _brujaPrefab;
    [SerializeField] private NetworkPrefabRef _arqueraPrefab;
    [SerializeField] private NetworkPrefabRef _cirujanoBarberoPrefab;

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

    async void Start()
    {
        // Verificar si ya existe un NetworkRunner activo (creado desde NetworkSessionStarter)
        NetworkRunner existingRunner = NetworkSessionStarter.GetRunner();
        
        if (existingRunner != null && existingRunner.IsRunning)
        {
            _runner = existingRunner;
            _runner.AddCallbacks(this);
            
            // Spawnear jugadores que ya están conectados
            if (_runner.IsServer)
            {
                foreach (var player in _runner.ActivePlayers)
                {
                    if (!_spawnedCharacters.ContainsKey(player))
                    {
                        await SpawnPlayerAsync(_runner, player);
                    }
                }
            }
            return;
        }
        
        // Si no hay runner activo, buscar uno en la escena
        existingRunner = FindFirstObjectByType<NetworkRunner>();
        if (existingRunner != null && existingRunner.IsRunning)
        {
            _runner = existingRunner;
            _runner.AddCallbacks(this);
            
            // Spawnear jugadores que ya están conectados
            if (_runner.IsServer)
            {
                foreach (var player in _runner.ActivePlayers)
                {
                    if (!_spawnedCharacters.ContainsKey(player))
                    {
                        await SpawnPlayerAsync(_runner, player);
                    }
                }
            }
            return;
        }

        // Fallback: iniciar sesión desde PlayerPrefs (flujo antiguo)
        Debug.LogWarning("⚠ No se encontró sesión de red activa. Verifica el flujo de inicio.");
        
        string tipoPartida = PlayerPrefs.GetString("TipoPartida", "");
        string nombrePartida = PlayerPrefs.GetString("NombrePartida", "");

        if (tipoPartida == "Host")
        {
            StartGame(GameMode.Host, nombrePartida);
        }
        else if (tipoPartida == "Client" && !string.IsNullOrEmpty(nombrePartida))
        {
            StartGame(GameMode.Client, nombrePartida);
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


    private async System.Threading.Tasks.Task SpawnPlayerAsync(NetworkRunner runner, PlayerRef player)
    {
        // Obtener el nombre de usuario del jugador
        string userName = null;
        if (PlayerUserNameRegistry.Instance != null)
        {
            userName = PlayerUserNameRegistry.Instance.GetUserName(player);
        }
        
        // Obtener el personaje seleccionado
        string characterName = null;
        if (!string.IsNullOrEmpty(userName))
        {
            characterName = PlayerCharacterSelection.GetSelection(userName);
        }
        
        // Seleccionar el prefab correcto según el personaje
        NetworkPrefabRef prefabToSpawn = GetPrefabForCharacter(characterName);
        
        if (prefabToSpawn.IsValid == false)
        {
            Debug.LogError($"❌ No hay prefab válido para el personaje '{characterName}' del usuario '{userName}'");
            return;
        }
        
        Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 10, 1, 20);
        NetworkObject networkPlayerObject = await runner.SpawnAsync(prefabToSpawn, spawnPosition, Quaternion.identity, player);
        
        if (networkPlayerObject != null)
        {
            _spawnedCharacters.Add(player, networkPlayerObject);
            Debug.Log($"✅ Jugador {player.PlayerId} ({userName}) spawneado como '{characterName}'");
        }
        else
        {
            Debug.LogError($"❌ Error al spawnear jugador {player.PlayerId}");
        }
    }
    
    private NetworkPrefabRef GetPrefabForCharacter(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogWarning("⚠️ Personaje no seleccionado, usando prefab genérico");
            return _playerPrefab;
        }
        
        // Los nombres deben coincidir con los nombres de los GameObjects en el cilindro
        // Ejemplo: "Player_Paladin (0)", "Player_Bruja (2)", etc.
        if (characterName.Contains("Paladin") || characterName.Contains("Paladín"))
        {
            return _paladinPrefab;
        }
        else if (characterName.Contains("Bruja"))
        {
            return _brujaPrefab;
        }
        else if (characterName.Contains("Arquera"))
        {
            return _arqueraPrefab;
        }
        else if (characterName.Contains("Cirujano") || characterName.Contains("Barbero"))
        {
            return _cirujanoBarberoPrefab;
        }
        
        Debug.LogWarning($"⚠️ Personaje '{characterName}' no reconocido, usando prefab genérico");
        return _playerPrefab;
    }
    
    public async void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            await SpawnPlayerAsync(runner, player);
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