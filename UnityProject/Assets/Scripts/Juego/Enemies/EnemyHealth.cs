using UnityEngine;

namespace DungeonFighter.Combat
{
    public class EnemyHealth : MonoBehaviour
    {
        public int maxHP = 3;
        int hp;
        Animator anim;

        void Awake()
        {
            hp = maxHP;
            anim = GetComponent<Animator>();
        }

        public void TakeDamage(int dmg)
        {
            hp -= Mathf.Max(1, dmg);
            // anim?.SetTrigger("Hurt");
            if (hp <= 0)
            {
                // anim?.SetTrigger("Die");
                Destroy(gameObject, 0.1f);
            }
        }
    }
}
