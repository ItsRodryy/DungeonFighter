using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public float speed = 5f;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    Vector2 input;
    Vector2 faceDir = Vector2.down;   // dirección estable a la que "mira"

    // Histéresis: evita parpadeo Idle/Run
    bool moving = false;
    const float ENTER = 0.15f;  // entra a Run si superas esto
    const float EXIT = 0.05f;  // vuelve a Idle si bajas de esto

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Ejes crudos (-1/0/1) y normalizados (diagonal = 1)
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        float mag = input.sqrMagnitude; // 0 o 1 (≈1 en diagonal)

        // Histéresis para Speed
        if (!moving && mag > ENTER) moving = true;
        else if (moving && mag < EXIT) moving = false;

        // Dirección cardinal estable: lateral O arriba/abajo (no ambas)
        if (mag > 0.001f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                faceDir = new Vector2(Mathf.Sign(input.x), 0f);   // derecha/izquierda
            else
                faceDir = new Vector2(0f, Mathf.Sign(input.y));   // arriba/abajo
        }

        // Parámetros del Animator
        anim.SetFloat("MoveX", faceDir.x);
        anim.SetFloat("MoveY", faceDir.y);
        anim.SetFloat("Speed", moving ? 1f : 0f);   // 0 ó 1, sin flicker

        // Flip solo si está en lateral
        sr.flipX = (faceDir.y == 0f) && (faceDir.x < 0f);
    }

    void FixedUpdate()
    {
        // Usa velocity (más compatible). Si 'linearVelocity' te compila, te vale también.
        rb.linearVelocity = input * speed;
        // rb.linearVelocity = input * speed; // alternativa en Unity 6 si la prefieres
    }
}
