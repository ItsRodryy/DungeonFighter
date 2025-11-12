using UnityEngine;

namespace DungeonFighter.Combat
{
    public class EnemyMeleeDamage : MonoBehaviour
    {
        public int damage = 1;
        bool didHitThisSwing;

        void OnEnable() { didHitThisSwing = false; }
        void OnDisable() { didHitThisSwing = false; }

        void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
        void OnTriggerStay2D(Collider2D other) { TryHit(other); }

        void TryHit(Collider2D other)
        {
            if (didHitThisSwing) return;
            if (!other.CompareTag("Player")) return;

            var hp = other.GetComponentInParent<PlayerHealth>();
            if (hp != null)
            {
                hp.TakeDamage(damage, transform.position);
                didHitThisSwing = true;        // 1 golpe por ventana
            }
        }
    }
}
