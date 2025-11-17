using UnityEngine;

namespace DungeonFighter.Combat
{
    public class PlayerMeleeDamage : MonoBehaviour
    {
        // Daño que hace el golpe cuerpo a cuerpo del jugador.
        public int damage = 1;

        // Cuando la hitbox del jugador entra en contacto con otro collider.
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Solo nos interesa si el otro objeto tiene tag "Enemy".
            if (other.CompareTag("Enemy"))
            {
                // Buscamos EnemyHealth en el padre (el collider puede estar en un hijo).
                var hp = other.GetComponentInParent<EnemyHealth>();

                // Si tiene EnemyHealth, aplicamos el daño.
                if (hp)
                {
                    hp.TakeDamage(damage, transform.position);
                }
            }
        }
    }
}
