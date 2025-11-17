using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class JuegoUI : MonoBehaviour
{
    // Singleton sencillo para acceder desde otros scripts.
    public static JuegoUI Instance { get; private set; }

    [Header("Vida")]
    public TextMeshProUGUI txtHealth;   // Txt_Health
    public Image imgHeart;              // Img_Heart (por si luego la usas)

    [Header("Mensajes")]
    public GameObject panelMessage;     // Panel_Message
    public TextMeshProUGUI txtMessage;  // Txt_Message
    public float defaultMessageTime = 2f;

    float messageTimer;

    [Header("Pausa")]
    public GameObject panelPause;       // Panel_Pause

    [Header("Nombres de escenas")]
    public string loginSceneName = "LoginRegistro";
    public string mainMenuSceneName = "MenuPrincipal";

    bool isPaused;

    void Awake()
    {
        // Configurar instancia única
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Estado inicial de paneles
        if (panelMessage) panelMessage.SetActive(false);
        if (panelPause) panelPause.SetActive(false);

        // Asegurar tiempo normal al entrar en la escena
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Contador de mensaje
        if (panelMessage && panelMessage.activeSelf)
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageTimer <= 0f)
                    panelMessage.SetActive(false);
            }
        }

        // ESC abre / cierra pausa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // -------------------- VIDA --------------------

    public void SetHealth(int current, int max)
    {
        if (!txtHealth) return;

        current = Mathf.Max(0, current);
        max = Mathf.Max(1, max);

        txtHealth.text = current + " / " + max;
    }

    // -------------------- MENSAJES --------------------

    public void ShowMessage(string msg, float time = -1f)
    {
        if (!panelMessage || !txtMessage) return;

        panelMessage.SetActive(true);
        txtMessage.text = msg;

        if (time <= 0f)
            time = defaultMessageTime;

        messageTimer = time;
    }

    // -------------------- PAUSA --------------------

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (panelPause)
            panelPause.SetActive(isPaused);

        // Congelar / reanudar el tiempo del juego
        Time.timeScale = isPaused ? 0f : 1f;
    }

    // Llamado por el botón "Continuar"
    public void OnContinueButton()
    {
        if (isPaused)
            TogglePause();
    }

    // Llamado por el botón "Guardar partida"
    public void OnSaveButton()
    {
        // Aquí luego meterás tu guardado real (Firebase, etc.)
        Debug.Log("Guardar partida (TODO)");

        // Pequeño feedback visual
        ShowMessage("PARTIDA GUARDADA");
    }

    // Llamado por el botón "Menú principal"
    public void OnMenuPrincipalButton()
    {
        Time.timeScale = 1f; // por si acaso salimos en pausa

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("mainMenuSceneName no está asignado en JuegoUI.");
        }
    }

    // Llamado por el botón "Volver al login"
    public void OnLoginButton()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(loginSceneName))
        {
            SceneManager.LoadScene(loginSceneName);
        }
        else
        {
            Debug.LogWarning("loginSceneName no está asignado en JuegoUI.");
        }
    }
}
