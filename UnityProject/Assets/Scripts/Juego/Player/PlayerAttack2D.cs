using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAttack2D : MonoBehaviour
{
    [Header("Refs")]
    // Hitbox del arma (collider de la espada).
    public Collider2D hitbox;

    [Header("Ajustes")]
    // Tiempo mínimo entre ataques.
    public float cooldown = 0.2f;

    // Tecla o botón que dispara el ataque.
    public KeyCode key = KeyCode.J;

    [Header("Offsets locales de la hitbox")]
    // Offset local cuando se ataca a derecha/izquierda.
    public Vector2 offsetSide = new Vector2(0.6f, 0f);

    // Offset local cuando se ataca hacia arriba.
    public Vector2 offsetUp = new Vector2(0f, 0.6f);

    // Offset local cuando se ataca hacia abajo.
    public Vector2 offsetDown = new Vector2(0f, -0.6f);

    // Componentes cacheados.
    Animator anim;
    Rigidbody2D rb;

    // Tiempo mínimo hasta el siguiente ataque permitido.
    float nextTime;

    void Awake()
    {
        // Cacheo de componentes.
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // La hitbox empieza siempre desactivada.
        if (hitbox) hitbox.enabled = false;
    }

    void Update()
    {
        // Pulsación de ataque: tecla configurada o botón izquierdo del ratón.
        bool pressed = Input.GetKeyDown(key) || Input.GetMouseButtonDown(0);

        // Si no se ha pulsado o sigue en cooldown, salimos.
        if (!pressed || Time.time < nextTime) return;

        // Dirección actual de la mirada, sacada del Animator.
        Vector2 face = GetFacingFromAnimator();

        // Actualizamos la dirección en el Animator para que el árbol de ataques la use.
        anim.SetFloat("MoveX", face.x);
        anim.SetFloat("MoveY", face.y);

        // Forzar update para que el BlendTree coja la dirección en este frame.
        anim.Update(0f);

        // Reset de triggers antiguos, por seguridad.
        anim.ResetTrigger("Attack");

        // Disparo del trigger de ataque.
        anim.SetTrigger("Attack");

        if (GestorDeAudio.I != null)
            GestorDeAudio.I.ReproducirGolpe();


        // Actualizamos el cooldown del siguiente ataque.
        nextTime = Time.time + cooldown;
    }

    // Saca una dirección cardinal a partir de los parámetros de movimiento del Animator.
    Vector2 GetFacingFromAnimator()
    {
        float mx = anim.GetFloat("MoveX");
        float my = anim.GetFloat("MoveY");

        // Determinamos si domina el eje X o el eje Y para escoger entre 4 direcciones.
        if (Mathf.Abs(mx) > Mathf.Abs(my))
        {
            // Izquierda / derecha.
            return new Vector2(Mathf.Sign(mx), 0f);
        }
        else
        {
            // Arriba / abajo.
            return new Vector2(0f, Mathf.Sign(my));
        }
    }

    // Llamado desde eventos de animación en los clips Attack_* para encender la hitbox.
    public void EnableHitbox()
    {
        if (!hitbox) return;

        // Obtenemos la dirección actual de la cara.
        Vector2 face = GetFacingFromAnimator();

        // Colocamos la hitbox según esa dirección.
        Transform t = hitbox.transform;

        if (face.y > 0f)
        {
            t.localPosition = offsetUp;
        }
        else if (face.y < 0f)
        {
            t.localPosition = offsetDown;
        }
        else if (face.x > 0f)
        {
            t.localPosition = offsetSide;
        }
        else
        {
            // Hacia la izquierda.
            t.localPosition = new Vector2(-offsetSide.x, offsetSide.y);
        }

        // Activamos el collider de la hitbox.
        hitbox.enabled = true;
    }

    // Llamado desde eventos de animación para apagar la hitbox al terminar el golpe.
    public void DisableHitbox()
    {
        if (hitbox) hitbox.enabled = false;
    }
}
