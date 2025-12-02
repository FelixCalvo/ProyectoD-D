using UnityEngine;
using TMPro;
using Fusion;
using System.Collections.Generic;

public class ControlCylinder : NetworkBehaviour
{
    [SerializeField] private GameObject cylinderPlayers;
    [SerializeField] private float velocidadRotacion = 5f;
    [SerializeField] private TMP_InputField inputNombreUsuario;

    private Quaternion rotacionObjetivo;
    private bool rotando = false;
    private GameObject personajeSeleccionado = null;

    // Diccionario sincronizado en red: nombrePersonaje -> nombreUsuario
    [Networked, Capacity(4)]
    private NetworkDictionary<NetworkString<_32>, NetworkString<_32>> personajesSeleccionados => default;

    void Start()
    {
        if (cylinderPlayers != null)
        {
            rotacionObjetivo = cylinderPlayers.transform.rotation;
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

        // Obtener el primer hijo (índice 0) que siempre está delante de la cámara
        if (cylinderPlayers.transform.childCount > 0)
        {
            personajeSeleccionado = cylinderPlayers.transform.GetChild(0).gameObject;
            string nombrePersonaje = personajeSeleccionado.name;
            
            // Verificar si el personaje ya está seleccionado por otro jugador
            if (PersonajeYaSeleccionado(nombrePersonaje))
            {
                string usuarioOcupante = ObtenerUsuarioDelPersonaje(nombrePersonaje);
                Debug.LogWarning($"❌ El personaje '{nombrePersonaje}' ya está seleccionado por '{usuarioOcupante}'");
                // TODO: Mostrar mensaje en UI
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
            
            // Guardar en PlayerPrefs localmente
            PlayerPrefs.SetString("NombrePersonajeSeleccionado", nombrePersonaje);
            PlayerPrefs.SetString("NombreUsuario", nombreUsuario);
            PlayerPrefs.Save();
            
            Debug.Log($"✓ Personaje seleccionado: {nombrePersonaje}");
            Debug.Log($"✓ Usuario: {nombreUsuario}");
            
            // Notificar a la red sobre la selección
            if (Object != null && Object.HasStateAuthority)
            {
                RPC_SeleccionarPersonaje(nombrePersonaje, nombreUsuario);
            }
        }
        else
        {
            Debug.LogError($"No se encontró ningún hijo en cylinderPlayers. Hijos totales: {cylinderPlayers.transform.childCount}");
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
        
        // TODO: Actualizar UI para mostrar personajes bloqueados
    }

    /// <summary>
    /// Verifica si un personaje ya está seleccionado
    /// </summary>
    private bool PersonajeYaSeleccionado(string nombrePersonaje)
    {
        NetworkString<_32> key = nombrePersonaje;
        return personajesSeleccionados.ContainsKey(key);
    }

    /// <summary>
    /// Obtiene el nombre del usuario que tiene seleccionado un personaje
    /// </summary>
    private string ObtenerUsuarioDelPersonaje(string nombrePersonaje)
    {
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
        foreach (var kvp in personajesSeleccionados)
        {
            resultado[kvp.Key.Value] = kvp.Value.Value;
        }
        return resultado;
    }
}
