using UnityEngine;

public class DungeonGate : MonoBehaviour
{
    public Animator anim;      // Animator de esta puerta (AC_Gate)
    public Collider2D col;     // Collider que bloquea el paso

    bool opened;

    void Awake()
    {
        // Si no están asignados a mano, intenta cogerlos del mismo objeto
        if (!anim) anim = GetComponent<Animator>();
        if (!col) col = GetComponent<Collider2D>();
    }

    public void Open()
    {
        if (opened) return;    // solo una vez

        opened = true;

        // Lanza la animación de abrir
        if (anim)
            anim.SetTrigger("Open");

        // Quita la colisión para poder pasar
        if (col)
            col.enabled = false;
    }
}
