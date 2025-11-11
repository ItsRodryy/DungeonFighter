using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public float speed = 5f;

    Rigidbody2D rb; Animator anim; SpriteRenderer sr;
    Vector2 input, faceDir = Vector2.down;
    bool moving = false;
    const float ENTER = 0.15f, EXIT = 0.05f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
    }

    void Start()
    {
        // Nacer mirando "abajo"
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveY", -1f);
        anim.SetFloat("Speed", 0f);
    }

    void Update()
    {
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        float mag = input.sqrMagnitude;

        if (!moving && mag > ENTER) moving = true;
        else if (moving && mag < EXIT) moving = false;

        if (mag > 0.001f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                faceDir = new Vector2(Mathf.Sign(input.x), 0f);
            else
                faceDir = new Vector2(0f, Mathf.Sign(input.y));
        }

        anim.SetFloat("MoveX", faceDir.x);
        anim.SetFloat("MoveY", faceDir.y);
        anim.SetFloat("Speed", moving ? 1f : 0f);

        sr.flipX = (faceDir.y == 0f) && (faceDir.x < 0f);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * speed;
    }
}
