using UnityEngine;
using DungeonFighter.Combat;

// Abrimos la puerta cuando todos los enemigos de la sala han desaparecido
public class RoomChallengeController2 : MonoBehaviour
{
    public EnemyHealth[] enemies;
    public DungeonGate gate;

    bool completed;

    void Update()
    {
        // Si ya lo completamos no repetimos
        if (completed) return;

        // Comprobamos si queda algún enemigo vivo
        bool allDead = true;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                allDead = false;
                break;
            }
        }

        // Si todavía hay enemigos salimos
        if (!allDead) return;

        // Marcamos como completado y abrimos la puerta
        completed = true;

        if (gate)
        {
            Debug.Log("Room2 sala limpia abriendo puerta");
            gate.Open();
        }
    }
}
