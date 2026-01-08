using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using DungeonFighter.Combat;

public class CargarPartidaEnJuego : MonoBehaviour
{
    [Tooltip("Si está activo, al entrar en Juego intenta cargar y aplicar la partida.")]
    public bool autoCargarAlEntrar = true;

    async void Start()
    {
        if (!autoCargarAlEntrar) return;

        // Solo tiene sentido en la escena Juego
        if (SceneManager.GetActiveScene().name.Trim().ToLower() != "juego")
            return;

        var gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
        if (gameSave == null || string.IsNullOrEmpty(gameSave.Uid) || string.IsNullOrEmpty(gameSave.IdToken))
            return;

        // Encuentra jugador
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (!playerGO) return;

        var hp = playerGO.GetComponent<PlayerHealth>();
        if (!hp) return;

        try
        {
            var p = await gameSave.CargarAsync();

            // Aplicar posición
            playerGO.transform.position = new Vector3(p.datosJugador.posX, p.datosJugador.posY, playerGO.transform.position.z);

            // Aplicar vida
            hp.AplicarCarga(p.datosJugador.vida, p.datosJugador.vidaMaxima);

            // UI
            if (JuegoUI.Instance != null)
                JuegoUI.Instance.ShowMessage("Partida cargada ✅");

        }
        catch
        {
            // Si no existe documento, Firestore devuelve error => ignoramos
            // (Ej: usuario sin partida guardada aún)
        }
    }
}
