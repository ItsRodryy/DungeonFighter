using UnityEngine;
using DungeonFighter.Combat;   // Para PlayerHealth

public class SpikesTrap : MonoBehaviour
{
    // Collider que hace daño cuando está activado
    public Collider2D damageCollider;

    // Si los pinchos están activos (lo encienden los Animation Events)
    bool active;

    void Awake()
    {
        if (damageCollider) damageCollider.enabled = false;
    }

    // Llamado EXACTAMENTE desde Animation Event cuando los pinchos están ARRIBA
    public void EnableDamage()
    {
        active = true;
        if (damageCollider) damageCollider.enabled = true;
    }

    // Llamado EXACTAMENTE desde Animation Event cuando los pinchos BAJAN
    public void DisableDamage()
    {
        active = false;
        if (damageCollider) damageCollider.enabled = false;
    }

    // Si el jugador entra o se queda dentro mientras están arriba ? daño
    void OnTriggerEnter2D(Collider2D other)
    {
        TryKill(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryKill(other);
    }

    void TryKill(Collider2D other)
    {
        if (!active) return;
        if (!other.CompareTag("Player")) return;

        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null)
        {
            // LLAMADA 100% REAL A TU FUNCIÓN AUTÉNTICA
            hp.TakeDamage(999, transform.position);
        }
    }
}
