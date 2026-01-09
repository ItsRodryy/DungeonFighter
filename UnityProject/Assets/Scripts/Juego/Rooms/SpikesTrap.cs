using DungeonFighter.Combat;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class SpikesTrap : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int damage = 1;

    // Guardamos el jugador que está dentro del trigger si lo hay
    PlayerHealth playerInside;

    // Marcamos si la ventana de daño está activa cuando los pinchos están arriba
    bool damageWindowActive;

    // Evitamos golpear más de una vez en la misma subida
    bool alreadyHitThisWindow;

    Collider2D col;

    void Awake()
    {
        // Cogemos el collider y lo ponemos como trigger para detectar al jugador
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo reaccionamos al jugador
        if (!other.CompareTag("Player")) return;

        // Buscamos PlayerHealth en el padre por si el collider está en un hijo
        playerInside = other.GetComponentInParent<PlayerHealth>();
        if (playerInside == null) return;

        // Si entramos cuando la ventana ya está activa pegamos al momento
        if (damageWindowActive && !alreadyHitThisWindow)
        {
            HitPlayer();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Solo reaccionamos al jugador
        if (!other.CompareTag("Player")) return;

        // Si sale el mismo jugador limpiamos referencia
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp == playerInside)
        {
            playerInside = null;
        }
    }

    public void EnableDamage()
    {
        // Activamos ventana de daño y reiniciamos el flag de golpe
        damageWindowActive = true;
        alreadyHitThisWindow = false;

        // Si el jugador ya estaba encima cuando suben pegamos aquí
        if (playerInside != null && !alreadyHitThisWindow)
        {
            HitPlayer();
        }
    }

    public void DisableDamage()
    {
        // Desactivamos ventana de daño y limpiamos flags
        damageWindowActive = false;
        alreadyHitThisWindow = false;
    }

    void HitPlayer()
    {
        // Si no hay jugador no hacemos nada
        if (playerInside == null) return;

        // Aplicamos daño al jugador pasando la posición del pincho como origen
        playerInside.TakeDamage(damage, transform.position);
        alreadyHitThisWindow = true;
    }
}
