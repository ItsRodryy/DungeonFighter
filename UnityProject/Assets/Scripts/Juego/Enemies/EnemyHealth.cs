using UnityEngine;

namespace DungeonFighter.Combat
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyHealth : MonoBehaviour
    {
        // Vida máxima del enemigo.
        public int maxHP = 3;

        // Vida actual.
        int hp;

        // Flag para marcar que ya está muerto.
        bool isDead;

        // Referencias a componentes.
        Animator anim;
        Rigidbody2D rb;

        // Todos los colliders (incluidos los hijos) para desactivarlos al morir.
        Collider2D[] cols;

        // IA de persecución del enemigo, para desactivarla al morir.
        EnemyChase2D ai;

        void Awake()
        {
            // Inicializamos vida al máximo.
            hp = maxHP;

            // Cacheo de componentes.
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            cols = GetComponentsInChildren<Collider2D>(true);
            ai = GetComponent<EnemyChase2D>();
        }

        // Llamada cuando el enemigo recibe daño.
        public void TakeDamage(int dmg, Vector2 fromWorldPos)
        {
            // Si ya está muerto, ignoramos.
            if (isDead) return;

            // Evitar daño menor a 1.
            dmg = Mathf.Max(1, dmg);

            // Restar vida y clamp a 0.
            hp = Mathf.Max(0, hp - dmg);

            // Calcular desde dónde viene el golpe para orientar el "Hurt".
            Vector2 delta = (Vector2)transform.position - fromWorldPos;

            // Determinar si el golpe viene más por X o por Y.
            Vector2 face = (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                ? new Vector2(Mathf.Sign(delta.x), 0f)
                : new Vector2(0f, Mathf.Sign(delta.y));

            // Mandar dirección al Animator y disparar el trigger "Hurt".
            anim.SetFloat("MoveX", face.x);
            anim.SetFloat("MoveY", face.y);
            anim.SetTrigger("Hurt");

            // Log para depuración.
            Debug.Log($"Jugador ataca a enemigo, -{dmg} de vida, vida restante: {hp}");

            // Si la vida llega a 0, muerte.
            if (hp <= 0)
            {
                isDead = true;

                // Animación de muerte.
                anim.SetTrigger("Die");

                // Desactivar IA si existe.
                if (ai) ai.enabled = false;

                // Frenar el rigidbody si existe.
                if (rb) rb.linearVelocity = Vector2.zero;

                // Desactivar todos los colliders (para no seguir recibiendo golpes).
                foreach (var c in cols)
                {
                    c.enabled = false;
                }

                // Destruir el enemigo tras un tiempo (da margen a la animación de muerte).
                Destroy(gameObject, 0.8f);
            }
        }
    }
}
