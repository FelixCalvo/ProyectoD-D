using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Visual Panels")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private GameObject nameGamePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject clientPanel;
    [SerializeField] private GameObject contenedorPartidas;


    [Header("Name Game Input")]
    [SerializeField] private TMP_InputField inputFieldNameGame;


    [Header("Botones")]

    [SerializeField] private Button buttonNewGame;
    [SerializeField] private Button buttonServer;
    [SerializeField] private Button buttonNameGame;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonCreditos;

    [SerializeField] private Button buttonToDesktop;


    [Header("Fade Settings")]

    [SerializeField] private float fadeDuration = 2.0f; // duración del fade out y música
    [SerializeField] private float fadeInDuration = 3.0f; // duración del fade in al iniciar

    void Awake()
    {
        // CRÍTICO: Establecer pantalla completamente negra ANTES del primer frame
        if (fadePanel != null)
        {
            Color color = fadePanel.color;
            color.a = 1f;
            fadePanel.color = color;
            //fadePanel.raycastTarget = true; // Bloquear clics durante el fade in
        }
    }

    void Start()
    {
        //fadePanel.raycastTarget = false;
        // Iniciar el fade in después de que la escena esté completamente cargada
        StartCoroutine(FadeInRoutine());

        if (buttonNewGame != null)
            buttonNewGame.onClick.AddListener(() => StartCoroutine(LoadSceneWithFade("NewGame")));

        if (buttonCreditos != null)
            buttonCreditos.onClick.AddListener(() => StartCoroutine(LoadSceneWithFade("Creditos")));

        if (buttonClient != null)
            buttonClient.onClick.AddListener(() =>
            {
                clientPanel.SetActive(true);
                mainMenuPanel.SetActive(false);
            });

        //StartCoroutine(LoadSceneWithFade("ListGames")));

        if (buttonNameGame != null)
            buttonNameGame.onClick.AddListener(() => OnCrearPartida());

        if (buttonServer != null)
            buttonServer.onClick.AddListener(() =>
            {
                nameGamePanel.SetActive(true);
                mainMenuPanel.SetActive(false);
            });

        if (buttonToDesktop != null)
            buttonToDesktop.onClick.AddListener(() => ExitToDesktop());

    }

    public void ExitToDesktop()
    {
        // Salir del juego compilado
        Application.Quit();

        // Parar el modo Play si estamos en el Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator FadeInRoutine()
    {
        // Pausa para asegurar que la escena está completamente cargada
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        // Reactivar raycast para bloquear clics durante el fade out
        if (fadePanel != null)
            fadePanel.raycastTarget = true;

        // Iniciar fade out visual
        yield return StartCoroutine(FadeOut());

        // Pequeña pausa para que se vea el negro completo
        yield return new WaitForSeconds(1.5f);

        // Cargar escena después de que el fade esté completo
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;

        // De negro opaco (1) a transparente (0)
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeInDuration);

            Color color = fadePanel.color;
            color.a = alpha;
            fadePanel.color = color;

            yield return null;
        }

        // Asegurar que termina transparente
        Color finalColor = fadePanel.color;
        finalColor.a = 0f;
        fadePanel.color = finalColor;

        // Desactivar raycast para permitir clics en botones
        if (fadePanel != null)
            fadePanel.raycastTarget = false;
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;

        // De transparente (0) a negro opaco (1)
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);

            Color color = fadePanel.color;
            color.a = alpha;
            fadePanel.color = color;

            yield return null;
        }

        // Asegurar que termina en negro completo
        Color finalColor = fadePanel.color;
        finalColor.a = 1f;
        fadePanel.color = finalColor;
    }
    
    private async void OnCrearPartida()
    {
        string nombre = inputFieldNameGame.text;
        
        if (string.IsNullOrEmpty(nombre))
        {
            Debug.LogWarning("⚠ Debes introducir un nombre para la partida");
            return;
        }
        
        // Crear la partida de red ANTES de ir a Players
        await NetworkSessionStarter.CrearPartidaYCargarPlayers(nombre);
    }
}
