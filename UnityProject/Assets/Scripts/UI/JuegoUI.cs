using UnityEngine;
using System.Collections.Generic;
using DungeonFighter.Combat;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class JuegoUI : MonoBehaviour
{
    // Usamos un singleton sencillo para poder acceder a la UI desde otros scripts
    public static JuegoUI Instance { get; private set; }

    [Header("Vida")]
    public TextMeshProUGUI txtHealth;   // Txt_Health
    public Image imgHeart;              // Img_Heart por si luego la usamos

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
        // Nos aseguramos de que solo exista una instancia de esta UI en la escena
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Dejamos los paneles en su estado inicial
        if (panelMessage) panelMessage.SetActive(false);
        if (panelPause) panelPause.SetActive(false);

        // Nos aseguramos de entrar con el tiempo normal por si venimos de una pausa
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Controlamos el temporizador del panel de mensajes aunque el juego esté en pausa
        if (panelMessage && panelMessage.activeSelf)
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageTimer <= 0f)
                    panelMessage.SetActive(false);
            }
        }

        // Detectamos ESC para abrir o cerrar el menú de pausa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();

            // Reproducimos un sonido de click si tenemos gestor de audio
            if (GestorDeAudio.I != null)
                GestorDeAudio.I.ReproducirUIClick();
        }
    }

    // Actualizamos el texto de vida en pantalla con el formato actual y máximo
    public void SetHealth(int current, int max)
    {
        // Si no tenemos referencia al texto no hacemos nada
        if (!txtHealth) return;

        // Nos aseguramos de que no existan valores raros
        current = Mathf.Max(0, current);
        max = Mathf.Max(1, max);

        // Pintamos el texto en el HUD
        txtHealth.text = current + " / " + max;
    }

    // Mostramos un mensaje temporal en pantalla durante X segundos
    public void ShowMessage(string msg, float time = -1f)
    {
        // Si faltan referencias no podemos mostrar nada
        if (!panelMessage || !txtMessage) return;

        // Activamos el panel y escribimos el mensaje
        panelMessage.SetActive(true);
        txtMessage.text = msg;

        // Si no se pasa tiempo usamos el valor por defecto
        if (time <= 0f)
            time = defaultMessageTime;

        // Reiniciamos el temporizador
        messageTimer = time;
    }

    // Alternamos el estado de pausa y congelamos o reanudamos el tiempo del juego
    public void TogglePause()
    {
        // Invertimos el estado actual
        isPaused = !isPaused;

        // Activamos o desactivamos el panel de pausa
        if (panelPause)
            panelPause.SetActive(isPaused);

        // Cambiamos el timeScale para congelar el gameplay
        Time.timeScale = isPaused ? 0f : 1f;
    }

    // Botón Continuar del menú de pausa
    public void OnContinueButton()
    {
        // Si estamos en pausa la quitamos
        if (isPaused)
            TogglePause();
    }

    // Botón Guardar del menú de pausa
    public async void OnSaveButton()
    {
        // Mostramos feedback inmediato para que el jugador sepa que hemos recibido el click
        ShowMessage("Guardando partida...");

        try
        {
            // Buscamos el servicio de guardado y comprobamos que exista sesión válida
            var gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
            if (gameSave == null ||
                string.IsNullOrEmpty(gameSave.Uid) ||
                string.IsNullOrEmpty(gameSave.IdToken))
            {
                ShowMessage("No hay usuario logueado.");
                Debug.LogWarning("OnSaveButton: GameSaveServicio o sesión nula.");
                return;
            }

            // Buscamos el objeto del jugador por tag para sacar posición y vida
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO == null)
            {
                ShowMessage("No encuentro al jugador.");
                Debug.LogWarning("OnSaveButton: no se ha encontrado GameObject con tag Player.");
                return;
            }

            // Sacamos el componente PlayerHealth del jugador para guardar la vida
            var hpComp = playerGO.GetComponent<PlayerHealth>();
            if (hpComp == null)
            {
                ShowMessage("Jugador sin PlayerHealth.");
                Debug.LogWarning("OnSaveButton: el jugador no tiene PlayerHealth.");
                return;
            }

            // Leemos posición y escena actual para reaparecer al cargar
            var pos = playerGO.transform.position;
            var escena = SceneManager.GetActiveScene().name;

            // Montamos el objeto PartidaGuardada con el estado actual
            var partida = new FirestoreCliente.PartidaGuardada
            {
                // Guardamos siempre el mismo nombre porque es una partida por usuario
                nombrePartida = "Partida 1",

                datosJugador = new FirestoreCliente.DatosJugador
                {
                    vida = hpComp.CurrentHP,
                    vidaMaxima = hpComp.maxHP,
                    posX = pos.x,
                    posY = pos.y,
                    nombreEscena = escena
                },

                // De momento el inventario está fijo porque aún no lo hemos conectado al sistema real
                datosInventario = new FirestoreCliente.DatosInventario
                {
                    monedas = 0,
                    llaves = 0,
                    pociones = 0
                },

                // De momento el estado del mundo va vacío porque aún no estamos guardando IDs reales
                estadoMundo = new FirestoreCliente.EstadoMundo
                {
                    enemigosEliminados = new List<string>(),
                    cofresAbiertos = new List<string>()
                }
            };

            // Guardamos en Firestore dentro de /partidasGuardadas/{uid}
            await gameSave.GuardarAsync(partida);

            // Confirmamos por UI y por consola
            ShowMessage("Partida guardada.");
            Debug.Log("Partida guardada correctamente en Firestore.");
        }
        catch (System.Exception ex)
        {
            // Si revienta algo lo registramos y mostramos un mensaje genérico
            Debug.LogError("Error guardando partida: " + ex);
            ShowMessage("Error al guardar partida.");
        }
    }

    // Botón Menú principal del menú de pausa
    public void OnMenuPrincipalButton()
    {
        // Dejamos el timeScale a 1 por si salimos estando en pausa
        Time.timeScale = 1f;

        // Si el nombre de escena está asignado cargamos el menú principal
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("mainMenuSceneName no está asignado en JuegoUI.");
        }
    }

    // Botón Volver al login del menú de pausa
    public void OnLoginButton()
    {
        // Dejamos el timeScale a 1 por si salimos estando en pausa
        Time.timeScale = 1f;

        // Si el nombre de escena está asignado cargamos la escena de login
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
