using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public float speed = 5f;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    Vector2 input;

    Vector2 faceDir = Vector2.down;

    bool moving = false;

    const float ENTER = 0.15f;
    const float EXIT = 0.05f;

    void Awake()
    {
        // Cacheamos componentes
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Configuramos rigidbody para topdown
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
    }

    void Start()
    {
        // Hacemos que el jugador nazca mirando hacia abajo
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveY", -1f);
        anim.SetFloat("Speed", 0f);
    }

    void Update()
    {
        // Leemos input crudo y lo normalizamos
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        float mag = input.sqrMagnitude;

        // Aplicamos histéresis para evitar parpadeos en idle moving
        if (!moving && mag > ENTER)
        {
            moving = true;
        }
        else if (moving && mag < EXIT)
        {
            moving = false;
        }

        // Si hay input actualizamos la dirección de mirada
        if (mag > 0.001f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                faceDir = new Vector2(Mathf.Sign(input.x), 0f);
            }
            else
            {
                faceDir = new Vector2(0f, Mathf.Sign(input.y));
            }
        }

        // Mandamos parámetros al animator
        anim.SetFloat("MoveX", faceDir.x);
        anim.SetFloat("MoveY", faceDir.y);
        anim.SetFloat("Speed", moving ? 1f : 0f);

        // Hacemos flip si miramos a la izquierda
        sr.flipX = (faceDir.y == 0f) && (faceDir.x < 0f);

        // Activamos o paramos pasos según movimiento
        if (GestorDeAudio.I != null)
            GestorDeAudio.I.SetPasos(moving);
    }

    void FixedUpdate()
    {
        // Movemos el rigidbody con la velocidad configurada
        rb.linearVelocity = input * speed;
    }
}
