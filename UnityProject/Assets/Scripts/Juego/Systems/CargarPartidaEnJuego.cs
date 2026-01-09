using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonFighter.Combat;

// Al entrar en Juego intentamos cargar y aplicar la partida guardada
public class CargarPartidaEnJuego : MonoBehaviour
{
    [Tooltip("Si está activo al entrar en Juego intentamos cargar y aplicar la partida")]
    public bool autoCargarAlEntrar = true;

    async void Start()
    {
        // Si desactivamos autocarga no hacemos nada
        if (!autoCargarAlEntrar) return;

        // Solo tiene sentido dentro de la escena Juego
        if (SceneManager.GetActiveScene().name.Trim().ToLower() != "juego")
            return;

        // Buscamos el servicio de guardado y comprobamos que haya sesión
        var gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
        if (gameSave == null || string.IsNullOrEmpty(gameSave.Uid) || string.IsNullOrEmpty(gameSave.IdToken))
            return;

        // Buscamos el jugador por tag
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (!playerGO) return;

        // Cogemos el componente de vida para aplicar vida cargada
        var hp = playerGO.GetComponent<PlayerHealth>();
        if (!hp) return;

        try
        {
            // Cargamos desde Firestore la partida del usuario
            var p = await gameSave.CargarAsync();

            // Aplicamos posición manteniendo la z actual
            playerGO.transform.position = new Vector3(
                p.datosJugador.posX,
                p.datosJugador.posY,
                playerGO.transform.position.z
            );

            // Aplicamos vida y vida máxima cargadas
            hp.AplicarCarga(p.datosJugador.vida, p.datosJugador.vidaMaxima);

            // Mostramos un mensaje si tenemos UI
            if (JuegoUI.Instance != null)
                JuegoUI.Instance.ShowMessage("Partida cargada");
        }
        catch
        {
            // Si no existe documento o falla la carga ignoramos y seguimos jugando normal
        }
    }
}
