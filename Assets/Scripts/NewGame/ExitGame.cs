using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;
using System.Collections;

/// <summary>
/// Gestiona la salida del juego con efecto de fade y limpieza de recursos de red
/// </summary>
public class ExitGameWithFade : MonoBehaviour
{
    [Header("Fade Settings")]
    public Image fadePanel;
    public float fadeDuration = 2f;

    [Header("Audio")]
    public float musicFadeTime = 2.5f;

    /// <summary>
    /// Inicia la rutina de salida al menú principal
    /// </summary>
    public void ExitToMenu()
    {
        // Proteger este GameObject de ser destruido durante el cambio de escena
        DontDestroyOnLoad(gameObject);
        StartCoroutine(ExitRoutine());
    }

    /// <summary>
    /// Corrutina que maneja el fade out, cierre de conexión y carga de escena
    /// </summary>
    private IEnumerator ExitRoutine()
    {
        // Activar el panel de fade si estaba desactivado
        fadePanel.gameObject.SetActive(true);

        // Bloquear interacciones durante el fade
        fadePanel.raycastTarget = true;

        // Fade de música (opcional, descomentar si existe SoundManager)
        //if (SoundManager.Instance != null)
            //StartCoroutine(SoundManager.Instance.FadeOutMusic(musicFadeTime));

        // Realizar fade a negro
        float t = 0;
        Color c = fadePanel.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            fadePanel.color = c;
            yield return null;
        }

        // Proteger el panel de fade de ser destruido durante el cambio de escena
        DontDestroyOnLoad(fadePanel.transform.root.gameObject);
        
        // Precargar la escena MainMenu sin activarla aún
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu");
        asyncLoad.allowSceneActivation = false;
        
        // Esperar a que la escena esté cargada al 90%
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        
        // Cerrar la conexión de Fusion después de precargar la escena
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            // Shutdown y destrucción automática del NetworkRunner
            runner.Shutdown(destroyGameObject: true);
        }
        
        // Limpiar datos de sesión de PlayerPrefs
        PlayerPrefs.DeleteKey("TipoPartida");
        PlayerPrefs.DeleteKey("NombrePartida");
        PlayerPrefs.Save();
        
        // Activar la escena precargada
        asyncLoad.allowSceneActivation = true;
        
        // Esperar a que la activación termine
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
