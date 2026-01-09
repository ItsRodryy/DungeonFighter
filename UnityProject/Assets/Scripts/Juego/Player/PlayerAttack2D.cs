using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAttack2D : MonoBehaviour
{
    [Header("Refs")]
    public Collider2D hitbox;

    [Header("Ajustes")]
    public float cooldown = 0.2f;

    public KeyCode key = KeyCode.J;

    [Header("Offsets locales de la hitbox")]
    public Vector2 offsetSide = new Vector2(0.6f, 0f);
    public Vector2 offsetUp = new Vector2(0f, 0.6f);
    public Vector2 offsetDown = new Vector2(0f, -0.6f);

    Animator anim;
    Rigidbody2D rb;

    float nextTime;

    void Awake()
    {
        // Cacheamos componentes
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Apagamos hitbox al iniciar
        if (hitbox) hitbox.enabled = false;
    }

    void Update()
    {
        // Detectamos pulsación por tecla o click izquierdo
        bool pressed = Input.GetKeyDown(key) || Input.GetMouseButtonDown(0);

        // Si no se pulsa o estamos en cooldown salimos
        if (!pressed || Time.time < nextTime) return;

        // Sacamos dirección actual desde el animator
        Vector2 face = GetFacingFromAnimator();

        // Actualizamos parámetros para que el ataque salga en la dirección correcta
        anim.SetFloat("MoveX", face.x);
        anim.SetFloat("MoveY", face.y);

        // Forzamos update para que el blendtree coja dirección este frame
        anim.Update(0f);

        // Reseteamos trigger por seguridad
        anim.ResetTrigger("Attack");

        // Disparamos ataque
        anim.SetTrigger("Attack");

        // Reproducimos sonido de golpe si tenemos gestor
        if (GestorDeAudio.I != null)
            GestorDeAudio.I.ReproducirGolpe();

        // Actualizamos cooldown
        nextTime = Time.time + cooldown;
    }

    Vector2 GetFacingFromAnimator()
    {
        float mx = anim.GetFloat("MoveX");
        float my = anim.GetFloat("MoveY");

        // Elegimos entre 4 direcciones según eje dominante
        if (Mathf.Abs(mx) > Mathf.Abs(my))
        {
            return new Vector2(Mathf.Sign(mx), 0f);
        }
        else
        {
            return new Vector2(0f, Mathf.Sign(my));
        }
    }

    public void EnableHitbox()
    {
        if (!hitbox) return;

        // Cogemos dirección actual
        Vector2 face = GetFacingFromAnimator();

        // Ajustamos posición local de la hitbox
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
            t.localPosition = new Vector2(-offsetSide.x, offsetSide.y);
        }

        // Encendemos collider para que haga daño
        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        // Apagamos la hitbox al terminar el golpe
        if (hitbox) hitbox.enabled = false;
    }
}
