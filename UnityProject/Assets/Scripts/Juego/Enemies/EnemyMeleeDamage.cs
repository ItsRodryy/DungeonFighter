using UnityEngine;

namespace DungeonFighter.Combat   // ← meto namespace para blindarte
{
    public class EnemyMeleeDamage : MonoBehaviour
    {
        public int damage = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // Conecta con tu vida del jugador cuando la tengas:
                Debug.Log($"Golpe al player: -{damage}");
                // other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            }
        }
    }
}
