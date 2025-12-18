using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de transparencia de paredes para CINEMACHINE en MULTIPLAYER
/// Usar en: Cinemachine Virtual Camera
/// Autodetecta: Jugador local con HelperClass.ActivePlayer
/// </summary>
public class TransparentarParedesMultiplayer : MonoBehaviour
{
    public GameObject player; // Se autodetecta con HelperClass.ActivePlayer
    public GameObject rootObjetosTransparentes;
    public float alturaCapsule = 2.0f;
    public float radioCapsule = 0.4f;
    public float tiempoEsperaRestaurar = 0.5f;
    [Range(0f, 1f)]
    public float valorTransparencia = 0.2f;
    public LayerMask layerObjetosTransparentes;

    private Dictionary<Renderer, Material[]> materialesOriginales = new Dictionary<Renderer, Material[]>();
    private Dictionary<GameObject, List<Renderer>> rendersPorPared = new Dictionary<GameObject, List<Renderer>>();
    private Dictionary<GameObject, float> tiemposSinBloquear = new Dictionary<GameObject, float>();

    void Update()
    {
        // Autodetectar el jugador local en multiplayer
        if (player == null)
        {
            player = HelperClass.ActivePlayer;
        }
        
        if (player == null) return;

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

        HashSet<GameObject> objetosBloqueando = new HashSet<GameObject>();
        
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
                
                if (obj.layer == LayerMask.NameToLayer("Ground"))
                {
                    continue;
                }
                
                bool esPuerta = obj.CompareTag("Door") || obj.GetComponent<DoorTrigger>() != null;
                if (obj.transform.parent != null)
                {
                    esPuerta = esPuerta || obj.transform.parent.CompareTag("Door") || 
                               obj.transform.parent.GetComponent<DoorTrigger>() != null;
                }
                
                if (esPuerta)
                {
                    if (rendersPorPared.ContainsKey(obj))
                    {
                        RestaurarOpacidad(obj);
                    }
                }
                else if (obj.layer == LayerMask.NameToLayer("ObjetosTransparentes"))
                {
                    if (!TienePuertaCerrada(obj))
                    {
                        objetosBloqueando.Add(obj);
                    }
                    else if (rendersPorPared.ContainsKey(obj))
                    {
                        RestaurarOpacidad(obj);
                    }
                }
            }
        }

        List<GameObject> paredesARestaurar = new List<GameObject>();
        foreach (GameObject pared in rendersPorPared.Keys)
        {
            if (!objetosBloqueando.Contains(pared))
            {
                if (!tiemposSinBloquear.ContainsKey(pared))
                {
                    tiemposSinBloquear[pared] = 0f;
                }
                
                tiemposSinBloquear[pared] += Time.deltaTime;
                
                if (tiemposSinBloquear[pared] >= tiempoEsperaRestaurar)
                {
                    paredesARestaurar.Add(pared);
                }
            }
            else
            {
                tiemposSinBloquear.Remove(pared);
            }
        }
        
        foreach (GameObject pared in paredesARestaurar)
        {
            RestaurarOpacidad(pared);
            tiemposSinBloquear.Remove(pared);
        }

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
        // Si ya está transparente, no hacer nada
        if (rendersPorPared.ContainsKey(pared)) return;
        
        Renderer[] renderers = pared.GetComponentsInChildren<Renderer>(includeInactive: false);
        
        rendersPorPared[pared] = new List<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // Guardar materiales originales SOLO si no los tenemos ya
            if (!materialesOriginales.ContainsKey(renderer))
            {
                materialesOriginales[renderer] = renderer.materials;
            }
            
            rendersPorPared[pared].Add(renderer);

            // Crear materiales transparentes
            Material[] materiales = renderer.materials;
            for (int i = 0; i < materiales.Length; i++)
            {
                materiales[i] = new Material(materiales[i]);

                materiales[i].SetFloat("_Surface", 1);
                materiales[i].SetFloat("_Blend", 0);
                materiales[i].SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                materiales[i].SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                materiales[i].SetFloat("_ZWrite", 0);
                materiales[i].renderQueue = 3000;
                materiales[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                materiales[i].EnableKeyword("_ALPHAPREMULTIPLY_ON");

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
        List<GameObject> paredes = new List<GameObject>(rendersPorPared.Keys);
        
        foreach (GameObject pared in paredes)
        {
            RestaurarOpacidad(pared);
        }
        
        tiemposSinBloquear.Clear();
    }

    bool TienePuertaCerrada(GameObject obj)
    {
        DoorTrigger[] puertas = obj.GetComponentsInChildren<DoorTrigger>();
        
        foreach (DoorTrigger puerta in puertas)
        {
            if (!puerta.IsDoorOpen())
            {
                return true;
            }
        }
        
        return false;
    }
}
