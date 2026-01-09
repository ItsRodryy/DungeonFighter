using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonFighter.Combat
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour
    {
        public bool IsDead => isDead;

        public int maxHP = 10;

        int hp;

        bool isDead;

        public int CurrentHP => hp;

        Animator anim;
        Rigidbody2D rb;

        Collider2D[] cols;

        MonoBehaviour move;
        MonoBehaviour attack;

        [Header("Game Over")]
        [SerializeField] float gameOverDelay = 1.0f;
        bool gameOverLoading;

        void Awake()
        {
            // Inicializamos la vida al máximo
            hp = maxHP;

            // Cacheamos componentes para usarlos rápido
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            cols = GetComponentsInChildren<Collider2D>(true);

            // Cogemos scripts concretos si existen para poder desactivarlos al morir
            move = GetComponent<PlayerController2D>();
            attack = GetComponent<PlayerAttack2D>();
        }

        void Start()
        {
            // Actualizamos HUD al empezar si tenemos UI
            if (JuegoUI.Instance != null)
            {
                JuegoUI.Instance.SetHealth(hp, maxHP);
            }
        }

        public void TakeDamage(int dmg, Vector2 fromWorldPos)
        {
            // Si ya estamos muertos ignoramos
            if (isDead) return;

            // Nos aseguramos de que el daño mínimo sea 1
            dmg = Mathf.Max(1, dmg);

            // Restamos vida y la dejamos como mínimo en 0
            hp = Mathf.Max(0, hp - dmg);

            // Actualizamos HUD si tenemos UI
            if (JuegoUI.Instance != null)
            {
                JuegoUI.Instance.SetHealth(hp, maxHP);
            }

            // Calculamos desde dónde viene el golpe para orientar la animación de hurt
            Vector2 delta = (Vector2)transform.position - fromWorldPos;

            // Elegimos eje dominante para orientar a 4 direcciones
            Vector2 face = (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                ? new Vector2(Mathf.Sign(delta.x), 0f)
                : new Vector2(0f, Mathf.Sign(delta.y));

            // Mandamos dirección al animator y disparamos hurt
            anim.SetFloat("MoveX", face.x);
            anim.SetFloat("MoveY", face.y);
            anim.SetTrigger("Hurt");

            Debug.Log($"Enemigo ataca a jugador -{dmg} vida restante {hp}");

            if (hp <= 0)
            {
                isDead = true;

                // Disparamos animación de muerte
                anim.SetTrigger("Die");

                // Reproducimos sonido de muerte una sola vez si tenemos gestor
                if (GestorDeAudio.I != null)
                    GestorDeAudio.I.ReproducirDeadUnaVez();

                // Desactivamos movimiento y ataque si existen
                if (move) move.enabled = false;
                if (attack) attack.enabled = false;

                // Paramos el rigidbody
                if (rb) rb.linearVelocity = Vector2.zero;

                // Desactivamos colliders para evitar más interacciones
                foreach (var c in cols)
                {
                    c.enabled = false;
                }

                // Cargamos GameOver con un pequeño delay
                if (!gameOverLoading)
                {
                    gameOverLoading = true;
                    StartCoroutine(LoadGameOverAfterDelay());
                }
            }
        }

        System.Collections.IEnumerator LoadGameOverAfterDelay()
        {
            // Esperamos el tiempo configurado
            yield return new WaitForSeconds(gameOverDelay);

            // Cargamos la escena GameOver
            SceneManager.LoadScene("GameOver");
        }

        public void HealToFull()
        {
            // Si estamos muertos no curamos aquí
            if (isDead) return;

            // Ponemos vida al máximo
            hp = maxHP;

            // Actualizamos HUD si tenemos UI
            if (JuegoUI.Instance != null)
            {
                JuegoUI.Instance.SetHealth(hp, maxHP);
            }

            Debug.Log($"Cofre cura al jugador vida restaurada {hp}/{maxHP}");
        }

        public void AplicarCarga(int vidaCargada, int vidaMaxCargada)
        {
            // Si estamos muertos no aplicamos carga
            if (isDead) return;

            // Ajustamos vida máxima y clamp de vida actual
            maxHP = Mathf.Max(1, vidaMaxCargada);
            hp = Mathf.Clamp(vidaCargada, 0, maxHP);

            // Actualizamos HUD si tenemos UI
            if (JuegoUI.Instance != null)
                JuegoUI.Instance.SetHealth(hp, maxHP);
        }
    }
}
