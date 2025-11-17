using UnityEngine;

namespace DungeonFighter.Combat
{
    public class EnemyMeleeDamage : MonoBehaviour
    {
        // Daño que hace el enemigo con un golpe cuerpo a cuerpo.
        public int damage = 1;

        // Indica si estamos dentro de la "ventana de daño" del swing.
        bool armed;

        // Indica si en este swing ya hemos golpeado una vez.
        bool didHitThisSwing;

        // Llamado desde animación al comenzar el swing.
        public void BeginSwing()
        {
            armed = true;
            didHitThisSwing = false;
        }

        // Llamado desde animación al terminar el swing.
        public void EndSwing()
        {
            armed = false;
            didHitThisSwing = false;
        }

        // Cuando entra un collider dentro de la hitbox.
        void OnTriggerEnter2D(Collider2D other)
        {
            TryHit(other);
        }

        // Mientras se queda dentro del trigger, seguimos intentando (por si entra en mitad del frame).
        void OnTriggerStay2D(Collider2D other)
        {
            TryHit(other);
        }

        // Lógica de comprobar si podemos golpear al jugador.
        void TryHit(Collider2D other)
        {
            // Solo dentro de la ventana de daño.
            if (!armed) return;

            // Solo un golpe por swing.
            if (didHitThisSwing) return;

            // Solo afectar a objetos con tag "Player".
            if (!other.CompareTag("Player")) return;

            // Buscamos PlayerHealth en el padre (por si el trigger está en un hijo).
            var hp = other.GetComponentInParent<PlayerHealth>();
            if (hp)
            {
                // Aplicamos daño al jugador.
                hp.TakeDamage(damage, transform.position);

                // Marcamos que ya hemos golpeado en este swing.
                didHitThisSwing = true;
            }
        }
    }
}
