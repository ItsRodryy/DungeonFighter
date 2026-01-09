using UnityEngine;
using DungeonFighter.Combat;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class EnemyChase2D : MonoBehaviour
{
    [Header("Distancia mínima al jugador")]
    public float minDistanceFromPlayer = 0.6f;

    [Header("Referencias")]
    public Transform player;
    public BoxCollider2D hitbox;

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
    public Vector2 offsetSide;
    public Vector2 offsetUp;
    public Vector2 offsetDown;
    public float offsetMargin = 0.05f;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    Collider2D bodyCol;

    Vector2 input;
    Vector2 faceDir = Vector2.down;

    bool chasing;

    float lastAttackTime = -999f;

    Transform hitTf;

    Collider2D myBody;
    Collider2D playerBody;
    bool ignoringPush;
    float stickDist = 0.6f;

    PlayerHealth playerHp;

    void Awake()
    {
        // Cacheamos componentes
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        bodyCol = GetComponent<Collider2D>();
        myBody = bodyCol;

        // Configuramos rigidbody para topdown
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
    }

    void Start()
    {
        // Si no asignamos jugador lo buscamos por tag
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // Cacheamos collider y vida del jugador si existe
        if (player)
        {
            playerBody = player.GetComponent<Collider2D>();
            playerHp = player.GetComponent<PlayerHealth>();
        }

        // Configuramos hitbox de ataque
        if (hitbox)
        {
            hitbox.enabled = false;
            hitTf = hitbox.transform;
        }

        // Ponemos idle inicial mirando abajo
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveY", -1f);
        anim.SetFloat("Speed", 0f);

        // Recalculamos offsets de hitbox en base al tamaño
        RecalcOffsets();

        // Calculamos distancia de pegado para no empujar
        var be = bodyCol.bounds.extents;
        stickDist = Mathf.Max(be.x, be.y) * 1.2f;
    }

    static Vector2 WorldExtents(BoxCollider2D bc)
    {
        // Calculamos semiejes en mundo teniendo en cuenta escala
        var ls = bc.transform.lossyScale;
        Vector2 half = bc.size * 0.5f;

        return new Vector2(
            Mathf.Abs(half.x * ls.x),
            Mathf.Abs(half.y * ls.y)
        );
    }

    void RecalcOffsets()
    {
        // Si faltan referencias no recalculamos
        if (!hitbox || !bodyCol) return;

        var be = bodyCol.bounds.extents;
        var he = WorldExtents(hitbox);

        // Calculamos offsets para separar cuerpo y hitbox
        offsetSide = new Vector2(be.x + he.x + offsetMargin, 0f);
        offsetUp = new Vector2(0f, be.y + he.y + offsetMargin);
        offsetDown = new Vector2(0f, -(be.y + he.y + offsetMargin));
    }

    void Update()
    {
        // Si no hay jugador o está muerto dejamos al enemigo en idle
        if (!player || (playerHp && playerHp.IsDead))
        {
            input = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            anim.ResetTrigger("Attack");
            anim.SetFloat("Speed", 0f);
            return;
        }

        Vector2 pos = rb.position;
        Vector2 toP = (Vector2)player.position - pos;
        float dist = toP.magnitude;

        // Marcamos si estamos demasiado cerca para no empujar ni meter jitter
        bool tooClose = dist <= minDistanceFromPlayer;

        // Comprobamos rango de aggro
        bool inAggro = dist <= aggroTiles;

        bool hasLOS = true;

        // Si pedimos línea de visión lanzamos un raycast a pared
        if (requireLineOfSight && inAggro)
        {
            var hit = Physics2D.Raycast(
                pos,
                toP.normalized,
                dist,
                wallMask
            );

            hasLOS = (hit.collider == null);
        }

        // Entramos en persecución si está en rango y con visión
        if (!chasing && inAggro && hasLOS)
        {
            chasing = true;
        }

        // Salimos si se va demasiado lejos
        if (chasing && dist > deaggroTiles)
        {
            chasing = false;
        }

        if (chasing)
        {
            // Si estamos muy cerca paramos y solo atacamos si toca
            if (tooClose)
            {
                input = Vector2.zero;
                rb.linearVelocity = Vector2.zero;

                if (dist <= attackRangeTiles && Time.time >= lastAttackTime + attackCooldown)
                {
                    TriggerAttack(toP);
                }
            }
            else if (dist <= attackRangeTiles && Time.time >= lastAttackTime + attackCooldown)
            {
                // Si estamos en rango de ataque paramos y atacamos
                input = Vector2.zero;
                rb.linearVelocity = Vector2.zero;
                TriggerAttack(toP);
            }
            else
            {
                // Perseguimos y ajustamos para evitar paredes
                Vector2 desired = toP.normalized;
                input = ObstacleAwareDirection(desired);
            }
        }
        else
        {
            // Si no perseguimos nos quedamos quietos
            input = Vector2.zero;
        }

        // Actualizamos dirección de mirada en 4 direcciones
        if (input.sqrMagnitude > 0.001f)
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
        anim.SetFloat("Speed", input.sqrMagnitude > 0.001f ? 1f : 0f);

        // Hacemos flip si miramos izquierda
        sr.flipX = (faceDir.y == 0f) && (faceDir.x < 0f);

        // Gestionamos no push para evitar empujones raros al pegarse
        HandleNoPush();
    }

    void FixedUpdate()
    {
        // Aplicamos movimiento físico
        rb.linearVelocity = input * moveSpeed;
    }

    Vector2 ObstacleAwareDirection(Vector2 desired)
    {
        if (desired == Vector2.zero) return Vector2.zero;

        // Redondeamos dirección a cardinal
        Vector2 d = new Vector2(
            Mathf.Abs(desired.x) > 0.01f ? Mathf.Sign(desired.x) : 0f,
            Mathf.Abs(desired.y) > 0.01f ? Mathf.Sign(desired.y) : 0f
        );

        // Probamos dirección completa primero
        if (!Blocked(d)) return d;

        // Si falla probamos ejes según dominante
        if (Mathf.Abs(desired.x) >= Mathf.Abs(desired.y))
        {
            if (!Blocked(new Vector2(d.x, 0f))) return new Vector2(d.x, 0f);
            if (!Blocked(new Vector2(0f, d.y))) return new Vector2(0f, d.y);
        }
        else
        {
            if (!Blocked(new Vector2(0f, d.y))) return new Vector2(0f, d.y);
            if (!Blocked(new Vector2(d.x, 0f))) return new Vector2(d.x, 0f);
        }

        // Si todo está bloqueado nos paramos
        return Vector2.zero;
    }

    bool Blocked(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;

        var b = bodyCol.bounds;

        // Hacemos un boxcast corto delante para detectar pared
        return Physics2D.BoxCast(
            b.center,
            b.size * 0.95f,
            0f,
            dir,
            0.1f,
            wallMask
        );
    }

    void TriggerAttack(Vector2 toP)
    {
        // Guardamos instante para cooldown
        lastAttackTime = Time.time;

        // Marcamos dirección hacia el jugador en cardinal
        if (Mathf.Abs(toP.x) > Mathf.Abs(toP.y))
        {
            faceDir = new Vector2(Mathf.Sign(toP.x), 0f);
        }
        else
        {
            faceDir = new Vector2(0f, Mathf.Sign(toP.y));
        }

        // Disparamos trigger de ataque
        anim.SetTrigger("Attack");
    }

    public void EnableHitbox()
    {
        if (!hitbox) return;

        // Si el jugador murió no armamos
        if (playerHp && playerHp.IsDead) return;

        // Recalculamos offsets por si cambió escala
        RecalcOffsets();

        // Actualizamos dirección hacia el jugador para colocar hitbox bien
        if (player)
        {
            Vector2 toP = (Vector2)player.position - (Vector2)transform.position;

            if (Mathf.Abs(toP.x) > Mathf.Abs(toP.y))
            {
                faceDir = new Vector2(Mathf.Sign(toP.x), 0f);
            }
            else
            {
                faceDir = new Vector2(0f, Mathf.Sign(toP.y));
            }
        }

        // Colocamos hitbox según dirección
        if (hitTf)
        {
            if (faceDir.y > 0f)
            {
                hitTf.localPosition = offsetUp;
            }
            else if (faceDir.y < 0f)
            {
                hitTf.localPosition = offsetDown;
            }
            else if (faceDir.x > 0f)
            {
                hitTf.localPosition = offsetSide;
            }
            else
            {
                hitTf.localPosition = new Vector2(-offsetSide.x, offsetSide.y);
            }
        }

        // Armamos el componente de daño para este swing
        var dmg = hitbox.GetComponent<EnemyMeleeDamage>();
        if (dmg) dmg.BeginSwing();

        // Encendemos collider para que golpee
        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        if (!hitbox) return;

        // Desarmamos el componente de daño del swing
        var dmg = hitbox.GetComponent<EnemyMeleeDamage>();
        if (dmg) dmg.EndSwing();

        // Apagamos hitbox al terminar
        hitbox.enabled = false;
    }

    void HandleNoPush()
    {
        if (!playerBody || !myBody) return;

        float d = Vector2.Distance(transform.position, player.position);

        // Si estamos muy cerca ignoramos colisión para evitar empujones
        if (d < stickDist && !ignoringPush)
        {
            Physics2D.IgnoreCollision(myBody, playerBody, true);
            ignoringPush = true;
            rb.linearVelocity = Vector2.zero;
        }
        else if (d > stickDist + 0.4f && ignoringPush)
        {
            // Cuando nos separamos reactivamos la colisión
            Physics2D.IgnoreCollision(myBody, playerBody, false);
            ignoringPush = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujamos rangos para depurar
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroTiles);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRangeTiles);

        // Dibujamos la hitbox si existe
        if (hitbox)
        {
            Gizmos.color = Color.cyan;
            var c = hitbox.transform.position;
            var e = WorldExtents(hitbox);
            Gizmos.DrawWireCube(c, e * 2f);
        }
    }
}
