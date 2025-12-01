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
        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
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

        // 5. Cerrar Fusion
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
            yield return runner.Shutdown();

        yield return new WaitForSeconds(0.3f);

        // 6. Cargar MainMenu
        SceneManager.LoadScene("MainMenu");
    }
}
