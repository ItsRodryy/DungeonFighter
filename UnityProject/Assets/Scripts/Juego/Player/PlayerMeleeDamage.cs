using UnityEngine;

namespace DungeonFighter.Combat
{
    // Aplicamos daño cuando la hitbox del jugador toca un enemigo
    public class PlayerMeleeDamage : MonoBehaviour
    {
        public int damage = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Solo nos interesa si el otro objeto es un enemigo
            if (other.CompareTag("Enemy"))
            {
                // Buscamos EnemyHealth en el padre por si el collider está en un hijo
                var hp = other.GetComponentInParent<EnemyHealth>();

                // Si existe aplicamos daño
                if (hp)
                {
                    hp.TakeDamage(damage, transform.position);
                }
            }
        }
    }
}
