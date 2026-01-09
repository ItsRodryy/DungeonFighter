using UnityEngine;

namespace DungeonFighter.Combat
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyHealth : MonoBehaviour
    {
        public int maxHP = 3;

        int hp;

        bool isDead;

        Animator anim;
        Rigidbody2D rb;

        Collider2D[] cols;

        EnemyChase2D ai;

        void Awake()
        {
            // Inicializamos vida al máximo
            hp = maxHP;

            // Cacheamos componentes
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            cols = GetComponentsInChildren<Collider2D>(true);
            ai = GetComponent<EnemyChase2D>();
        }

        public void TakeDamage(int dmg, Vector2 fromWorldPos)
        {
            // Si ya está muerto ignoramos
            if (isDead) return;

            // Forzamos daño mínimo
            dmg = Mathf.Max(1, dmg);

            // Restamos vida y clamp a 0
            hp = Mathf.Max(0, hp - dmg);

            // Calculamos dirección del golpe para orientar hurt
            Vector2 delta = (Vector2)transform.position - fromWorldPos;

            Vector2 face = (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                ? new Vector2(Mathf.Sign(delta.x), 0f)
                : new Vector2(0f, Mathf.Sign(delta.y));

            // Mandamos dirección al animator y dispararnos hurt
            anim.SetFloat("MoveX", face.x);
            anim.SetFloat("MoveY", face.y);
            anim.SetTrigger("Hurt");

            Debug.Log($"Jugador ataca a enemigo -{dmg} vida restante {hp}");

            if (hp <= 0)
            {
                isDead = true;

                // Lanzamos animación de muerte
                anim.SetTrigger("Die");

                // Desactivamos IA para que deje de perseguir
                if (ai) ai.enabled = false;

                // Paramos rigidbody
                if (rb) rb.linearVelocity = Vector2.zero;

                // Desactivamos colliders para evitar más colisiones
                foreach (var c in cols)
                {
                    c.enabled = false;
                }

                // Destruimos tras un pequeño tiempo para que se vea la animación
                Destroy(gameObject, 0.8f);
            }
        }
    }
}
