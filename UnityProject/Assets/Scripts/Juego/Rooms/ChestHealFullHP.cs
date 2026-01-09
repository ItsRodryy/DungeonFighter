using UnityEngine;
using DungeonFighter.Combat;

// Controlamos un cofre que al abrir cura al jugador a vida completa
public class ChestHealFullHP : MonoBehaviour
{
    public Animator anim;
    public KeyCode key = KeyCode.E;

    public bool locked = true;

    bool opened;

    PlayerHealth player;

    public GameObject pressEPrompt;

    void Awake()
    {
        // Cogemos Animator si no lo asignamos a mano
        if (!anim) anim = GetComponent<Animator>();

        // Ocultamos el texto al iniciar
        if (pressEPrompt) pressEPrompt.SetActive(false);
    }

    public void Unlock()
    {
        // Quitamos el bloqueo cuando la sala se limpia
        locked = false;
        Debug.Log("Cofre desbloqueado ya se puede abrir");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Detectamos al jugador dentro del área del cofre
        if (!other.CompareTag("Player")) return;

        player = other.GetComponentInParent<PlayerHealth>();

        // Actualizamos el texto según estado
        UpdatePromptVisibility();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Limpiamos la referencia cuando el jugador sale
        if (!other.CompareTag("Player")) return;

        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp && hp == player)
        {
            player = null;
        }

        // Ocultamos texto al salir
        UpdatePromptVisibility();
    }

    void Update()
    {
        // Vamos actualizando el prompt para que sea consistente
        UpdatePromptVisibility();

        // Si ya lo abrimos no hacemos nada más
        if (opened) return;

        // Si está bloqueado no se puede abrir
        if (locked) return;

        // Si no hay jugador cerca no podemos interactuar
        if (!player) return;

        // Comprobamos pulsación de tecla de interacción
        bool pressed =
            Input.GetKeyDown(key) ||
            Input.GetKeyDown(KeyCode.E);

        if (pressed)
        {
            // Si este cofre es el final lanzamos victoria y cortamos el flujo normal
            var final = GetComponent<ChestFinalWin>();
            if (final != null)
            {
                opened = true;
                UpdatePromptVisibility();

                if (anim)
                    anim.SetTrigger("Open");

                final.LanzarVictoria();
                return;
            }

            // Cofre normal curamos a tope
            opened = true;

            UpdatePromptVisibility();

            if (anim)
                anim.SetTrigger("Open");

            player.HealToFull();

            if (JuegoUI.Instance != null)
            {
                JuegoUI.Instance.ShowMessage("VIDA RESTAURADA AL MAXIMO");
            }
        }
    }

    void UpdatePromptVisibility()
    {
        if (!pressEPrompt) return;

        // Mostramos el texto solo si no está abierto no está bloqueado y el jugador está dentro
        bool canOpen = !opened && !locked && (player != null);

        pressEPrompt.SetActive(canOpen);
    }
}
