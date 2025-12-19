using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [Header("Configuración UI")]
    [Tooltip("Panel UI que se muestra cuando se pulsa ESC")]
    public GameObject panelPausa;
    
    [Tooltip("Nombre de la escena del menú principal")]
    public string nombreEscenaMainMenu = "MainMenu";

    [Header("Configuración Desarrollador")]
    [Tooltip("Tecla para activar/desactivar modo desarrollador")]
    public KeyCode teclaModoDesarrollador = KeyCode.F12;

    [Header("Estado")]
    public bool juegoPausado = false;
    public bool modoDesarrollador = false;

    private float timeScaleOriginal = 1f;

    private void Start()
    {
        timeScaleOriginal = Time.timeScale;
        
        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }
    }

    private void Update()
    {
        // Abrir/Cerrar panel de pausa con ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!juegoPausado)
            {
                AbrirPanelPausa();
            }
            else
            {
                BotonContinuar();
            }
        }
    }

    /// <summary>
    /// Abre el panel de pausa y congela el juego
    /// </summary>
    private void AbrirPanelPausa()
    {
        juegoPausado = true;
        Time.timeScale = 0f;
        
        if (panelPausa != null)
        {
            panelPausa.SetActive(true);
        }
        
        Debug.Log("Panel de pausa abierto");
    }

    // ============================================
    // MÉTODOS PÚBLICOS PARA BOTONES DEL PANEL
    // ============================================
    
    /// <summary>
    /// Botón 1: Continuar - Cierra el panel y reanuda el juego
    /// </summary>
    public void BotonContinuar()
    {
        juegoPausado = false;
        Time.timeScale = timeScaleOriginal;
        
        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }
        
        Debug.Log("Juego reanudado");
    }

    /// <summary>
    /// Botón 2: Volver al MainMenu - Carga la escena del menú principal
    /// </summary>
    public void BotonVolverAlMenu()
    {
        Debug.Log($"Volviendo a {nombreEscenaMainMenu}...");
        Time.timeScale = timeScaleOriginal; // Restaurar timeScale antes de cambiar escena
        SceneManager.LoadScene(nombreEscenaMainMenu);
    }

    /// <summary>
    /// Botón 3: Salir al escritorio - Cierra la aplicación
    /// </summary>
    public void BotonSalirAlEscritorio()
    {
        Debug.Log("Saliendo del juego al escritorio...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Activa o desactiva el modo desarrollador
    /// </summary>
    public void AlternarModoDesarrollador()
    {
        modoDesarrollador = !modoDesarrollador;
        
        if (modoDesarrollador)
        {
            Debug.Log("<color=green>MODO DESARROLLADOR ACTIVADO</color>");
        }
        else
        {
            Debug.Log("<color=red>MODO DESARROLLADOR DESACTIVADO</color>");
        }
    }

    /// <summary>
    /// Funciones adicionales disponibles en modo desarrollador
    /// </summary>
    private void FuncionesDesarrollador()
    {
        // Recargar escena actual (R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Recargando escena...");
            Time.timeScale = timeScaleOriginal;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Slow motion (tecla 1)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = 0.5f;
            Debug.Log("Slow motion: 0.5x");
        }

        // Velocidad normal (tecla 2)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Time.timeScale = 1f;
            Debug.Log("Velocidad normal: 1x");
        }

        // Fast forward (tecla 3)
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Time.timeScale = 2f;
            Debug.Log("Fast forward: 2x");
        }

        // Mostrar FPS en consola (tecla F)
        if (Input.GetKeyDown(KeyCode.F))
        {
            float fps = 1f / Time.unscaledDeltaTime;
            Debug.Log($"FPS: {fps:F1}");
        }

        // Información de sistema (tecla I)
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"=== INFORMACIÓN DEL SISTEMA ===");
            Debug.Log($"Plataforma: {Application.platform}");
            Debug.Log($"Unity Version: {Application.unityVersion}");
            Debug.Log($"Escena actual: {SceneManager.GetActiveScene().name}");
            Debug.Log($"Time.timeScale: {Time.timeScale}");
            Debug.Log($"GPU: {SystemInfo.graphicsDeviceName}");
            Debug.Log($"RAM: {SystemInfo.systemMemorySize} MB");
        }
    }

    private void OnGUI()
    {
        if (modoDesarrollador)
        {
            // Mostrar indicador en pantalla
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 10, 300, 30), "MODO DESARROLLADOR ACTIVO (F12)");
            GUI.Label(new Rect(10, 30, 300, 30), "R: Recargar | 1/2/3: Velocidad | F: FPS | I: Info");
            GUI.color = Color.white;
        }
    }
}
