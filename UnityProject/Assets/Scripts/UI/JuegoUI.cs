using UnityEngine;
using System.Collections.Generic;
using DungeonFighter.Combat;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
            if (GestorDeAudio.I != null)
                GestorDeAudio.I.ReproducirUIClick();

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
    public async void OnSaveButton()
    {
        // Feedback rápido
        ShowMessage("Guardando partida...");

        try
        {
            // Buscamos el servicio de guardado
            var gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
            if (gameSave == null ||
                string.IsNullOrEmpty(gameSave.Uid) ||
                string.IsNullOrEmpty(gameSave.IdToken))
            {
                ShowMessage("No hay usuario logueado.");
                Debug.LogWarning("OnSaveButton: GameSaveServicio o sesión nula.");
                return;
            }

            // Buscamos al jugador
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO == null)
            {
                ShowMessage("No encuentro al jugador.");
                Debug.LogWarning("OnSaveButton: no se ha encontrado GameObject con tag Player.");
                return;
            }

            var hpComp = playerGO.GetComponent<PlayerHealth>();
            if (hpComp == null)
            {
                ShowMessage("Jugador sin PlayerHealth.");
                Debug.LogWarning("OnSaveButton: el jugador no tiene PlayerHealth.");
                return;
            }

            var pos = playerGO.transform.position;
            var escena = SceneManager.GetActiveScene().name;

            // Montamos el objeto PartidaGuardada
            var partida = new FirestoreCliente.PartidaGuardada
            {
                nombrePartida = "Partida 1", // siempre la misma => UNA partida por usuario

                datosJugador = new FirestoreCliente.DatosJugador
                {
                    vida = hpComp.CurrentHP,
                    vidaMaxima = hpComp.maxHP,
                    posX = pos.x,
                    posY = pos.y,
                    nombreEscena = escena
                },

                datosInventario = new FirestoreCliente.DatosInventario
                {
                    monedas = 0,
                    llaves = 0,
                    pociones = 0
                },

                estadoMundo = new FirestoreCliente.EstadoMundo
                {
                    enemigosEliminados = new List<string>(),
                    cofresAbiertos = new List<string>()
                }
            };

            // Guardar en Firestore en /partidasGuardadas/{uid}
            await gameSave.GuardarAsync(partida);

            ShowMessage("Partida guardada.");
            Debug.Log("Partida guardada correctamente en Firestore.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error guardando partida: " + ex);
            ShowMessage("Error al guardar partida.");
        }
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
