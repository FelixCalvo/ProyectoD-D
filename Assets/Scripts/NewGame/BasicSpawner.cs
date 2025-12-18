using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Jugadores Pre-colocados en Escena")]
    [Tooltip("Asigna los 4 GameObjects de jugadores en la escena (desactivados por defecto)")]
    [SerializeField] private GameObject[] preplacedPlayers = new GameObject[4];
    
    [Header("UI")]
    [SerializeField] private ListaPartidasUI listaPartidasUI;

    private Dictionary<PlayerRef, GameObject> _activePlayerMappings = new Dictionary<PlayerRef, GameObject>();
    //public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    //public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    //public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
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
    
    // CRÍTICO: Buffer para almacenar clicks capturados en Update()
    private bool _hasPendingClick = false;
    private bool _isPendingAttack = false;
    private Vector3 _pendingTargetPosition = Vector3.zero;
    private int _pendingTargetPlayerId = -1;
    private bool _hasPendingInteract = false;

    async void Start()
    {
        // CRÍTICO: Asegurar que el cursor está activo en Build
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Verificar configuración de jugadores pre-colocados
        ValidatePreplacedPlayers();
        
        // Verificar si ya existe un NetworkRunner activo (creado desde NetworkSessionStarter)
        NetworkRunner existingRunner = NetworkSessionStarter.GetRunner();
        
        if (existingRunner != null && existingRunner.IsRunning)
        {
            _runner = existingRunner;
            _runner.AddCallbacks(this);
            
            // Activar jugadores que ya están conectados
            if (_runner.IsServer)
            {
                foreach (var player in _runner.ActivePlayers)
                {
                    if (!_activePlayerMappings.ContainsKey(player))
                    {
                        ActivatePlayer(player);
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
            
            // Activar jugadores que ya están conectados
            if (_runner.IsServer)
            {
                foreach (var player in _runner.ActivePlayers)
                {
                    if (!_activePlayerMappings.ContainsKey(player))
                    {
                        ActivatePlayer(player);
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
    
    /// <summary>
    /// Update se ejecuta CADA FRAME para capturar TODOS los clicks del ratón.
    /// Esto evita perder clicks entre los ticks de Fusion.
    /// </summary>
    void Update()
    {
        // Solo capturar input si hay un runner activo
        if (_runner == null || !_runner.IsRunning)
            return;
        
        // Capturar tecla E para interacción
        if (Input.GetKeyDown(KeyCode.E) && !_hasPendingInteract)
        {
            _hasPendingInteract = true;
            Debug.Log($"[Input] 🔑 TECLA E capturada en Update() (Frame {Time.frameCount})");
        }
        
        // Capturar clicks del ratón (solo si no hay un click pendiente)
        if (Input.GetMouseButtonDown(1) && !_hasPendingClick)
        {
            Debug.Log($"[Input] 🖱️ CLIC DERECHO capturado en Update() (Frame {Time.frameCount})");
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // Intentar detectar jugador enemigo
            if (Physics.Raycast(ray, out RaycastHit hitPlayer, 1000f, LayerMask.GetMask("Player")))
            {
                Player targetPlayer = hitPlayer.collider.GetComponent<Player>();
                if (targetPlayer != null && targetPlayer.Object != null)
                {
                    _hasPendingClick = true;
                    _isPendingAttack = true;
                    _pendingTargetPlayerId = (int)targetPlayer.Object.Id.Raw;
                    Debug.Log($"[Input] ✓ Ataque almacenado → {hitPlayer.collider.name}");
                }
            }
            // Si no hay jugador, intentar detectar suelo
            else if (Physics.Raycast(ray, out RaycastHit hitGround, 1000f, LayerMask.GetMask("Ground")))
            {
                _hasPendingClick = true;
                _isPendingAttack = false;
                _pendingTargetPosition = hitGround.point;
                Debug.Log($"[Input] ✓ Movimiento almacenado → {hitGround.point}");
            }
            else
            {
                Debug.LogError($"[Input] ❌ CLIC NO DETECTÓ NADA - Verifica layers!");
                
                // Diagnóstico: Raycast SIN layer mask
                if (Physics.Raycast(ray, out RaycastHit hitAny, 1000f))
                {
                    Debug.LogError($"[Input] ❌ Terrain en layer INCORRECTO: '{hitAny.collider.name}' (Layer: '{LayerMask.LayerToName(hitAny.collider.gameObject.layer)}')");
                }
            }
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
            PlayerCount = HelperClass.MAX_PLAYERS,
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


    /// <summary>
    /// Activa la cámara Cinemachine para el jugador local
    /// </summary>
    private void ActivatePlayerCamera(GameObject playerGO, PlayerRef player)
    {
        // Verificar si este jugador es LOCAL para este cliente:
        // - En HOST (servidor): el primer jugador (PlayerRef con índice más bajo)
        // - En CLIENTE: el jugador con InputAuthority asignado
        
        bool isLocalPlayer = false;
        
        if (_runner.IsServer)
        {
            // En el HOST, el jugador local es el primero que se conecta (típicamente PlayerRef con PlayerId=0)
            // Pero para ser más seguro, comparamos con el LocalPlayer del runner
            isLocalPlayer = (player == _runner.LocalPlayer);
        }
        
        if (isLocalPlayer)
        {
            var camera = playerGO.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
            if (camera != null)
            {
                camera.Priority = 10; // Prioridad alta para jugador local
                Debug.Log($"[{playerGO.name}] 📷 Cámara activada para jugador LOCAL (Priority=10, PlayerRef={player.PlayerId})");
                
                // Actualizar HelperClass.ActivePlayer para compatibilidad
                HelperClass.ActivePlayer = playerGO;
            }
        }
        else
        {
            var camera = playerGO.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
            if (camera != null)
            {
                camera.Priority = 0; // Prioridad baja para jugadores remotos
                Debug.Log($"[{playerGO.name}] 📷 Cámara desactivada para jugador REMOTO (Priority=0, PlayerRef={player.PlayerId})");
            }
        }
    }
    
    /// <summary>
    /// Valida que los jugadores pre-colocados estén configurados correctamente
    /// </summary>
    private void ValidatePreplacedPlayers()
    {
        for (int i = 0; i < preplacedPlayers.Length; i++)
        {
            if (preplacedPlayers[i] == null)
            {
                Debug.LogError($"❌ preplacedPlayers[{i}] no está asignado en el Inspector!");
                continue;
            }
            
            NetworkObject netObj = preplacedPlayers[i].GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError($"❌ {preplacedPlayers[i].name} no tiene componente NetworkObject!");
                continue;
            }
            
            // IMPORTANTE: Los GameObjects deben estar ACTIVOS para que Fusion los replique
            if (!preplacedPlayers[i].activeSelf)
            {
                Debug.LogWarning($"⚠️ {preplacedPlayers[i].name} estaba inactivo, activándolo para replicación");
                preplacedPlayers[i].SetActive(true);
            }
            
            // Marcar como desconectado inicialmente (si tiene Player script)
            Player playerScript = preplacedPlayers[i].GetComponent<Player>();
            if (playerScript != null)
            {
                // Nota: IsPlayerConnected se inicializa en false por defecto en NetworkBool
                Debug.Log($"✅ Jugador {i}: {preplacedPlayers[i].name} configurado (IsPlayerConnected = false por defecto)");
            }
            else
            {
                Debug.LogWarning($"⚠️ {preplacedPlayers[i].name} no tiene componente Player!");
            }
        }
    }
    

    
    /// <summary>
    /// Activa un GameObject de jugador pre-colocado cuando un jugador se conecta
    /// </summary>
    private async void ActivatePlayer(PlayerRef player)
    {
        Debug.Log($"🎮 ActivatePlayer iniciado para PlayerRef {player.PlayerId}");
        
        // Esperar a que el userName esté registrado (máximo 3 segundos)
        string userName = null;
        int intentos = 0;
        const int maxIntentos = 30; // 30 intentos x 100ms = 3 segundos
        
        while (intentos < maxIntentos)
        {
            if (PlayerUserNameRegistry.Instance != null)
            {
                userName = PlayerUserNameRegistry.Instance.GetUserName(player);
                if (!string.IsNullOrEmpty(userName))
                {
                    Debug.Log($"✅ UserName encontrado: {userName} (intento {intentos + 1})");
                    break;
                }
            }
            
            intentos++;
            await System.Threading.Tasks.Task.Delay(100);
        }
        
        if (string.IsNullOrEmpty(userName))
        {
            userName = $"Player{player.PlayerId}";
            Debug.LogWarning($"⚠️ No se encontró userName para PlayerRef {player.PlayerId}, usando fallback: {userName}");
        }
        
        // Determinar el índice del jugador (0-3)
        int playerIndex = GetPlayerIndex(player);
        
        if (playerIndex < 0 || playerIndex >= preplacedPlayers.Length)
        {
            Debug.LogError($"❌ PlayerIndex {playerIndex} fuera de rango para {userName}!");
            return;
        }
        
        GameObject playerGO = preplacedPlayers[playerIndex];
        
        if (playerGO == null)
        {
            Debug.LogError($"❌ No hay jugador pre-colocado en índice {playerIndex}");
            return;
        }
        
        // Obtener personaje seleccionado
        string characterName = PlayerCharacterSelection.GetSelection(userName);
        
        Debug.Log($"🎮 Activando {playerGO.name} (índice {playerIndex}) para {userName} (personaje: {characterName})");
        
        // Obtener NetworkObject
        NetworkObject netObj = playerGO.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"❌ {playerGO.name} no tiene componente NetworkObject!");
            return;
        }
        
        // Obtener Player script
        Player playerScript = playerGO.GetComponent<Player>();
        if (playerScript == null)
        {
            Debug.LogError($"❌ {playerGO.name} no tiene componente Player!");
            return;
        }
        
        // Esperar un frame para que todo se inicialice
        await System.Threading.Tasks.Task.Delay(100);
        
        // Asignar InputAuthority (el objeto ya está spawneado por Fusion como Scene Object)
        if (_runner != null && _runner.IsServer && netObj.HasStateAuthority)
        {
            netObj.AssignInputAuthority(player);
            Debug.Log($"✅ InputAuthority asignada a {userName} en {playerGO.name}");
            
            // Marcar como conectado - esto se sincronizará automáticamente a todos los clientes
            playerScript.IsPlayerConnected = true;
            Debug.Log($"✅ IsPlayerConnected = true para {playerGO.name}");
            
            // CRÍTICO: Si es el jugador local del servidor, configurar cámara inmediatamente
            if (player == _runner.LocalPlayer)
            {
                Debug.Log($"🎥 Este es el jugador del servidor, configurando cámara...");
                // Esperar un frame adicional para que InputAuthority se propague
                await System.Threading.Tasks.Task.Delay(100);
                playerScript.ForceUpdateCamera();
            }
        }
        else if (netObj.HasStateAuthority == false)
        {
            Debug.LogWarning($"⚠️ {playerGO.name} no tiene StateAuthority aún, esperando...");
        }
        
        // Registrar el mapeo
        _activePlayerMappings[player] = playerGO;
        
        Debug.Log($"✅ {userName} activado y visible en {playerGO.name}");
    }
    
    /// <summary>
    /// Obtiene el índice del array para un PlayerRef
    /// Por ahora usa la selección de personaje, pero puedes cambiarlo según tu lógica
    /// </summary>
    private int GetPlayerIndex(PlayerRef player)
    {
        string userName = PlayerUserNameRegistry.Instance?.GetUserName(player);
        string characterName = PlayerCharacterSelection.GetSelection(userName);
        
        Debug.Log($"🔍 GetPlayerIndex para PlayerRef {player.PlayerId}: userName={userName}, character={characterName}");
        
        // Buscar jugador pre-colocado que coincida con el personaje
        if (!string.IsNullOrEmpty(characterName))
        {
            for (int i = 0; i < preplacedPlayers.Length; i++)
            {
                if (preplacedPlayers[i] == null) continue;
                
                // Si el jugador ya está activo, saltarlo
                if (_activePlayerMappings.ContainsValue(preplacedPlayers[i]))
                {
                    Debug.Log($"   Slot {i} ({preplacedPlayers[i].name}) ya está ocupado, saltando");
                    continue;
                }
                
                string playerName = preplacedPlayers[i].name;
                
                // Matching simple por nombre
                if (characterName.Contains("Paladin") && playerName.Contains("Paladin"))
                {
                    Debug.Log($"✅ Match encontrado: {characterName} → slot {i} ({playerName})");
                    return i;
                }
                if (characterName.Contains("Bruja") && playerName.Contains("Bruja"))
                {
                    Debug.Log($"✅ Match encontrado: {characterName} → slot {i} ({playerName})");
                    return i;
                }
                if (characterName.Contains("Arquera") && playerName.Contains("Arquera"))
                {
                    Debug.Log($"✅ Match encontrado: {characterName} → slot {i} ({playerName})");
                    return i;
                }
                if (characterName.Contains("Cirujano") && playerName.Contains("Cirujano"))
                {
                    Debug.Log($"✅ Match encontrado: {characterName} → slot {i} ({playerName})");
                    return i;
                }
            }
        }
        
        // Fallback: usar primer slot disponible
        Debug.LogWarning($"⚠️ No se encontró match para '{characterName}', buscando primer slot disponible...");
        for (int i = 0; i < preplacedPlayers.Length; i++)
        {
            if (preplacedPlayers[i] == null)
            {
                Debug.LogWarning($"   Slot {i} es null, saltando");
                continue;
            }
            
            if (!_activePlayerMappings.ContainsValue(preplacedPlayers[i]))
            {
                Debug.LogWarning($"✅ Usando slot {i} ({preplacedPlayers[i].name}) como fallback");
                return i;
            }
            else
            {
                Debug.LogWarning($"   Slot {i} ({preplacedPlayers[i].name}) ya está ocupado");
            }
        }
        
        Debug.LogError($"❌ No hay slots disponibles! Total slots: {preplacedPlayers.Length}, Activos: {_activePlayerMappings.Count}");
        return -1;
    }
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"🎮 OnPlayerJoined llamado: PlayerRef={player.PlayerId}, IsServer={runner.IsServer}");
        
        if (runner.IsServer)
        {
            Debug.Log($"🎮 Server activando jugador {player.PlayerId}...");
            ActivatePlayer(player);
        }
        else
        {
            Debug.Log($"🎮 Cliente detectó nuevo jugador {player.PlayerId}, el servidor lo activará");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_activePlayerMappings.TryGetValue(player, out GameObject playerGO))
        {
            Debug.Log($"🔴 {playerGO.name} desconectado");
            
            // Obtener Player script
            Player playerScript = playerGO.GetComponent<Player>();
            if (playerScript != null && runner.IsServer)
            {
                // Marcar como desconectado - esto se sincronizará automáticamente a todos los clientes
                playerScript.IsPlayerConnected = false;
                Debug.Log($"✅ IsPlayerConnected = false para {playerGO.name}");
            }
            
            // Obtener NetworkObject
            NetworkObject netObj = playerGO.GetComponent<NetworkObject>();
            if (netObj != null && runner.IsServer)
            {
                // Remover InputAuthority
                netObj.RemoveInputAuthority();
                Debug.Log($"✅ InputAuthority removida de {playerGO.name}");
            }
            
            // Remover del mapeo
            _activePlayerMappings.Remove(player);
            
            Debug.Log($"✅ {playerGO.name} desconectado correctamente");
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // Procesar tecla E
        if (_hasPendingInteract)
        {
            data.interactCommand = true;
            Debug.Log($"[Input] ✓✓ Enviando INTERACCIÓN (E)");
            _hasPendingInteract = false;
        }

        // CRÍTICO: Procesar clicks almacenados en Update()
        // Esto garantiza que NO se pierdan clicks entre ticks de Fusion
        if (_hasPendingClick)
        {
            Debug.Log($"[Input] ⚡ Procesando click almacenado en OnInput() (Frame {Time.frameCount})");
            
            if (_isPendingAttack)
            {
                data.attackCommand = true;
                data.targetPlayerId = _pendingTargetPlayerId;
                Debug.Log($"[Input] ✓✓ Enviando ATAQUE a jugador ID: {_pendingTargetPlayerId}");
            }
            else
            {
                data.moveCommand = true;
                data.targetPosition = _pendingTargetPosition;
                Debug.Log($"[Input] ✓✓ Enviando MOVIMIENTO a {_pendingTargetPosition}");
            }
            
            // Limpiar buffer
            _hasPendingClick = false;
            _isPendingAttack = false;
            _pendingTargetPosition = Vector3.zero;
            _pendingTargetPlayerId = -1;
        }

        input.Set(data);
    }

}