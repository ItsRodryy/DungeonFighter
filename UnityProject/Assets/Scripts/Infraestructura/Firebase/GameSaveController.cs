using DungeonFighter.Combat;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Preparamos los datos de la partida actual y se los pasamos al GameSaveServicio
// La idea es separar:
// - GameSaveController coge el estado del juego (vida, posición, escena)
// - GameSaveServicio habla con Firestore y guardar/cargar
public class GameSaveController : MonoBehaviour
{
    [Header("Servicios")]
    // Servicio de guardado
    public GameSaveServicio gameSave;

    // Referencia a la vida del jugador
    public PlayerHealth playerHealth;

    [Header("Config")]
    // Nombre de la partida que se guardará (de momento 1 por jugador)
    public string nombrePartida = "Partida 1";

    private void Awake()
    {
        // Si no se asigna en el Inspector, buscamos automáticamente el servicio en la escena
        if (!gameSave)
            gameSave = Object.FindFirstObjectByType<GameSaveServicio>();

        // Igual que con PlayerHealth, buscamos el componente del jugador para obtener la vida y la posición.
        if (!playerHealth)
            playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
    }

    // Llamamos desde el botón Guardar (menú de pausa)
    public async void GuardarPartida()
    {
        // Comprobamos que exista el servicio de guardado en la escena
        if (gameSave == null)
        {
            Debug.LogError("GameSaveController: no hay GameSaveServicio en la escena.");
            return;
        }

        // Comprobamos que exista el componente de vida del jugador (si no, no podemos guardar nada)
        if (playerHealth == null)
        {
            Debug.LogError("GameSaveController: no hay PlayerHealth en la escena.");
            return;
        }

        // Posición actual del jugador, para reaparecer al cargar la partida
        var pos = playerHealth.transform.position;

        // Datos del jugador
        var datosJugador = new FirestoreCliente.DatosJugador
        {
            vida = playerHealth.CurrentHP,
            vidaMaxima = playerHealth.maxHP,
            posX = pos.x,
            posY = pos.y,
            nombreEscena = SceneManager.GetActiveScene().name
        };

        // Datos de inventario (de momento no está implementado)
        var datosInventario = new FirestoreCliente.DatosInventario
        {
            monedas = 0,
            llaves = 0,
            pociones = 0
        };

        // Estado del mundo
        var estadoMundo = new FirestoreCliente.EstadoMundo
        {
            enemigosEliminados = new List<string>(),
            cofresAbiertos = new List<string>()
        };

        // Objeto final de la partida al completo cuando ganamos el juego
        var partida = new FirestoreCliente.PartidaGuardada
        {
            nombrePartida = nombrePartida,
            datosJugador = datosJugador,
            datosInventario = datosInventario,
            estadoMundo = estadoMundo
        };

        // Guardamos la partida en Firestore
        try
        {
            await gameSave.GuardarAsync(partida);
            Debug.Log("Partida guardada correctamente.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error guardando partida: " + ex.Message);
        }
    }
}
