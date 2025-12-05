using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonFighter.Combat
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour
    {
        // Propiedad pública de solo lectura para saber si el jugador está muerto.
        public bool IsDead => isDead;

        // Vida máxima del jugador.
        public int maxHP = 10;

        // Vida actual.
        int hp;

        // Flag para marcar que el jugador ya está muerto.
        bool isDead;

        public int HPActual => hp;

        // Referencias a componentes.
        Animator anim;
        Rigidbody2D rb;

        // Todos los colliders del jugador (incluidos hijos).
        Collider2D[] cols;

        // Scripts de movimiento y ataque del jugador, para desactivarlos al morir.
        MonoBehaviour move;   // Esperado: PlayerController2D.
        MonoBehaviour attack; // Esperado: PlayerAttack2D.

        [Header("Game Over")]
        [SerializeField] float gameOverDelay = 1.0f;   // segundos de espera antes de cargar GameOver
        bool gameOverLoading;

        void Awake()
        {
            // Inicializamos la vida al máximo.
            hp = maxHP;

            // Cacheo de componentes.
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            cols = GetComponentsInChildren<Collider2D>(true);

            // Asignación explícita a los componentes concretos (si existen).
            move = GetComponent<PlayerController2D>();
            attack = GetComponent<PlayerAttack2D>();
        }

        void Start()
        {
            // Actualizar HUD de vida al empezar (si hay GameUI en la escena).
            if (JuegoUI.Instance != null)
            {
                JuegoUI.Instance.SetHealth(hp, maxHP);
            }
        }

        // Llamado cuando el jugador recibe daño.
        public void TakeDamage(int dmg, Vector2 fromWorldPos)
        {
            // Si ya está muerto, ignoramos.
            if (isDead) return;

            // No permitimos daño menor a 1.
            dmg = Mathf.Max(1, dmg);

            // Restar vida y clamp a 0.
            hp = Mathf.Max(0, hp - dmg);

            // UI: actualizar HUD de vida.
            if (JuegoUI.Instance != null)
            {
                JuegoUI.Instance.SetHealth(hp, maxHP);
            }

            // Calcular vector desde el golpe hasta el jugador para orientar el Hurt.
            Vector2 delta = (Vector2)transform.position - fromWorldPos;

            // Determinar eje dominante para saber si ha sido más lateral o vertical.
            Vector2 face = (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                ? new Vector2(Mathf.Sign(delta.x), 0f)
                : new Vector2(0f, Mathf.Sign(delta.y));

            // Mandar dirección al Animator y disparar trigger "Hurt".
            anim.SetFloat("MoveX", face.x);
            anim.SetFloat("MoveY", face.y);
            anim.SetTrigger("Hurt");

            // Log de depuración.
            Debug.Log($"Enemigo ataca a jugador, -{dmg} de vida, vida restante: {hp}");

            // Si la vida llega a 0, procesamos la muerte.
            if (hp <= 0)
            {
                isDead = true;

                // Disparar animación de muerte.
                anim.SetTrigger("Die");

                // Desactivar scripts de movimiento y ataque si existen.
                if (move) move.enabled = false;
                if (attack) attack.enabled = false;

                // Frenar el rigidbody.
                if (rb) rb.linearVelocity = Vector2.zero;

                // Desactivar colliders para no recibir más golpes.
                foreach (var c in cols)
                {
                    c.enabled = false;
                }

                // Lanzar escena de Game Over tras un pequeño delay.
                if (!gameOverLoading)
                {
                    gameOverLoading = true;
                    StartCoroutine(LoadGameOverAfterDelay());
                }
            }
        }

        System.Collections.IEnumerator LoadGameOverAfterDelay()
        {
            // Espera a que acabe (más o menos) la animación de muerte.
            yield return new WaitForSeconds(gameOverDelay);

            // Cargar escena GameOver (asegúrate de que se llama EXACTO "GameOver").
            SceneManager.LoadScene("GameOver");
        }

        // Cura al jugador a tope de vida (usado por el cofre).
        public void HealToFull()
        {
            // Si el jugador está muerto, no tiene sentido curarlo aquí.
            if (isDead) return;

            // Poner vida actual al máximo.
            hp = maxHP;

            // UI: actualizar HUD de vida.
            if (JuegoUI.Instance != null)
            {
                JuegoUI.Instance.SetHealth(hp, maxHP);
            }

            // Log de depuración.
            Debug.Log($"Cofre cura al jugador: vida restaurada a {hp}/{maxHP}");
        }
    }
}
