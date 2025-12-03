using UnityEngine;
using TMPro;
using Fusion;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class ControlCylinder : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject cylinderPlayers;
    [SerializeField] private TMP_InputField inputNombreUsuario;
    
    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI textoNombrePersonaje;
    [SerializeField] private TextMeshProUGUI textoEstadoPersonaje;
    [SerializeField] private GameObject panelMensaje;
    [SerializeField] private TextMeshProUGUI textoMensaje;
    
    [Header("Botón Iniciar Partida")]
    [SerializeField] private Button botonIniciarPartida;
    [SerializeField] private TextMeshProUGUI textoBotonIniciar;
    
    [Header("Configuración")]
    [SerializeField] private float velocidadRotacion = 5f;
    [SerializeField] private Color colorDisponible = Color.green;
    [SerializeField] private Color colorOcupado = Color.red;
    [SerializeField] private Color colorMiSeleccion = Color.cyan;

    private Quaternion rotacionObjetivo;
    private bool rotando = false;
    private GameObject personajeSeleccionado = null;
    private int indicePersonajeActual = 0; // Índice del personaje frente a la cámara
    private int totalPersonajes = 0; // Total de personajes en el cilindro
    
    private string miNombreUsuario = "";
    private string miPersonajeSeleccionado = ""; // Caché local de mi selección
    private int ultimoCountPersonajes = 0; // Para detectar cambios en el diccionario

    // Diccionario sincronizado en red: nombrePersonaje -> nombreUsuario
    [Networked, Capacity(4)]
    private NetworkDictionary<NetworkString<_32>, NetworkString<_32>> personajesSeleccionados => default;

    private void Start()
    {
        // Inicialización local (no de red)
        if (cylinderPlayers != null)
        {
            rotacionObjetivo = cylinderPlayers.transform.rotation;
            // Solo contar los primeros 4 hijos (personajes), el 5º es el cubo
            totalPersonajes = Mathf.Min(cylinderPlayers.transform.childCount, 4);
            indicePersonajeActual = 0; // Empezamos en el índice 0
            
            // Cargar información inicial del personaje en posición 0
            ActualizarUIPersonajeActual();
        }
        
        if (panelMensaje != null)
        {
            panelMensaje.SetActive(false);
        }
        
        // Configurar botón de iniciar partida
        if (botonIniciarPartida != null)
        {
            botonIniciarPartida.onClick.AddListener(IniciarPartida);
            botonIniciarPartida.gameObject.SetActive(false); // Ocultar por defecto
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        
        // Registrar nombre de usuario en el registry
        string nombreUsuario = PlayerPrefs.GetString("NombreUsuario", "");
        if (!string.IsNullOrEmpty(nombreUsuario) && PlayerUserNameRegistry.Instance != null)
        {
            PlayerUserNameRegistry.Instance.RPC_RegisterPlayer(Object.InputAuthority.PlayerId, nombreUsuario);
        }
        
        // Actualizar UI con información de red (incluye valores existentes al unirse)
        ActualizarUIPersonajeActual();
        
        // Mostrar botón solo si eres el Host
        if (botonIniciarPartida != null && Object.HasStateAuthority)
        {
            botonIniciarPartida.gameObject.SetActive(true);
            ActualizarEstadoBoton();
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        // Solo actualizar si el diccionario cambió
        int currentCount = personajesSeleccionados.Count;
        if (currentCount != ultimoCountPersonajes)
        {
            ultimoCountPersonajes = currentCount;
            ActualizarUIPersonajeActual();
            
            if (Object.HasStateAuthority && botonIniciarPartida != null)
            {
                ActualizarEstadoBoton();
            }
        }
    }
    
    void Update()
    {
        // Si estamos rotando, interpolar suavemente hacia la rotación objetivo
        if (rotando && cylinderPlayers != null)
        {
            cylinderPlayers.transform.rotation = Quaternion.Slerp(
                cylinderPlayers.transform.rotation,
                rotacionObjetivo,
                Time.deltaTime * velocidadRotacion
            );

            // Si estamos muy cerca de la rotación objetivo, detener la rotación
            if (Quaternion.Angle(cylinderPlayers.transform.rotation, rotacionObjetivo) < 0.1f)
            {
                cylinderPlayers.transform.rotation = rotacionObjetivo;
                rotando = false;
            }
        }
    }


    ///Rota a la izquierda pero visualmente lo hace a la derecha

    /// <summary>
    /// Rota el cilindro 90 grados a la izquierda (sentido antihorario)
    /// </summary>
    public void RotarDerecha()
    {
        if (cylinderPlayers != null)
        {
            // Rotar -90 grados en el eje Y (izquierda/antihorario)
            rotacionObjetivo *= Quaternion.Euler(0, -90, 0);
            rotando = true;
            
            // Incrementar índice (movemos a la derecha en el array)
            indicePersonajeActual++;
            if (indicePersonajeActual >= totalPersonajes)
            {
                indicePersonajeActual = 0; // Volver al inicio
            }
            
            // Actualizar UI inmediatamente sin esperar a que termine la rotación
            ActualizarUIPersonajeActual();
        }
    }

    /// <summary>
    /// Rota el cilindro 90 grados a la derecha (sentido horario)
    /// </summary>
    public void RotarIzquierda()
    {
        if (cylinderPlayers != null)
        {
            // Rotar +90 grados en el eje Y (derecha/horario)
            rotacionObjetivo *= Quaternion.Euler(0, 90, 0);
            rotando = true;

            // Decrementar índice (movemos a la izquierda en el array)
            indicePersonajeActual--;
            if (indicePersonajeActual < 0)
            {
                indicePersonajeActual = totalPersonajes - 1; // Volver al final
            }
            
            // Actualizar UI inmediatamente sin esperar a que termine la rotación
            ActualizarUIPersonajeActual();
        }
    }

    /// <summary>
    /// Selecciona al personaje que está actualmente delante de la cámara
    /// </summary>
    public void SeleccionarPersonaje()
    {
        if (cylinderPlayers == null)
        {
            Debug.LogError("cylinderPlayers no está asignado!");
            return;
        }

        // Obtener el personaje actual usando el índice
        personajeSeleccionado = ObtenerPersonajeFrontal();
        if (personajeSeleccionado == null)
        {
            Debug.LogError("No se pudo encontrar el personaje frontal");
            return;
        }
        
        string nombrePersonaje = personajeSeleccionado.name;
        
        // Obtener el nombre del usuario desde el InputField
        string nombreUsuario = "";
        if (inputNombreUsuario != null && !string.IsNullOrEmpty(inputNombreUsuario.text))
        {
            nombreUsuario = inputNombreUsuario.text;
        }
        else
        {
            Debug.LogWarning("InputField de nombre de usuario no asignado o vacío. Usando nombre por defecto.");
            nombreUsuario = "Jugador" + Random.Range(1000, 9999);
        }
        
        // Verificar si este usuario ya tiene un personaje seleccionado
        string personajeAnterior = ObtenerPersonajeDelUsuario(nombreUsuario);
        
        // Si intentas seleccionar el mismo personaje que ya tienes, deseleccionar
        if (!string.IsNullOrEmpty(personajeAnterior) && personajeAnterior == nombrePersonaje)
        {
            Debug.Log($"🔓 Deseleccionando personaje '{nombrePersonaje}'");
            MostrarMensaje($"🔓 Has deseleccionado a '{nombrePersonaje}'", true);
            
            // Limpiar caché local
            miPersonajeSeleccionado = "";
            PlayerPrefs.SetString("NombrePersonajeSeleccionado", "");
            PlayerPrefs.Save();
            
            // Notificar a la red para liberar
            if (Object != null && Object.HasStateAuthority)
            {
                RPC_LiberarPersonaje(nombrePersonaje);
            }
            else if (Object != null)
            {
                RPC_SolicitarLiberacion(nombrePersonaje);
            }
            return;
        }
        
        // Verificar si el personaje ya está seleccionado por OTRO jugador
        if (PersonajeYaSeleccionado(nombrePersonaje))
        {
            string usuarioOcupante = ObtenerUsuarioDelPersonaje(nombrePersonaje);
            if (usuarioOcupante != nombreUsuario) // Solo bloquear si NO es tu personaje
            {
                Debug.LogWarning($"❌ El personaje '{nombrePersonaje}' ya está seleccionado por '{usuarioOcupante}'");
                MostrarMensaje($"❌ '{nombrePersonaje}' ya está seleccionado por '{usuarioOcupante}'", false);
                return;
            }
        }
        
        // Guardar nombre de usuario localmente
        miNombreUsuario = nombreUsuario;
        string antiguaSeleccion = miPersonajeSeleccionado;
        miPersonajeSeleccionado = nombrePersonaje; // Guardar en caché local
        
        // Guardar en PlayerPrefs localmente
        PlayerPrefs.SetString("NombrePersonajeSeleccionado", nombrePersonaje);
        PlayerPrefs.SetString("NombreUsuario", nombreUsuario);
        PlayerPrefs.Save();
        
        // Guardar en el sistema de selección global
        PlayerCharacterSelection.SetSelection(nombreUsuario, nombrePersonaje);
        
        MostrarMensaje($"✓ Has seleccionado a '{nombrePersonaje}'", true);
        
        // Actualizar UI LOCALMENTE de forma inmediata (antes del RPC)
        if (textoEstadoPersonaje != null)
        {
            textoEstadoPersonaje.text = "✓ TU SELECCIÓN";
            textoEstadoPersonaje.color = colorMiSeleccion;
        }
        
        // Notificar a la red sobre la selección (incluye liberar anterior)
        if (Object != null && Object.HasStateAuthority)
        {
            // Si eres el Host, ejecuta directamente
            RPC_SeleccionarPersonaje(nombrePersonaje, nombreUsuario, personajeAnterior);
        }
        else if (Object != null)
        {
            // Si eres cliente, solicita al host
            RPC_SolicitarSeleccion(nombrePersonaje, nombreUsuario, personajeAnterior);
        }
    }
    
    /// <summary>
    /// RPC para que los clientes soliciten al host registrar su selección
    /// </summary>
    [Rpc(RpcSources.Proxies, RpcTargets.StateAuthority)]
    private void RPC_SolicitarSeleccion(NetworkString<_32> nombrePersonaje, NetworkString<_32> nombreUsuario, NetworkString<_32> personajeAnterior)
    {
        RPC_SeleccionarPersonaje(nombrePersonaje, nombreUsuario, personajeAnterior);
    }
    
    /// <summary>
    /// RPC para que los clientes soliciten liberar un personaje
    /// </summary>
    [Rpc(RpcSources.Proxies, RpcTargets.StateAuthority)]
    private void RPC_SolicitarLiberacion(NetworkString<_32> nombrePersonaje)
    {
        RPC_LiberarPersonaje(nombrePersonaje);
    }
    
    /// <summary>
    /// RPC para liberar un personaje (deseleccionar)
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_LiberarPersonaje(NetworkString<_32> nombrePersonaje)
    {
        if (personajesSeleccionados.ContainsKey(nombrePersonaje))
        {
            personajesSeleccionados.Remove(nombrePersonaje);
            ActualizarUIPersonajeActual();
            
            if (Object.HasStateAuthority && botonIniciarPartida != null)
            {
                ActualizarEstadoBoton();
            }
        }
    }
    
    /// <summary>
    /// RPC para sincronizar la selección de personaje a todos los clientes
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SeleccionarPersonaje(NetworkString<_32> nombrePersonaje, NetworkString<_32> nombreUsuario, NetworkString<_32> personajeAnterior)
    {
        // Si el usuario tenía un personaje anterior seleccionado, liberarlo
        if (!string.IsNullOrEmpty(personajeAnterior.Value) && personajesSeleccionados.ContainsKey(personajeAnterior))
        {
            personajesSeleccionados.Remove(personajeAnterior);
        }
        
        // Agregar nuevo personaje al diccionario sincronizado
        personajesSeleccionados.Add(nombrePersonaje, nombreUsuario);
        
        // Guardar selección en el sistema global (importante para el spawn)
        PlayerCharacterSelection.SetSelection(nombreUsuario.Value, nombrePersonaje.Value);
        
        // Actualizar UI
        ActualizarUIPersonajeActual();
        
        // Actualizar botón si eres Host
        if (Object.HasStateAuthority && botonIniciarPartida != null)
        {
            ActualizarEstadoBoton();
        }
    }

    /// <summary>
    /// Verifica si un personaje ya está seleccionado
    /// </summary>
    private bool PersonajeYaSeleccionado(string nombrePersonaje)
    {
        // Validar que el objeto está spawneado antes de acceder a propiedades de red
        if (Object == null || !Object.IsValid)
            return false;
            
        NetworkString<_32> key = nombrePersonaje;
        return personajesSeleccionados.ContainsKey(key);
    }

    /// <summary>
    /// Obtiene el nombre del usuario que tiene seleccionado un personaje
    /// </summary>
    private string ObtenerUsuarioDelPersonaje(string nombrePersonaje)
    {
        // Validar que el objeto está spawneado antes de acceder a propiedades de red
        if (Object == null || !Object.IsValid)
            return "";
            
        NetworkString<_32> key = nombrePersonaje;
        if (personajesSeleccionados.TryGet(key, out NetworkString<_32> usuario))
        {
            return usuario.Value;
        }
        return "";
    }

    /// <summary>
    /// Obtiene el personaje que tiene seleccionado un usuario (búsqueda inversa)
    /// </summary>
    private string ObtenerPersonajeDelUsuario(string nombreUsuario)
    {
        // Validar que el objeto está spawneado antes de acceder a propiedades de red
        if (Object == null || !Object.IsValid)
            return "";
            
        foreach (var kvp in personajesSeleccionados)
        {
            if (kvp.Value.Value == nombreUsuario)
            {
                return kvp.Key.Value; // Retornar el nombre del personaje
            }
        }
        return "";
    }
    
    /// <summary>
    /// Obtiene la lista de todos los personajes seleccionados
    /// </summary>
    public Dictionary<string, string> ObtenerPersonajesSeleccionados()
    {
        Dictionary<string, string> resultado = new Dictionary<string, string>();
        
        // Validar que el objeto está spawneado antes de acceder a propiedades de red
        if (Object == null || !Object.IsValid)
            return resultado;
            
        foreach (var kvp in personajesSeleccionados)
        {
            resultado[kvp.Key.Value] = kvp.Value.Value;
        }
        return resultado;
    }
    
    // ==================== MÉTODOS DE FEEDBACK VISUAL ====================
    
    /// <summary>
    /// Obtiene el personaje que está frente a la cámara usando el índice actual
    /// </summary>
    private GameObject ObtenerPersonajeFrontal()
    {
        if (cylinderPlayers == null || totalPersonajes == 0)
            return null;
        
        if (indicePersonajeActual >= 0 && indicePersonajeActual < totalPersonajes)
        {
            return cylinderPlayers.transform.GetChild(indicePersonajeActual).gameObject;
        }
        
        return null;
    }
    
    /// <summary>
    /// Actualiza la UI con el nombre y estado del personaje actual
    /// </summary>
    private void ActualizarUIPersonajeActual()
    {
        GameObject personajeActual = ObtenerPersonajeFrontal();
        if (personajeActual == null) return;
        
        string nombrePersonaje = personajeActual.name;
        
        // Actualizar nombre del personaje
        if (textoNombrePersonaje != null)
        {
            textoNombrePersonaje.text = nombrePersonaje;
        }
        
        // Actualizar estado (con validación de red)
        if (textoEstadoPersonaje != null)
        {
            // PRIORIDAD 1: Verificar caché local primero
            if (!string.IsNullOrEmpty(miPersonajeSeleccionado) && nombrePersonaje == miPersonajeSeleccionado)
            {
                textoEstadoPersonaje.text = "TU SELECCIÓN";
                textoEstadoPersonaje.color = colorMiSeleccion;
            }
            // PRIORIDAD 2: Verificar diccionario de red
            else if (PersonajeYaSeleccionado(nombrePersonaje))
            {
                string usuario = ObtenerUsuarioDelPersonaje(nombrePersonaje);
                bool esMiSeleccion = !string.IsNullOrEmpty(miNombreUsuario) && usuario == miNombreUsuario;
                
                if (esMiSeleccion)
                {
                    textoEstadoPersonaje.text = "TU SELECCIÓN";
                    textoEstadoPersonaje.color = colorMiSeleccion;
                }
                else
                {
                    textoEstadoPersonaje.text = $"OCUPADO POR: {usuario}";
                    textoEstadoPersonaje.color = colorOcupado;
                }
            }
            else
            {
                textoEstadoPersonaje.text = "DISPONIBLE";
                textoEstadoPersonaje.color = colorDisponible;
            }
        }
    }
    
    /// <summary>
    /// Muestra un mensaje temporal en la UI
    /// </summary>
    private void MostrarMensaje(string mensaje, bool esExito)
    {
        if (panelMensaje != null && textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            textoMensaje.color = esExito ? colorDisponible : colorOcupado;
            panelMensaje.SetActive(true);
            
            // Ocultar después de 3 segundos
            CancelInvoke(nameof(OcultarMensaje));
            Invoke(nameof(OcultarMensaje), 3f);
        }
        
        Debug.Log(mensaje);
    }
    
    /// <summary>
    /// Oculta el panel de mensaje
    /// </summary>
    private void OcultarMensaje()
    {
        if (panelMensaje != null)
        {
            panelMensaje.SetActive(false);
        }
    }
    
    // ==================== MÉTODOS DE INICIAR PARTIDA ====================
    
    /// <summary>
    /// Actualiza el estado del botón según si todos han seleccionado
    /// </summary>
    private void ActualizarEstadoBoton()
    {
        if (!Object.HasStateAuthority || botonIniciarPartida == null || Runner == null)
            return;
        
        int jugadoresConectados = Runner.ActivePlayers.Count();
        int personajesSeleccionadosCount = this.personajesSeleccionados.Count;
        
        // Cambio: solo activar cuando hay exactamente 4 jugadores seleccionados
        bool todosSeleccionaron = personajesSeleccionadosCount == 4;
        
        // Activar/desactivar interacción del botón
        botonIniciarPartida.interactable = todosSeleccionaron;
        
        // Cambiar color del botón según el estado
        ColorBlock colores = botonIniciarPartida.colors;
        if (todosSeleccionaron)
        {
            colores.normalColor = colorDisponible;
            colores.highlightedColor = Color.Lerp(colorDisponible, Color.white, 0.3f);
            colores.pressedColor = Color.Lerp(colorDisponible, Color.black, 0.3f);
        }
        else
        {
            colores.normalColor = Color.gray;
            colores.highlightedColor = Color.gray;
            colores.pressedColor = Color.gray;
        }
        botonIniciarPartida.colors = colores;
        
        // Actualizar texto del botón
        if (textoBotonIniciar != null)
        {
            if (todosSeleccionaron)
            {
                textoBotonIniciar.text = "✓ INICIAR PARTIDA";
                textoBotonIniciar.color = Color.white;
            }
            else
            {
                textoBotonIniciar.text = $"Esperando jugadores ({personajesSeleccionadosCount}/4)";
                textoBotonIniciar.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// Inicia la partida cargando la escena NewGame (solo Host)
    /// </summary>
    private void IniciarPartida()
    {
        if (!Object.HasStateAuthority || Runner == null)
        {
            Debug.LogWarning("Solo el Host puede iniciar la partida o Runner no está disponible");
            return;
        }
        
        int seleccionados = personajesSeleccionados.Count;
        
        if (seleccionados < 4)
        {
            MostrarMensaje($"⚠ Faltan jugadores por seleccionar ({seleccionados}/4)", false);
            return;
        }
        
        MostrarMensaje("🎮 Iniciando partida...", true);
        
        // Cargar escena NewGame usando Fusion (preserva NetworkRunner)
        SceneRef newGameScene = SceneRef.FromIndex(3);
        Runner.LoadScene(newGameScene);
    }
    
}
