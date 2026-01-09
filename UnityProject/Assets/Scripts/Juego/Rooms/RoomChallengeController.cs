using UnityEngine;
using DungeonFighter.Combat;

// Desbloqueamos un cofre cuando limpiamos la sala de enemigos
public class RoomChallengeController : MonoBehaviour
{
    public EnemyHealth[] enemies;
    public ChestHealFullHP chest;

    bool completed;

    void Start()
    {
        // Desactivamos el script del cofre mientras esté bloqueado
        if (chest) chest.enabled = false;
    }

    void Update()
    {
        // Si ya se completó no hacemos nada más
        if (completed) return;

        // Contamos enemigos vivos comprobando referencias no nulas
        int vivos = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                vivos++;
            }
        }

        // Si queda alguno vivo salimos
        if (vivos > 0) return;

        // Marcamos completado y desbloqueamos el cofre
        completed = true;

        Debug.Log($"RoomChallengeController sala limpia enemigos vivos {vivos} desbloqueando cofre");

        if (chest)
        {
            chest.Unlock();
        }
        else
        {
            Debug.LogWarning("RoomChallengeController no hay cofre asignado en el Inspector");
        }

        if (chest) chest.enabled = true;
    }
}
