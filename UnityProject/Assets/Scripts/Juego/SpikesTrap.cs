using DungeonFighter.Combat;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class SpikesTrap : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int damage = 1;

    // Jugador que está pisando este pincho (si hay)
    PlayerHealth playerInside;

    // Ventana de daño activa (pinchos arriba)
    bool damageWindowActive;

    // Para que solo golpee una vez por subida
    bool alreadyHitThisWindow;

    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;    // importante: trigger
    }

    // ---------------- TRIGGERS ----------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = other.GetComponentInParent<PlayerHealth>();
        if (playerInside == null) return;

        // Si entramos mientras la ventana está activa, pegar ya
        if (damageWindowActive && !alreadyHitThisWindow)
        {
            HitPlayer();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp == playerInside)
        {
            playerInside = null;
        }
    }

    // ---------------- EVENTOS DE ANIMACIÓN ----------------

    // Llamado en el frame donde los pinchos ESTÁN ARRIBA
    public void EnableDamage()
    {
        damageWindowActive = true;
        alreadyHitThisWindow = false;

        // Si el jugador ya estaba encima cuando suben, pegar aquí
        if (playerInside != null && !alreadyHitThisWindow)
        {
            HitPlayer();
        }
    }

    // Llamado en el frame donde los pinchos BAJAN
    public void DisableDamage()
    {
        damageWindowActive = false;
        alreadyHitThisWindow = false;
    }

    // ---------------- LÓGICA DE DAÑO ----------------

    void HitPlayer()
    {
        if (playerInside == null) return;

        playerInside.TakeDamage(damage, transform.position);
        alreadyHitThisWindow = true;
    }
}
