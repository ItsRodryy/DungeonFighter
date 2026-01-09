using UnityEngine;

// Abrimos una puerta de barrotes y quitamos su colisión
public class DungeonGate : MonoBehaviour
{
    public Animator anim;
    public Collider2D col;

    bool opened;

    void Awake()
    {
        // Si no asignamos referencias las cogemos del mismo objeto
        if (!anim) anim = GetComponent<Animator>();
        if (!col) col = GetComponent<Collider2D>();
    }

    public void Open()
    {
        // Evitamos abrir más de una vez
        if (opened) return;

        opened = true;

        // Lanzamos animación de abrir
        if (anim)
            anim.SetTrigger("Open");

        // Quitamos la colisión para poder pasar
        if (col)
            col.enabled = false;

        // Reproducimos sonido de puerta si tenemos gestor de audio
        if (GestorDeAudio.I != null)
            GestorDeAudio.I.ReproducirPuerta();
    }
}
