using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Threading.Tasks;

/// <summary>
/// Crea la sesión de red ANTES de cargar la escena de selección de personajes
/// </summary>
public static class NetworkSessionStarter
{
    private static NetworkRunner _runner;
    
    /// <summary>
    /// Crea una sesión como Host y luego carga la escena Players
    /// </summary>
    public static async Task CrearPartidaYCargarPlayers(string nombrePartida)
    {
        Debug.Log($"📡 Creando partida '{nombrePartida}' como Host...");
        
        // Guardar información para usar después
        PlayerPrefs.SetString("NombrePartida", nombrePartida);
        PlayerPrefs.SetString("TipoPartida", "Host");
        PlayerPrefs.Save();
        
        // CRÍTICO: Limpiar runner existente si hay uno
        if (_runner != null)
        {
            Debug.LogWarning("⚠ Ya existe un NetworkRunner, destruyéndolo antes de crear uno nuevo");
            if (_runner.IsRunning)
            {
                await _runner.Shutdown();
            }
            UnityEngine.Object.Destroy(_runner.gameObject);
            _runner = null;
            
            // Esperar un frame para asegurar destrucción completa
            await Task.Delay(100);
        }
        
        // Crear NetworkRunner nuevo y fresco
        GameObject runnerObj = new GameObject("NetworkRunner");
        Object.DontDestroyOnLoad(runnerObj);
        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        
        Debug.Log($"✓ NetworkRunner creado: {_runner.gameObject.name}");
        
        // Configurar región EU
        if (Fusion.Photon.Realtime.PhotonAppSettings.TryGetGlobal(out var photonSettings))
        {
            photonSettings.AppSettings.FixedRegion = "eu";
            Debug.Log("✓ Región configurada: EU");
        }
        
        // Conectar al lobby primero
        var lobbyResult = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        
        if (!lobbyResult.Ok)
        {
            Debug.LogError($"❌ Error al conectar al lobby: {lobbyResult.ErrorMessage}");
            return;
        }
        
        Debug.Log("✓ Conectado al lobby, creando sesión...");
        
        // Pequeño delay para sincronización
        await Task.Delay(300);
        
        // Obtener la escena Players desde Build Settings (índice 2)
        // 0: Splash, 1: MainMenu, 2: Players, 3: NewGame
        var playersSceneRef = SceneRef.FromIndex(2);
        var sceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        
        Debug.Log($"🎮 Iniciando sesión como HOST...");
        
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = nombrePartida,
            Scene = playersSceneRef, // Cargar directamente la escena Players
            SceneManager = sceneManager,
            PlayerCount = HelperClass.MAX_PLAYERS,
            IsVisible = true,
            IsOpen = true
        });
        
        if (result.Ok)
        {
            Debug.Log($"✅ Partida '{nombrePartida}' creada como HOST en región {_runner.SessionInfo.Region}");
            Debug.Log($"   - GameMode: {_runner.GameMode}");
            Debug.Log($"   - IsServer: {_runner.IsServer}");
            Debug.Log($"   - IsClient: {_runner.IsClient}");
        }
        else
        {
            Debug.LogError($"❌ Error al crear partida: {result.ErrorMessage}");
        }
    }
    
    /// <summary>
    /// Se une a una partida existente como Client y luego carga la escena Players
    /// </summary>
    public static async Task UnirseAPartidaYCargarPlayers(string nombrePartida)
    {
        Debug.Log($"📡 Uniéndose a partida '{nombrePartida}' como Client...");
        
        // Guardar información para usar después
        PlayerPrefs.SetString("NombrePartida", nombrePartida);
        PlayerPrefs.SetString("TipoPartida", "Client");
        PlayerPrefs.Save();
        
        // Crear NetworkRunner si no existe
        if (_runner == null)
        {
            GameObject runnerObj = new GameObject("NetworkRunner");
            Object.DontDestroyOnLoad(runnerObj);
            _runner = runnerObj.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }
        
        // Configurar región EU
        if (Fusion.Photon.Realtime.PhotonAppSettings.TryGetGlobal(out var photonSettings))
        {
            photonSettings.AppSettings.FixedRegion = "eu";
            Debug.Log("✓ Región configurada: EU");
        }
        
        // Conectar al lobby
        var lobbyResult = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        
        if (!lobbyResult.Ok)
        {
            Debug.LogError($"❌ Error al conectar al lobby: {lobbyResult.ErrorMessage}");
            return;
        }
        
        Debug.Log("✓ Conectado al lobby, buscando sesión...");
        
        await Task.Delay(300);
        
        // Unirse a la sesión (Fusion sincronizará automáticamente la escena del host)
        var sceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = nombrePartida,
            SceneManager = sceneManager
            // No especificamos Scene, el cliente cargará automáticamente la escena del host
        });
        
        if (result.Ok)
        {
            Debug.Log($"✅ Unido a partida '{nombrePartida}' correctamente");
        }
        else
        {
            Debug.LogError($"❌ Error al unirse a partida: {result.ErrorMessage}");
        }
    }
    
    /// <summary>
    /// Obtiene el NetworkRunner actual (si existe)
    /// </summary>
    public static NetworkRunner GetRunner()
    {
        return _runner;
    }
}
