using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;
using System.Collections;

public class ExitGameWithFade : MonoBehaviour
{
    [Header("Fade Settings")]
    public Image fadePanel;
    public float fadeDuration = 2f;

    [Header("Audio")]
    public float musicFadeTime = 2.5f;

    public void ExitToMenu()
    {
        // Proteger este GameObject de ser destruido durante el cambio de escena
        DontDestroyOnLoad(gameObject);
        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        Debug.Log("=== INICIO ExitRoutine ===");
        
        // 1. Activar el panel si estaba apagado
        fadePanel.gameObject.SetActive(true);

        // 2. Bloquear clics
        fadePanel.raycastTarget = true;

        // 3. Fade música (si existe SoundManager)
        //if (SoundManager.Instance != null)
            //StartCoroutine(SoundManager.Instance.FadeOutMusic(musicFadeTime));

        // 4. Fade a negro
        float t = 0;
        Color c = fadePanel.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            fadePanel.color = c;
            yield return null;
        }

        // 5. Proteger el fadePanel de ser destruido
        DontDestroyOnLoad(fadePanel.transform.root.gameObject);
        
        // 6. Cargar MainMenu ANTES de destruir Fusion
        Debug.Log("Cargando MainMenu...");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu");
        asyncLoad.allowSceneActivation = false; // No activar todavía
        
        // Esperar a que esté casi cargada (90%)
        while (asyncLoad.progress < 0.9f)
        {
            Debug.Log($"Progreso de carga: {asyncLoad.progress * 100}%");
            yield return null;
        }
        
        Debug.Log("Escena precargada al 90%, cerrando NetworkRunner...");
        
        // 7. Cerrar Fusion DESPUÉS de precargar la escena
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("Cerrando NetworkRunner...");
            runner.Shutdown(destroyGameObject: true); // Destruir automáticamente
        }
        
        Debug.Log("NetworkRunner cerrado, activando escena...");
        
        // Limpiar PlayerPrefs
        PlayerPrefs.DeleteKey("TipoPartida");
        PlayerPrefs.DeleteKey("NombrePartida");
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs limpiados");
        
        // Activar la escena precargada INMEDIATAMENTE
        asyncLoad.allowSceneActivation = true;
        
        // Esperar a que termine
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        Debug.Log("=== ESCENA CARGADA ===");
    }
}
