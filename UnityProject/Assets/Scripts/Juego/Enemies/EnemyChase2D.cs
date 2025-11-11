using UnityEngine;

// ❗️Máx 3 por atributo → usa varios:
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class EnemyChase2D : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;           // si null, busca "Player"
    public Collider2D hitbox;          // BoxCollider2D del hijo Hitbox_Spear

    [Header("Movimiento / Persecución")]
    public float moveSpeed = 3f;
    public float aggroTiles = 4f;
    public float deaggroTiles = 5.5f;
    public bool requireLineOfSight = false;   // actívalo luego si quieres
    public LayerMask wallMask;                // SOLO muros

    [Header("Ataque")]
    public float attackRangeTiles = 1.6f;
    public float attackCooldown = 0.8f;

    [Header("Offsets hitbox (locales)")]
    public Vector2 offsetSide = new(0.6f, 0f);
    public Vector2 offsetUp = new(0f, 0.6f);
    public Vector2 offsetDown = new(0f, -0.6f);

    Rigidbody2D rb; Animator anim; SpriteRenderer sr; Collider2D bodyCol;
    Vector2 input, faceDir = Vector2.down;
    bool chasing; float lastAttackTime = -999f;
    Transform hitTf;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        bodyCol = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
    }

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (hitbox)
        {
            hitbox.enabled = false;
            hitTf = hitbox.transform;
        }
        // Mirar hacia abajo al nacer (Idle-Down)
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveY", -1f);
        anim.SetFloat("Speed", 0f);
    }

    void Update()
    {
        if (!player) { anim.SetFloat("Speed", 0f); return; }

        Vector2 pos = rb.position;
        Vector2 toP = (Vector2)player.position - pos;
        float dist = toP.magnitude;

        bool inAggro = dist <= aggroTiles;
        bool hasLOS = true;
        if (requireLineOfSight && inAggro)
        {
            var h = Physics2D.Raycast(pos, toP.normalized, dist, wallMask);
            hasLOS = (h.collider == null);
        }

        if (!chasing && inAggro && hasLOS) chasing = true;
        if (chasing && dist > deaggroTiles) chasing = false;

        if (chasing)
        {
            if (dist <= attackRangeTiles && Time.time >= lastAttackTime + attackCooldown)
            {
                input = Vector2.zero;
                TriggerAttack(toP);
            }
            else
            {
                Vector2 desired = toP.normalized;
                input = ObstacleAwareDirection(desired);
            }
        }
        else
        {
            input = Vector2.zero;
        }

        if (input.sqrMagnitude > 0.001f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                faceDir = new(Mathf.Sign(input.x), 0f);
            else
                faceDir = new(0f, Mathf.Sign(input.y));
        }

        anim.SetFloat("MoveX", faceDir.x);
        anim.SetFloat("MoveY", faceDir.y);
        anim.SetFloat("Speed", input.sqrMagnitude > 0.001f ? 1f : 0f);

        sr.flipX = (faceDir.y == 0f) && (faceDir.x < 0f);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }

    // Evitación simple de obstáculos (sin pathfinding)
    Vector2 ObstacleAwareDirection(Vector2 desired)
    {
        if (desired == Vector2.zero) return Vector2.zero;

        Vector2 d = new(
            Mathf.Abs(desired.x) > 0.01f ? Mathf.Sign(desired.x) : 0f,
            Mathf.Abs(desired.y) > 0.01f ? Mathf.Sign(desired.y) : 0f
        );

        if (!Blocked(d)) return d;

        if (Mathf.Abs(desired.x) >= Mathf.Abs(desired.y))
        {
            if (!Blocked(new Vector2(d.x, 0))) return new Vector2(d.x, 0);
            if (!Blocked(new Vector2(0, d.y))) return new Vector2(0, d.y);
        }
        else
        {
            if (!Blocked(new Vector2(0, d.y))) return new Vector2(0, d.y);
            if (!Blocked(new Vector2(d.x, 0))) return new Vector2(d.x, 0);
        }
        return Vector2.zero;
    }

    bool Blocked(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;
        var b = bodyCol.bounds;
        return Physics2D.BoxCast(b.center, b.size * 0.95f, 0f, dir, 0.1f, wallMask);
    }

    void TriggerAttack(Vector2 toP)
    {
        lastAttackTime = Time.time;

        if (Mathf.Abs(toP.x) > Mathf.Abs(toP.y)) faceDir = new(Mathf.Sign(toP.x), 0);
        else faceDir = new(0, Mathf.Sign(toP.y));

        if (hitTf)
        {
            if (faceDir.y > 0) hitTf.localPosition = offsetUp;
            else if (faceDir.y < 0) hitTf.localPosition = offsetDown;
            else if (faceDir.x > 0) hitTf.localPosition = offsetSide;
            else hitTf.localPosition = new(-offsetSide.x, offsetSide.y);
        }
        anim.SetTrigger("Attack");
    }

    // Animation Events
    public void EnableHitbox() { if (hitbox) hitbox.enabled = true; }
    public void DisableHitbox() { if (hitbox) hitbox.enabled = false; }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, aggroTiles);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRangeTiles);
    }
}
