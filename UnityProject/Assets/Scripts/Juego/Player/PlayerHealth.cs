using UnityEngine;
using System.Collections;

namespace DungeonFighter.Combat
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour
    {
        public int maxHP = 10;
        int hp;
        bool isDead;

        Animator anim;
        Rigidbody2D rb;
        Collider2D[] cols;
        MonoBehaviour move;   // PlayerController2D
        MonoBehaviour attack; // PlayerAttack2D

        void Awake()
        {
            hp = maxHP;
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            cols = GetComponentsInChildren<Collider2D>(true);

            move = GetComponent<MonoBehaviour>(); // luego lo sustituimos abajo
            attack = GetComponent<MonoBehaviour>(); // idem
            // Si existen, asigna explícito para evitar ambigüedad:
            move = GetComponent<PlayerController2D>();
            attack = GetComponent<PlayerAttack2D>();
        }

        public void TakeDamage(int dmg, Vector2 fromWorldPos)
        {
            if (isDead) return;

            dmg = Mathf.Max(1, dmg);
            hp = Mathf.Max(0, hp - dmg);

            // Direccionalidad del Hurt
            Vector2 delta = (Vector2)transform.position - fromWorldPos;
            Vector2 face = (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                          ? new Vector2(Mathf.Sign(delta.x), 0f)
                          : new Vector2(0f, Mathf.Sign(delta.y));
            anim.SetFloat("MoveX", face.x);
            anim.SetFloat("MoveY", face.y);
            anim.SetTrigger("Hurt");

            Debug.Log($"Enemigo ataca a jugador, -{dmg} de vida, vida restante: {hp}");

            if (hp <= 0)
            {
                isDead = true;
                anim.SetTrigger("Die");
                if (move) move.enabled = false;
                if (attack) attack.enabled = false;
                if (rb) rb.linearVelocity = Vector2.zero;
                // desactiva colisionadores para no recibir más golpes
                foreach (var c in cols) c.enabled = false;

                // no destruimos al jugador (puedes poner GameOver aquí si quieres)
                // StartCoroutine(GameOverRoutine());
            }
        }
    }
}
