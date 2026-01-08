using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    // Velocidad de movimiento base del jugador.
    public float speed = 5f;

    // Componentes cacheados.
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    // Input de movimiento actual (normalizado).
    Vector2 input;

    // Dirección hacia la que mira el jugador (cardinal).
    Vector2 faceDir = Vector2.down;

    // Indica si estamos en estado de movimiento (para el BlendTree).
    bool moving = false;

    // Umbrales de entrada/salida del movimiento para evitar parpadeos.
    const float ENTER = 0.15f;
    const float EXIT = 0.05f;

    void Awake()
    {
        // Cacheo de componentes.
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Configuración típica del Rigidbody2D para topdown.
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
    }

    void Start()
    {
        // Hacemos que el jugador nazca mirando hacia abajo.
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveY", -1f);
        anim.SetFloat("Speed", 0f);
    }

    void Update()
    {
        // Leemos el input crudo de los ejes Horizontal/Vertical.
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // Magnitud al cuadrado, para evitar raíz.
        float mag = input.sqrMagnitude;

        // Cambio de estado de idle a moving con histéresis.
        if (!moving && mag > ENTER)
        {
            moving = true;
        }
        else if (moving && mag < EXIT)
        {
            moving = false;
        }

        // Si hay algo de input, actualizamos la dirección de mirada.
        if (mag > 0.001f)
        {
            // Elegimos el eje dominante.
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                faceDir = new Vector2(Mathf.Sign(input.x), 0f);
            }
            else
            {
                faceDir = new Vector2(0f, Mathf.Sign(input.y));
            }
        }

        // Enviamos la dirección y si se mueve o no al Animator.
        anim.SetFloat("MoveX", faceDir.x);
        anim.SetFloat("MoveY", faceDir.y);
        anim.SetFloat("Speed", moving ? 1f : 0f);

        // Flip horizontal del sprite si mira a la izquierda en eje X.
        sr.flipX = (faceDir.y == 0f) && (faceDir.x < 0f);

        if (GestorDeAudio.I != null)
            GestorDeAudio.I.SetPasos(moving);

    }

    void FixedUpdate()
    {
        // Aplicamos la velocidad en función del input y la velocidad base.
        rb.linearVelocity = input * speed;
    }
}
