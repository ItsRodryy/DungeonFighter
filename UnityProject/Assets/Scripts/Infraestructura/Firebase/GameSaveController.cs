using DungeonFighter.Combat;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSaveController : MonoBehaviour
{
    [Header("Servicios")]
    public GameSaveServicio gameSave;      // puedes dejarlo en None en el inspector
    public PlayerHealth playerHealth;

    [Header("Config")]
    public string nombrePartida = "Partida 1";

    private void Awake()
    {
        if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
        if (!playerHealth) playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
    }

    // Llamar desde el botón Guardar (menú de pausa)
    public async void GuardarPartida()
    {
        if (gameSave == null)
        {
            Debug.LogError("GameSaveController: no hay GameSaveServicio en la escena.");
            return;
        }
        if (playerHealth == null)
        {
            Debug.LogError("GameSaveController: no hay PlayerHealth en la escena.");
            return;
        }

        var pos = playerHealth.transform.position;

        var datosJugador = new FirestoreCliente.DatosJugador
        {
            vida = playerHealth.HPActual,
            vidaMaxima = playerHealth.maxHP,
            posX = pos.x,
            posY = pos.y,
            nombreEscena = SceneManager.GetActiveScene().name
        };

        var datosInventario = new FirestoreCliente.DatosInventario
        {
            monedas = 0,
            llaves = 0,
            pociones = 0
        };

        var estadoMundo = new FirestoreCliente.EstadoMundo
        {
            enemigosEliminados = new List<string>(),
            cofresAbiertos = new List<string>()
        };

        var partida = new FirestoreCliente.PartidaGuardada
        {
            nombrePartida = nombrePartida,
            datosJugador = datosJugador,
            datosInventario = datosInventario,
            estadoMundo = estadoMundo
        };

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
