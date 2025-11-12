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
    public BoxCollider2D hitbox;       // BoxCollider2D del hijo Hitbox_Spear

    [Header("Movimiento / Persecución")]
    public float moveSpeed = 3f;
    public float aggroTiles = 4f;
    public float deaggroTiles = 5.5f;
    public bool requireLineOfSight = false;
    public LayerMask wallMask;

    [Header("Ataque")]
    public float attackRangeTiles = 1.6f;
    public float attackCooldown = 0.8f;

    [Header("Offsets (se recalculan)")]
    public Vector2 offsetSide, offsetUp, offsetDown;
    public float offsetMargin = 0.05f;     // holgura

    Rigidbody2D rb; Animator anim; SpriteRenderer sr; Collider2D bodyCol;
    Vector2 input, faceDir = Vector2.down;
    bool chasing; float lastAttackTime = -999f;
    Transform hitTf;

    // no empujar sin layers
    Collider2D myBody, playerBody; bool ignoringPush;
    float stickDist = 0.6f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        bodyCol = GetComponent<Collider2D>();
        myBody = bodyCol;

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
        if (player) playerBody = player.GetComponent<Collider2D>();

        if (hitbox)
        {
            hitbox.enabled = false;      // apagada por defecto
            hitTf = hitbox.transform;
        }

        // mirar abajo al nacer
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveY", -1f);
        anim.SetFloat("Speed", 0f);

        RecalcOffsets();                 // calcula offsets exactos
        // distancia de “pegados” según tamaño reales
        var be = bodyCol.bounds.extents;
        stickDist = Mathf.Max(be.x, be.y) * 1.1f;
    }

    // --- util: extents en mundo de un BoxCollider2D, aunque esté en un hijo ---
    static Vector2 WorldExtents(BoxCollider2D bc)
    {
        var ls = bc.transform.lossyScale;
        Vector2 half = bc.size * 0.5f;
        return new Vector2(Mathf.Abs(half.x * ls.x), Mathf.Abs(half.y * ls.y));
    }

    void RecalcOffsets()
    {
        if (!hitbox || !bodyCol) return;

        // extents de cuerpo e hitbox en mundo
        var be = bodyCol.bounds.extents;
        var he = WorldExtents(hitbox);

        offsetSide = new Vector2(be.x + he.x + offsetMargin, 0f);
        offsetUp = new Vector2(0f, be.y + he.y + offsetMargin);
        offsetDown = new Vector2(0f, -(be.y + he.y + offsetMargin));
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
                rb.linearVelocity = Vector2.zero; // no andes mientras golpeas
                TriggerAttack(toP);
            }
            else
            {
                Vector2 desired = toP.normalized;
                input = ObstacleAwareDirection(desired);
            }
        }
        else input = Vector2.zero;

        if (input.sqrMagnitude > 0.001f)
            faceDir = (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                    ? new(Mathf.Sign(input.x), 0f)
                    : new(0f, Mathf.Sign(input.y));

        anim.SetFloat("MoveX", faceDir.x);
        anim.SetFloat("MoveY", faceDir.y);
        anim.SetFloat("Speed", input.sqrMagnitude > 0.001f ? 1f : 0f);

        sr.flipX = (faceDir.y == 0f) && (faceDir.x < 0f);

        HandleNoPush();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }

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

        // fija la dirección del golpe hacia el player
        faceDir = (Mathf.Abs(toP.x) > Mathf.Abs(toP.y)) ? new(Mathf.Sign(toP.x), 0)
                                                        : new(0, Mathf.Sign(toP.y));
        anim.SetTrigger("Attack");
    }

    // --- Animation Events en los clips ATTACK ---
    public void EnableHitbox()
    {
        if (!hitbox) return;

        // por si cambiaste de escala/size en editor
        RecalcOffsets();

        // reevalúa dirección justo ahora
        if (player)
        {
            Vector2 toP = (Vector2)player.position - (Vector2)transform.position;
            faceDir = (Mathf.Abs(toP.x) > Mathf.Abs(toP.y)) ? new(Mathf.Sign(toP.x), 0)
                                                            : new(0, Mathf.Sign(toP.y));
        }

        // coloca la hitbox delante
        if (hitTf)
        {
            if (faceDir.y > 0) hitTf.localPosition = offsetUp;
            else if (faceDir.y < 0) hitTf.localPosition = offsetDown;
            else if (faceDir.x > 0) hitTf.localPosition = offsetSide;
            else hitTf.localPosition = new(-offsetSide.x, offsetSide.y);
        }
        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitbox) hitbox.enabled = false;
    }

    // No empujar sin layers
    void HandleNoPush()
    {
        if (!playerBody || !myBody) return;

        float d = Vector2.Distance(transform.position, player.position);

        if (d < stickDist && !ignoringPush)
        {
            Physics2D.IgnoreCollision(myBody, playerBody, true);
            ignoringPush = true;
            rb.linearVelocity = Vector2.zero;
        }
        else if (d > stickDist + 0.4f && ignoringPush)
        {
            Physics2D.IgnoreCollision(myBody, playerBody, false);
            ignoringPush = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, aggroTiles);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRangeTiles);
        if (hitbox)
        {
            Gizmos.color = Color.cyan;
            var c = hitbox.transform.position;
            var e = WorldExtents(hitbox);
            Gizmos.DrawWireCube(c, e * 2f); // ver tamaño hitbox
        }
    }
}
