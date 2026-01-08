using UnityEngine;
using DungeonFighter.Combat;   // Para PlayerHealth

public class ChestHealFullHP : MonoBehaviour
{
    // Animator del cofre (controla la animación de abrir).
    public Animator anim;

    // Tecla para interactuar con el cofre (configurable en Inspector).
    public KeyCode key = KeyCode.E;

    // True mientras el cofre esté bloqueado (enemigos vivos).
    public bool locked = true;

    // True cuando el cofre ya se ha abierto una vez.
    bool opened;

    // Referencia al PlayerHealth del jugador que está dentro del trigger.
    PlayerHealth player;

    // Referencia al texto "Presiona E para abrir" (hijo del cofre).
    public GameObject pressEPrompt;

    void Awake()
    {
        // Si no se ha asignado el Animator a mano, lo cogemos del mismo objeto.
        if (!anim) anim = GetComponent<Animator>();

        // Asegurarnos de que el texto empieza oculto.
        if (pressEPrompt) pressEPrompt.SetActive(false);
    }

    // Llamado desde el RoomChallengeController cuando todos los enemigos han muerto.
    public void Unlock()
    {
        locked = false;
        Debug.Log("Cofre desbloqueado: ya se puede abrir.");
        // No mostramos aún nada aquí: lo gestiona Update cuando el jugador esté cerca.
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Guardamos la referencia al jugador cuando entra en el área del cofre.
        if (!other.CompareTag("Player")) return;

        player = other.GetComponentInParent<PlayerHealth>();

        // Actualizar visibilidad del texto al entrar.
        UpdatePromptVisibility();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Borramos la referencia cuando el jugador sale del área.
        if (!other.CompareTag("Player")) return;

        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp && hp == player)
        {
            player = null;
        }

        // Al salir del trigger, ocultar el texto.
        UpdatePromptVisibility();
    }

    void Update()
    {
        // Control de visibilidad del texto en cada frame
        UpdatePromptVisibility();

        // Si ya se abrió, no hace nada más.
        if (opened) return;

        // Si sigue bloqueado por enemigos vivos, no se puede abrir.
        if (locked) return;

        // Si no hay jugador dentro del trigger, no podemos interactuar.
        if (!player) return;

        // Comprobamos la tecla de interacción:
        bool pressed =
            Input.GetKeyDown(key) ||
            Input.GetKeyDown(KeyCode.E);   // por si en el inspector cambias la tecla sin querer

        if (pressed)
        {
            // 🔥 SI ES EL COFRE FINAL (Chest3), HACEMOS VICTORIA
            var final = GetComponent<ChestFinalWin>();
            if (final != null)
            {
                opened = true;
                UpdatePromptVisibility();

                if (anim)
                    anim.SetTrigger("Open");

                final.LanzarVictoria();
                return; // ⬅️ MUY IMPORTANTE: no ejecutar el código normal
            }

            // 🟢 COFRE NORMAL (Chest1 / Chest2)
            opened = true;

            UpdatePromptVisibility();

            if (anim)
                anim.SetTrigger("Open");

            player.HealToFull();

            if (JuegoUI.Instance != null)
            {
                JuegoUI.Instance.ShowMessage("VIDA RESTAURADA AL MÁXIMO");
            }
        }

    }

    // Muestra/oculta el texto de "Presiona E" según estado actual
    void UpdatePromptVisibility()
    {
        if (!pressEPrompt) return;

        // Solo queremos ver el texto cuando:
        //  - el cofre NO está abierto
        //  - el cofre está desbloqueado
        //  - el jugador está dentro del trigger
        bool canOpen = !opened && !locked && (player != null);

        pressEPrompt.SetActive(canOpen);
    }
}
