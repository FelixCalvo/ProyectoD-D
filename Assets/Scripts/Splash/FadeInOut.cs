using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FadeInOut : MonoBehaviour
{
    [SerializeField]
    private Image fadePanel;
    
    [SerializeField]
    private float fadeDuration = 3f;
    
    [SerializeField]
    private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    
    [SerializeField]
    private float delayBeforeFadeOut = 3f;

    void Awake()
    {
        // Asegurar que el panel inicia completamente opaco y negro
        if (fadePanel != null)
        {
            fadePanel.color = new Color(0f, 0f, 0f, 1f);
            fadePanel.enabled = true;
            fadePanel.raycastTarget = false; // Evitar que bloquee interacciones
        }
    }

    void Start()
    {
        // Iniciar secuencia completa: fade in, esperar, fade out
        StartCoroutine(FadeSequence());
    }
    
    // Secuencia completa de fade
    private IEnumerator FadeSequence()
    {
        yield return StartCoroutine(FadeIn());
        yield return new WaitForSeconds(delayBeforeFadeOut);
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene("MainMenu");
    }

    // Fade In - de negro opaco a transparente
    public IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            float curveValue = fadeCurve.Evaluate(t);
            float alpha = 1f - curveValue;
            
            fadePanel.color = new Color(0f, 0f, 0f, alpha);
            
            yield return null;
        }
        
        // Asegurar que termina completamente transparente
        fadePanel.color = new Color(0f, 0f, 0f, 0f);
    }

    // Fade Out - de transparente a negro opaco
    public IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            float curveValue = fadeCurve.Evaluate(t);
            float alpha = curveValue;
            
            fadePanel.color = new Color(0f, 0f, 0f, alpha);
            
            yield return null;
        }
        
        // Asegurar que termina completamente opaco
        fadePanel.color = new Color(0f, 0f, 0f, 1f);
    }

    // Método para llamar fade in desde otros scripts
    public void DoFadeIn()
    {
        StartCoroutine(FadeIn());
    }

    // Método para llamar fade out desde otros scripts
    public void DoFadeOut()
    {
        StartCoroutine(FadeOut());
    }
}
