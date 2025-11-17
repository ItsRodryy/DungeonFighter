using UnityEngine;
using DungeonFighter.Combat;   // Para EnemyHealth

public class RoomChallengeController : MonoBehaviour
{
    // Enemigos de ESTA sala (los dos goblins en la escena).
    public EnemyHealth[] enemies;

    // Cofre que se desbloquea al limpiar la sala.
    public ChestHealFullHP chest;

    // True cuando ya se ha completado el desafío de la sala.
    bool completed;

    void Update()
    {
        // Si ya se completó una vez, no hacemos nada más.
        if (completed) return;

        // Contamos cuántos enemigos siguen vivos.
        int vivos = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            // Si EnemyHealth todavía existe, es que ese enemigo sigue vivo.
            if (enemies[i] != null)
            {
                vivos++;
            }
        }

        // Si aún queda algún enemigo vivo, salimos.
        if (vivos > 0) return;

        // A partir de aquí, sabemos que la sala está limpia.
        completed = true;

        Debug.Log($"RoomChallengeController: sala limpia, enemigos vivos = {vivos}. Desbloqueando cofre...");

        // Desbloqueamos el cofre, si hay.
        if (chest)
        {
            chest.Unlock();
        }
        else
        {
            Debug.LogWarning("RoomChallengeController: no hay cofre asignado en el Inspector.");
        }
    }
}
