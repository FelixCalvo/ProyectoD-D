using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparentarParedes : MonoBehaviour
{
    public GameObject player;
    public GameObject rootObjetosTransparentes; // GameObject raíz (Castle) - solo sus hijos se harán transparentes
    public float alturaCapsule = 2.0f; // Altura del jugador
    public float radioCapsule = 0.4f; // Radio del CapsuleCast
    public float tiempoEsperaRestaurar = 0.5f; // Tiempo antes de restaurar
    public LayerMask layerObjetosTransparentes;

    // Diccionario para almacenar materiales originales
    private Dictionary<Renderer, Material[]> materialesOriginales = new Dictionary<Renderer, Material[]>();
    // HashSet para almacenar objetos actualmente transparentes
    private HashSet<GameObject> objetosTransparentes = new HashSet<GameObject>();
    // Diccionario para rastrear tiempo desde que dejó de bloquear
    private Dictionary<GameObject, float> tiemposSinBloquear = new Dictionary<GameObject, float>();

    void Update()
    {
        if (player == null) return;

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
                    if (objetosTransparentes.Contains(obj))
                    {
                        RestaurarOpacidad(obj);
                        objetosTransparentes.Remove(obj);
                    }
                    // No agregar a bloqueantes
                }
                else
                {
                    // Verificar si el objeto es descendiente del root permitido
                    bool esDescendiente = false;
                    if (rootObjetosTransparentes != null)
                    {
                        Transform current = obj.transform;
                        while (current != null)
                        {
                            if (current.gameObject == rootObjetosTransparentes)
                            {
                                esDescendiente = true;
                                break;
                            }
                            current = current.parent;
                        }
                    }
                    else
                    {
                        // Si no hay root especificado, permitir todos
                        esDescendiente = true;
                    }
                    
                    if (esDescendiente)
                    {
                        // Si el objeto no tiene renderer, buscar en el padre
                        if (obj.GetComponent<Renderer>() == null && obj.transform.parent != null)
                        {
                            obj = obj.transform.parent.gameObject;
                        }
                        
                        objetosBloqueando.Add(obj);
                    }
                }
            }
        }

        // Manejar histéresis: actualizar timers
        List<GameObject> objetosARestaurar = new List<GameObject>();
        foreach (GameObject obj in objetosTransparentes)
        {
            if (!objetosBloqueando.Contains(obj))
            {
                // Incrementar tiempo sin bloquear
                if (!tiemposSinBloquear.ContainsKey(obj))
                {
                    tiemposSinBloquear[obj] = 0f;
                }
                
                tiemposSinBloquear[obj] += Time.deltaTime;
                
                // Si ya pasó suficiente tiempo, restaurar
                if (tiemposSinBloquear[obj] >= tiempoEsperaRestaurar)
                {
                    objetosARestaurar.Add(obj);
                }
            }
            else
            {
                // Sigue bloqueando, resetear timer
                if (tiemposSinBloquear.ContainsKey(obj))
                {
                    tiemposSinBloquear.Remove(obj);
                }
            }
        }
        
        // Restaurar objetos que cumplieron el tiempo de espera
        foreach (GameObject obj in objetosARestaurar)
        {
            RestaurarOpacidad(obj);
            objetosTransparentes.Remove(obj);
            tiemposSinBloquear.Remove(obj);
        }

        // Hacer transparentes todos los objetos bloqueantes
        foreach (GameObject obj in objetosBloqueando)
        {
            if (!objetosTransparentes.Contains(obj))
            {
                HacerTransparente(obj);
                objetosTransparentes.Add(obj);
            }
        }
    }

    void HacerTransparente(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (!materialesOriginales.ContainsKey(renderer))
            {
                // Guardar materiales originales
                materialesOriginales[renderer] = renderer.materials;
            }

            Material[] materiales = renderer.materials;
            for (int i = 0; i < materiales.Length; i++)
            {
                // Crear instancia del material si es compartido
                if (materiales[i].GetInstanceID() == renderer.sharedMaterials[i].GetInstanceID())
                {
                    materiales[i] = new Material(materiales[i]);
                }

                // Configurar transparencia para URP
                materiales[i].SetFloat("_Surface", 1); // Transparent
                materiales[i].SetFloat("_Blend", 0); // Alpha
                materiales[i].SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                materiales[i].SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                materiales[i].SetFloat("_ZWrite", 0);
                materiales[i].renderQueue = 3000;

                // Activar keywords para transparencia
                materiales[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                materiales[i].EnableKeyword("_ALPHAPREMULTIPLY_ON");

                // Ajustar alpha
                Color color = materiales[i].color;
                color.a = 0.3f;
                materiales[i].color = color;
            }
            renderer.materials = materiales;

            // Cambiar layer
            obj.layer = LayerMask.NameToLayer("ObjetosTransparentes");
        }
    }

    void RestaurarOpacidad(GameObject obj)
    {
        if (obj == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (materialesOriginales.ContainsKey(renderer))
            {
                renderer.materials = materialesOriginales[renderer];
                materialesOriginales.Remove(renderer);
            }
        }

        // Restaurar layer (asumiendo que era Default)
        obj.layer = 0;
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

