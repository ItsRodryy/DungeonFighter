using UnityEngine;
using DungeonFighter.Combat;  // ← para ver EnemyHealth

namespace DungeonFighter.Combat
{
    public class PlayerMeleeDamage : MonoBehaviour
    {
        public int damage = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                var hp = other.GetComponentInParent<EnemyHealth>();
                if (hp) hp.TakeDamage(damage);
                else Debug.Log("Hit a Enemy sin EnemyHealth");
            }
        }
    }
}
