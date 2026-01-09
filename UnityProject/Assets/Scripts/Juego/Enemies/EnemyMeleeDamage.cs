using UnityEngine;

namespace DungeonFighter.Combat
{
    // Aplicamos daño al jugador desde una hitbox del enemigo durante una ventana de ataque
    public class EnemyMeleeDamage : MonoBehaviour
    {
        public int damage = 1;

        bool armed;
        bool didHitThisSwing;

        public void BeginSwing()
        {
            // Activamos ventana de daño y permitimos un golpe
            armed = true;
            didHitThisSwing = false;
        }

        public void EndSwing()
        {
            // Desactivamos ventana y reiniciamos flags
            armed = false;
            didHitThisSwing = false;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryHit(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryHit(other);
        }

        void TryHit(Collider2D other)
        {
            // Solo pegamos si estamos armados
            if (!armed) return;

            // Solo pegamos una vez por swing
            if (didHitThisSwing) return;

            // Solo pegamos al jugador
            if (!other.CompareTag("Player")) return;

            // Buscamos PlayerHealth en el padre por si el collider está en un hijo
            var hp = other.GetComponentInParent<PlayerHealth>();
            if (hp)
            {
                // Aplicamos daño usando nuestra posición como origen
                hp.TakeDamage(damage, transform.position);

                // Reproducimos golpe enemigo si tenemos gestor
                if (GestorDeAudio.I != null)
                    GestorDeAudio.I.ReproducirGolpeEnemigo();

                // Marcamos que ya hemos golpeado en este swing
                didHitThisSwing = true;
            }
        }
    }
}
