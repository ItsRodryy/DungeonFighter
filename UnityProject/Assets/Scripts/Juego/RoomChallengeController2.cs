using UnityEngine;
using DungeonFighter.Combat;   // donde está EnemyHealth

public class RoomChallengeController2 : MonoBehaviour
{
    public EnemyHealth[] enemies;  // goblins de ESTA sala
    public DungeonGate gate;       // la puerta de barrotes

    bool completed;

    void Update()
    {
        if (completed) return;

        bool allDead = true;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                allDead = false;
                break;
            }
        }

        if (!allDead) return;

        completed = true;

        if (gate)
        {
            Debug.Log("Room2: sala limpia, abriendo puerta.");
            gate.Open();
        }
    }
}
