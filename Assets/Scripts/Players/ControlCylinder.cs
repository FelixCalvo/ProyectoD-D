using UnityEngine;
using TMPro;
using Fusion;
using System.Collections.Generic;

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
    
    // Materiales originales de los personajes
    private Dictionary<GameObject, Material[]> materialesOriginales = new Dictionary<GameObject, Material[]>();
    private string miNombreUsuario = "";

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
    }

    public override void Spawned()
    {
        base.Spawned();
        
        // Inicialización de red una vez que el objeto está spawneado
        if (cylinderPlayers != null)
        {
            GuardarMaterialesOriginales();
        }
        
        // Actualizar UI con información de red
        ActualizarUIPersonajeActual();
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
                
                // Actualizar UI cuando termine la rotación
                ActualizarUIPersonajeActual();
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
            
            // Verificar si el personaje ya está seleccionado por otro jugador
            if (PersonajeYaSeleccionado(nombrePersonaje))
            {
                string usuarioOcupante = ObtenerUsuarioDelPersonaje(nombrePersonaje);
                Debug.LogWarning($"❌ El personaje '{nombrePersonaje}' ya está seleccionado por '{usuarioOcupante}'");
                MostrarMensaje($"❌ '{nombrePersonaje}' ya está seleccionado por '{usuarioOcupante}'", false);
                return;
            }
            
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
            
            // Guardar nombre de usuario localmente
            miNombreUsuario = nombreUsuario;
            
            // Guardar en PlayerPrefs localmente
            PlayerPrefs.SetString("NombrePersonajeSeleccionado", nombrePersonaje);
            PlayerPrefs.SetString("NombreUsuario", nombreUsuario);
            PlayerPrefs.Save();
            
            Debug.Log($"✓ Personaje seleccionado: {nombrePersonaje}");
            Debug.Log($"✓ Usuario: {nombreUsuario}");
            
            MostrarMensaje($"✓ Has seleccionado a '{nombrePersonaje}'", true);
            
            // Notificar a la red sobre la selección
            if (Object != null && Object.HasStateAuthority)
            {
                RPC_SeleccionarPersonaje(nombrePersonaje, nombreUsuario);
            }
    }

    /// <summary>
    /// RPC para sincronizar la selección de personaje a todos los clientes
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SeleccionarPersonaje(NetworkString<_32> nombrePersonaje, NetworkString<_32> nombreUsuario)
    {
        // Agregar al diccionario sincronizado
        personajesSeleccionados.Add(nombrePersonaje, nombreUsuario);
        
        Debug.Log($"🌐 [RED] Personaje '{nombrePersonaje.Value}' seleccionado por '{nombreUsuario.Value}'");
        
        // Aplicar efectos visuales al personaje seleccionado
        AplicarEfectoVisualPersonaje(nombrePersonaje.Value, nombreUsuario.Value);
        
        // Actualizar UI
        ActualizarUIPersonajeActual();
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
    /// Guarda los materiales originales de todos los personajes
    /// </summary>
    private void GuardarMaterialesOriginales()
    {
        if (cylinderPlayers == null) return;
        
        // Solo guardar los primeros 4 hijos (personajes)
        int maxPersonajes = Mathf.Min(cylinderPlayers.transform.childCount, 4);
        for (int i = 0; i < maxPersonajes; i++)
        {
            Transform hijo = cylinderPlayers.transform.GetChild(i);
            Renderer[] renderers = hijo.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Material[] materiales = new Material[renderers.Length];
                for (int j = 0; j < renderers.Length; j++)
                {
                    materiales[j] = renderers[j].material;
                }
                materialesOriginales[hijo.gameObject] = materiales;
            }
        }
    }
    
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
            if (PersonajeYaSeleccionado(nombrePersonaje))
            {
                string usuario = ObtenerUsuarioDelPersonaje(nombrePersonaje);
                bool esMiSeleccion = !string.IsNullOrEmpty(miNombreUsuario) && usuario == miNombreUsuario;
                
                if (esMiSeleccion)
                {
                    textoEstadoPersonaje.text = "✓ TU SELECCIÓN";
                    textoEstadoPersonaje.color = colorMiSeleccion;
                }
                else
                {
                    textoEstadoPersonaje.text = $"❌ Ocupado por: {usuario}";
                    textoEstadoPersonaje.color = colorOcupado;
                }
            }
            else
            {
                textoEstadoPersonaje.text = "✓ DISPONIBLE";
                textoEstadoPersonaje.color = colorDisponible;
            }
        }
    }
    
    /// <summary>
    /// Aplica efecto visual al personaje seleccionado
    /// </summary>
    private void AplicarEfectoVisualPersonaje(string nombrePersonaje, string nombreUsuario)
    {
        if (cylinderPlayers == null) return;
        
        // Buscar el personaje por nombre
        foreach (Transform hijo in cylinderPlayers.transform)
        {
            if (hijo.name == nombrePersonaje)
            {
                bool esMiSeleccion = nombreUsuario == miNombreUsuario;
                Color colorAplicar = esMiSeleccion ? colorMiSeleccion : colorOcupado;
                
                // Aplicar color a todos los renderers del personaje
                Renderer[] renderers = hijo.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    // Crear nuevo material con color modificado
                    Material nuevoMaterial = new Material(renderer.material);
                    nuevoMaterial.color = colorAplicar;
                    renderer.material = nuevoMaterial;
                }
                
                // Añadir outline si está disponible (opcional)
                AgregarOutline(hijo.gameObject, colorAplicar);
                
                break;
            }
        }
    }
    
    /// <summary>
    /// Agrega un outline al personaje (requiere shader o componente específico)
    /// </summary>
    private void AgregarOutline(GameObject personaje, Color color)
    {
        // Esta es una implementación básica
        // Para un outline real, necesitarías un shader específico o el paquete "Quick Outline"
        
        // Opción simple: añadir un glow con emisión
        Renderer[] renderers = personaje.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", color * 0.3f);
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
    
    /// <summary>
    /// Restaura los materiales originales de un personaje
    /// </summary>
    private void RestaurarMaterialesOriginales(GameObject personaje)
    {
        if (materialesOriginales.ContainsKey(personaje))
        {
            Renderer[] renderers = personaje.GetComponentsInChildren<Renderer>();
            Material[] materiales = materialesOriginales[personaje];
            
            for (int i = 0; i < renderers.Length && i < materiales.Length; i++)
            {
                renderers[i].material = materiales[i];
            }
        }
    }
}
