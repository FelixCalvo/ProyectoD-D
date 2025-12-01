using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;


public class PanelFadeInOut : MonoBehaviour
{

    [Header("Fade Settings")]
    public Image fadePanel;
    public float fadeDuration = 2f;

    void Awake()
    {
        if (fadePanel != null)
        {
            Color color = fadePanel.color;
            color.a = 1f;
            fadePanel.color = color;
        }
    }

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    public IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            Color color = fadePanel.color;
            color.a = alpha;
            fadePanel.color = color;
            yield return null;
        }
        Color finalColor = fadePanel.color;
        finalColor.a = 0f;
        fadePanel.color = finalColor;
    }

    

}
