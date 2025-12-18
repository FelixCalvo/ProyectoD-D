using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparentarParedes : MonoBehaviour
{
    public GameObject player; // [DEPRECATED] Se autodetecta en multiplayer
    public GameObject rootObjetosTransparentes; // GameObject raíz (Castle) - solo sus hijos se harán transparentes
    public float alturaCapsule = 2.0f; // Altura del jugador
    public float radioCapsule = 0.4f; // Radio del CapsuleCast
    public float tiempoEsperaRestaurar = 0.5f; // Tiempo antes de restaurar
    [Range(0f, 1f)]
    public float valorTransparencia = 0.2f; // 0 = invisible, 1 = opaco
    public LayerMask layerObjetosTransparentes;

    // Diccionario para almacenar materiales originales por renderer
    private Dictionary<Renderer, Material[]> materialesOriginales = new Dictionary<Renderer, Material[]>();
    // Diccionario: pared -> lista de renderers que se transparentaron
    private Dictionary<GameObject, List<Renderer>> rendersPorPared = new Dictionary<GameObject, List<Renderer>>();
    // Diccionario para rastrear tiempo desde que dejó de bloquear
    private Dictionary<GameObject, float> tiemposSinBloquear = new Dictionary<GameObject, float>();

    void Update()
    {
        // Autodetectar el jugador local (singleplayer o multiplayer)
        if (player == null)
        {
            player = HelperClass.ActivePlayer;
            if (player != null)
            {
                Debug.Log($"[TransparentarParedes] ✅ Jugador detectado: {player.name}");
            }
        }
        
        if (player == null)
        {
            Debug.LogWarning($"[TransparentarParedes] ⚠️ No hay jugador activo (HelperClass.ActivePlayer = null)");
            return;
        }

        // Si hay un diálogo activo, restaurar todas las paredes
        if (DialogueBlocker.Instance != null && DialogueBlocker.Instance.IsDialogueActive)
        {
            RestaurarTodasLasParedes();
            return;
        }

        Vector3 posPies = player.transform.position;
        Vector3 posCabeza = posPies + Vector3.up * alturaCapsule;
        Vector3 direccion = posPies - transform.position;
        float distancia = direccion.magnitude;
        direccion.Normalize();

        // Lista de objetos bloqueantes detectados
        HashSet<GameObject> objetosBloqueando = new HashSet<GameObject>();
        
        // CapsuleCastAll para detectar TODOS los objetos que bloquean
        RaycastHit[] hits = Physics.CapsuleCastAll(
            transform.position, 
            transform.position + Vector3.up * alturaCapsule, 
            radioCapsule, 
            direccion, 
            distancia
        );
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject != player && hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
            {
                GameObject obj = hit.collider.gameObject;
                
                // Ignorar objetos en layer Ground (terreno, suelo)
                if (obj.layer == LayerMask.NameToLayer("Ground"))
                {
                    continue;
                }
                
                // Verificar si es una puerta
                bool esPuerta = obj.CompareTag("Door") || obj.GetComponent<DoorTrigger>() != null;
                if (obj.transform.parent != null)
                {
                    esPuerta = esPuerta || obj.transform.parent.CompareTag("Door") || 
                               obj.transform.parent.GetComponent<DoorTrigger>() != null;
                }
                
                if (esPuerta)
                {
                    // Restaurar puerta inmediatamente si estaba transparente
                    if (rendersPorPared.ContainsKey(obj))
                    {
                        RestaurarOpacidad(obj);
                    }
                    // No agregar a bloqueantes
                }
                // SOLO procesar si el objeto detectado ES UNA PARED (layer ObjetosTransparentes)
                else if (obj.layer == LayerMask.NameToLayer("ObjetosTransparentes"))
                {
                    // Verificar si la pared tiene una puerta cerrada
                    if (!TienePuertaCerrada(obj))
                    {
                        // Transparentar toda la pared con todos sus hijos y nietos
                        objetosBloqueando.Add(obj);
                    }
                    else if (rendersPorPared.ContainsKey(obj))
                    {
                        // Si tiene puerta cerrada, restaurar si estaba transparente
                        RestaurarOpacidad(obj);
                    }
                }
                // Si NO es una pared (libro, ventana, etc.), IGNORAR completamente
            }
        }

        // Manejar histéresis: actualizar timers
        List<GameObject> paredesARestaurar = new List<GameObject>();
        foreach (GameObject pared in rendersPorPared.Keys)
        {
            if (!objetosBloqueando.Contains(pared))
            {
                // Incrementar tiempo sin bloquear
                if (!tiemposSinBloquear.ContainsKey(pared))
                {
                    tiemposSinBloquear[pared] = 0f;
                }
                
                tiemposSinBloquear[pared] += Time.deltaTime;
                
                // Si ya pasó suficiente tiempo, restaurar
                if (tiemposSinBloquear[pared] >= tiempoEsperaRestaurar)
                {
                    paredesARestaurar.Add(pared);
                }
            }
            else
            {
                // Sigue bloqueando, resetear timer
                tiemposSinBloquear.Remove(pared);
            }
        }
        
        // Restaurar paredes que cumplieron el tiempo de espera
        foreach (GameObject pared in paredesARestaurar)
        {
            RestaurarOpacidad(pared);
            tiemposSinBloquear.Remove(pared);
        }

        // Hacer transparentes todas las paredes bloqueantes
        foreach (GameObject pared in objetosBloqueando)
        {
            if (!rendersPorPared.ContainsKey(pared))
            {
                HacerTransparente(pared);
            }
        }
    }

    void HacerTransparente(GameObject pared)
    {
        // Obtener TODOS los renderers de la pared y sus hijos
        Renderer[] renderers = pared.GetComponentsInChildren<Renderer>(includeInactive: false);
        
        // Crear lista para guardar qué renderers procesamos
        rendersPorPared[pared] = new List<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // Guardar materiales originales
            materialesOriginales[renderer] = renderer.materials;
            rendersPorPared[pared].Add(renderer);

            // Crear materiales transparentes
            Material[] materiales = renderer.materials;
            for (int i = 0; i < materiales.Length; i++)
            {
                // Crear instancia del material
                materiales[i] = new Material(materiales[i]);

                // Configurar transparencia para URP
                materiales[i].SetFloat("_Surface", 1);
                materiales[i].SetFloat("_Blend", 0);
                materiales[i].SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                materiales[i].SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                materiales[i].SetFloat("_ZWrite", 0);
                materiales[i].renderQueue = 3000;
                materiales[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                materiales[i].EnableKeyword("_ALPHAPREMULTIPLY_ON");

                // Ajustar alpha
                Color color = materiales[i].color;
                color.a = valorTransparencia;
                materiales[i].color = color;
            }
            renderer.materials = materiales;
        }
    }

    void RestaurarOpacidad(GameObject pared)
    {
        if (pared == null || !rendersPorPared.ContainsKey(pared)) return;

        // Restaurar materiales de todos los renderers
        foreach (Renderer renderer in rendersPorPared[pared])
        {
            if (renderer != null && materialesOriginales.ContainsKey(renderer))
            {
                renderer.materials = materialesOriginales[renderer];
                materialesOriginales.Remove(renderer);
            }
        }
        rendersPorPared.Remove(pared);
    }

    void RestaurarTodasLasParedes()
    {
        // Restaurar todas las paredes transparentes
        List<GameObject> paredes = new List<GameObject>(rendersPorPared.Keys);
        
        foreach (GameObject pared in paredes)
        {
            RestaurarOpacidad(pared);
        }
        
        // Limpiar colecciones
        tiemposSinBloquear.Clear();
    }

    void OnDrawGizmos()
    {
        if (player == null) return;

        Vector3 posPies = player.transform.position;
        Vector3 posCabeza = posPies + Vector3.up * alturaCapsule;
        Vector3 direccion = (posPies - transform.position).normalized;

        // Cápsula en la cámara (verde)
        Gizmos.color = Color.green;
        Vector3 point1Cam = transform.position;
        Vector3 point2Cam = transform.position + Vector3.up * alturaCapsule;
        DrawWireCapsule(point1Cam, point2Cam, radioCapsule);

        // Cápsula en el jugador (amarillo)
        Gizmos.color = Color.yellow;
        DrawWireCapsule(posPies, posCabeza, radioCapsule);

        // Línea desde cámara al jugador
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up * (alturaCapsule * 0.5f), 
                       posPies + Vector3.up * (alturaCapsule * 0.5f));
    }

    /// <summary>
    /// Busca el ancestro (padre, abuelo, etc.) que tiene el layer ObjetosTransparentes
    /// Este será la PARED completa que se transparentará con todos sus hijos
    /// </summary>
    GameObject EncontrarParedAncestro(GameObject obj)
    {
        int layerObjetosTransparentesId = LayerMask.NameToLayer("ObjetosTransparentes");
        
        Transform current = obj.transform;
        
        // Subir por la jerarquía hasta encontrar el objeto con layer ObjetosTransparentes
        while (current != null)
        {
            if (current.gameObject.layer == layerObjetosTransparentesId)
            {
                return current.gameObject;
            }
            current = current.parent;
        }
        
        // Si no encontramos ningún ancestro con ese layer, el objeto mismo podría tenerlo
        if (obj.layer == layerObjetosTransparentesId)
        {
            return obj;
        }
        
        return null;
    }
    
    /// <summary>
    /// Verifica si el objeto o sus hijos tienen una puerta cerrada
    /// </summary>
    bool TienePuertaCerrada(GameObject obj)
    {
        // Buscar DoorTrigger en el objeto y sus hijos
        DoorTrigger[] puertas = obj.GetComponentsInChildren<DoorTrigger>();
        
        foreach (DoorTrigger puerta in puertas)
        {
            // Si la puerta NO está abierta, la pared no debe transparentarse
            if (!puerta.IsDoorOpen())
            {
                return true;
            }
        }
        
        return false;
    }
    
    void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius)
    {
        // Esferas en los extremos
        Gizmos.DrawWireSphere(point1, radius);
        Gizmos.DrawWireSphere(point2, radius);
        
        // Líneas verticales
        Vector3[] offsets = new Vector3[]
        {
            Vector3.forward * radius,
            Vector3.back * radius,
            Vector3.left * radius,
            Vector3.right * radius
        };
        
        foreach (Vector3 offset in offsets)
        {
            Gizmos.DrawLine(point1 + offset, point2 + offset);
        }
    }
}

