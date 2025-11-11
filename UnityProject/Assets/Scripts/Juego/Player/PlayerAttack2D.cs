using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAttack2D : MonoBehaviour
{
    [Header("Refs")]
    public Collider2D hitbox;          // arrastra Hitbox_Sword (BoxCollider2D)

    [Header("Ajustes")]
    public float cooldown = 0.35f;     // tiempo entre ataques
    public float lockTime = 0.12f;     // “clavado” breve para que el golpe se sienta
    public KeyCode key = KeyCode.J;    // o usa botón del ratón

    [Header("Offsets locales de la hitbox")]
    public Vector2 offsetSide = new(0.6f, 0f);
    public Vector2 offsetUp = new(0f, 0.6f);
    public Vector2 offsetDown = new(0f, -0.6f);

    Animator anim; Rigidbody2D rb;
    float nextTime; float lockUntil;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (hitbox) hitbox.enabled = false; // siempre empieza apagada
    }

    void Update()
    {
        // bloquear un pelín el movimiento durante el swing
        if (Time.time < lockUntil) rb.linearVelocity = Vector2.zero;

        bool pressed = Input.GetKeyDown(key) || Input.GetMouseButtonDown(0);
        if (!pressed || Time.time < nextTime) return;

        // Dirección actual desde el Animator (la actualiza tu PlayerController2D)
        Vector2 face = GetFacingFromAnimator();

        // Forzamos que el BT_Attack lea esta dirección ESTE frame
        anim.SetFloat("MoveX", face.x);
        anim.SetFloat("MoveY", face.y);
        anim.Update(0f);               // empuja los cambios al árbol

        anim.SetTrigger("Attack");     // dispara el estado de ataque

        // Pequeño “lock” para que no patine durante el golpe
        lockUntil = Time.time + lockTime;
        nextTime = Time.time + cooldown;
    }

    Vector2 GetFacingFromAnimator()
    {
        float mx = anim.GetFloat("MoveX");
        float my = anim.GetFloat("MoveY");
        // cardinalizar
        if (Mathf.Abs(mx) > Mathf.Abs(my))
            return new Vector2(Mathf.Sign(mx), 0f);
        else
            return new Vector2(0f, Mathf.Sign(my));
    }

    // --- Llamadas desde Animation Events en los clips Attack_* ---
    public void EnableHitbox()
    {
        if (!hitbox) return;
        // recolocar según la dirección actual
        Vector2 face = GetFacingFromAnimator();
        Transform t = hitbox.transform;
        if (face.y > 0) t.localPosition = offsetUp;
        else if (face.y < 0) t.localPosition = offsetDown;
        else if (face.x > 0) t.localPosition = offsetSide;
        else t.localPosition = new(-offsetSide.x, offsetSide.y);

        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitbox) hitbox.enabled = false;
    }
}
