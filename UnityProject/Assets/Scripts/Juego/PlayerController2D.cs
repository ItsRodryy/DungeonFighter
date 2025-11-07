using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public float speed = 5f;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    Vector2 input;
    Vector2 lastDir = Vector2.down;

    // Hist�resis para evitar flicker Idle/Run
    bool moving = false;
    const float enterThresh = 0.15f; // entra en Run si superas esto
    const float exitThresh = 0.05f; // vuelve a Idle si bajas de esto

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // ayuda al suavizado
    }

    void Update()
    {
        // Ejes "Raw" ->  -1/0/1 (sin suavizado que introduce ruido)
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        float mag = input.sqrMagnitude; // 0 � 1 (o ~1 en diagonal)

        // Hist�resis: evita oscilar cerca de 0
        if (!moving && mag > enterThresh) moving = true;
        else if (moving && mag < exitThresh) moving = false;

        if (mag > 0.001f) lastDir = input;

        // Alimenta el Animator
        anim.SetFloat("MoveX", lastDir.x);
        anim.SetFloat("MoveY", lastDir.y);
        anim.SetFloat("Speed", moving ? 1f : 0f); // 0 � 1, sin parpadeos

        // Mirada lateral (reutiliza el clip Side)
        if (Mathf.Abs(lastDir.x) > Mathf.Abs(lastDir.y))
            sr.flipX = lastDir.x < 0f;
        else
            sr.flipX = false;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * speed;
    }
}
