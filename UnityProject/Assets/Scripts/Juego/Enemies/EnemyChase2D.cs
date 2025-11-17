using UnityEngine;
using DungeonFighter.Combat; // Para PlayerHealth y EnemyMeleeDamage.

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class EnemyChase2D : MonoBehaviour
{
    [Header("Referencias")]
    // Transform del jugador. Si está a null, se busca por tag "Player".
    public Transform player;

    // BoxCollider2D de la hitbox de ataque (hijo Hitbox_Spear).
    public BoxCollider2D hitbox;

    [Header("Movimiento / Persecución")]
    // Velocidad de movimiento del enemigo.
    public float moveSpeed = 3f;

    // Distancia a la que empieza a perseguir al jugador.
    public float aggroTiles = 4f;

    // Distancia a la que deja de perseguir (algo mayor para evitar "titiriteo").
    public float deaggroTiles = 5.5f;

    // Si es true, solo persigue si tiene línea de visión sin paredes de por medio.
    public bool requireLineOfSight = false;

    // Capas que se consideran "pared" para raycasts y boxcasts.
    public LayerMask wallMask;

    [Header("Ataque")]
    // Distancia máxima desde la que puede atacar.
    public float attackRangeTiles = 1.6f;

    // Tiempo mínimo entre ataques consecutivos.
    public float attackCooldown = 0.8f;

    [Header("Offsets (se recalculan)")]
    // Offset local de la hitbox cuando ataca hacia derecha/izquierda.
    public Vector2 offsetSide;

    // Offset local cuando ataca hacia arriba.
    public Vector2 offsetUp;

    // Offset local cuando ataca hacia abajo.
    public Vector2 offsetDown;

    // Margen extra entre el cuerpo y la hitbox para que no se solapen.
    public float offsetMargin = 0.05f;

    // Referencias internas a componentes.
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    Collider2D bodyCol;

    // Dirección de movimiento decidida por la IA (cardinal).
    Vector2 input;

    // Dirección a la que "mira" el sprite (cardinal).
    Vector2 faceDir = Vector2.down;

    // True si está en modo persecución del jugador.
    bool chasing;

    // Último instante en el que atacó (para controlar el cooldown).
    float lastAttackTime = -999f;

    // Transform de la hitbox (objeto hijo).
    Transform hitTf;

    // Gestión para no empujar al jugador cuando se pegan.
    Collider2D myBody;
    Collider2D playerBody;
    bool ignoringPush;
    float stickDist = 0.6f;

    // Referencia a la vida del jugador para saber si está muerto.
    PlayerHealth playerHp;

    void Awake()
    {
        // Cacheo de componentes.
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        bodyCol = GetComponent<Collider2D>();
        myBody = bodyCol;

        // Configuración típica de Rigidbody2D para topdown.
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
    }

    void Start()
    {
        // Si no se ha asignado el jugador a mano, se busca por tag "Player".
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // Cacheo del collider y PlayerHealth del jugador, si existe.
        if (player)
        {
            playerBody = player.GetComponent<Collider2D>();
            playerHp = player.GetComponent<PlayerHealth>();
        }

        // Configuración inicial de la hitbox de ataque.
        if (hitbox)
        {
            hitbox.enabled = false; // La hitbox empieza apagada.
            hitTf = hitbox.transform;
        }

        // Hacer que el enemigo nazca mirando hacia abajo.
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveY", -1f);
        anim.SetFloat("Speed", 0f);

        // Calcular offsets de la hitbox según el tamaño del cuerpo.
        RecalcOffsets();

        // Calcular distancia para la lógica de "no push".
        var be = bodyCol.bounds.extents;
        stickDist = Mathf.Max(be.x, be.y) * 1.2f;
    }

    // Calcula los semiejes de un BoxCollider2D en espacio de mundo.
    static Vector2 WorldExtents(BoxCollider2D bc)
    {
        var ls = bc.transform.lossyScale;
        Vector2 half = bc.size * 0.5f;

        return new Vector2(
            Mathf.Abs(half.x * ls.x),
            Mathf.Abs(half.y * ls.y)
        );
    }

    // Recalcula offsets locales de la hitbox a partir del tamaño del cuerpo y de la propia hitbox.
    void RecalcOffsets()
    {
        if (!hitbox || !bodyCol) return;

        // Semiejes del collider del cuerpo del enemigo.
        var be = bodyCol.bounds.extents;

        // Semiejes de la hitbox en mundo.
        var he = WorldExtents(hitbox);

        // Offset lateral (derecha/izquierda).
        offsetSide = new Vector2(be.x + he.x + offsetMargin, 0f);

        // Offset hacia arriba.
        offsetUp = new Vector2(0f, be.y + he.y + offsetMargin);

        // Offset hacia abajo.
        offsetDown = new Vector2(0f, -(be.y + he.y + offsetMargin));
    }

    void Update()
    {
        // Si no hay jugador o está muerto, el enemigo se queda quieto en idle.
        if (!player || (playerHp && playerHp.IsDead))
        {
            input = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            anim.ResetTrigger("Attack");
            anim.SetFloat("Speed", 0f);
            return;
        }

        // Posición actual y vector hacia el jugador.
        Vector2 pos = rb.position;
        Vector2 toP = (Vector2)player.position - pos;
        float dist = toP.magnitude;

        // Comprobación de si el jugador está dentro del rango de aggro.
        bool inAggro = dist <= aggroTiles;

        // Por defecto hay línea de visión.
        bool hasLOS = true;

        // Si está activado requireLineOfSight, se hace un Raycast hacia el jugador.
        if (requireLineOfSight && inAggro)
        {
            var hit = Physics2D.Raycast(
                pos,
                toP.normalized,
                dist,
                wallMask
            );

            // Si golpea algo en wallMask, no hay línea de visión.
            hasLOS = (hit.collider == null);
        }

        // Entramos en modo persecución si el jugador está en rango y con línea de visión.
        if (!chasing && inAggro && hasLOS)
        {
            chasing = true;
        }

        // Salimos de persecución si se pasa del rango de desaggro.
        if (chasing && dist > deaggroTiles)
        {
            chasing = false;
        }

        // Lógica de movimiento o ataque según si está persiguiendo.
        if (chasing)
        {
            // Si estamos a rango de ataque y ha pasado el cooldown, atacamos.
            if (dist <= attackRangeTiles && Time.time >= lastAttackTime + attackCooldown)
            {
                input = Vector2.zero;
                rb.linearVelocity = Vector2.zero;
                TriggerAttack(toP);
            }
            else
            {
                // Dirección deseada hacia el jugador (normalizada).
                Vector2 desired = toP.normalized;

                // Ajustamos esa dirección para evitar paredes.
                input = ObstacleAwareDirection(desired);
            }
        }
        else
        {
            // No persigue: se queda quieto.
            input = Vector2.zero;
        }

        // Actualizamos dirección de mirada (solo 4 direcciones cardinales).
        if (input.sqrMagnitude > 0.001f)
        {
            // Priorizamos el eje dominante para evitar diagonales raras.
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                faceDir = new Vector2(Mathf.Sign(input.x), 0f);
            }
            else
            {
                faceDir = new Vector2(0f, Mathf.Sign(input.y));
            }
        }

        // Mandamos parámetros al Animator.
        anim.SetFloat("MoveX", faceDir.x);
        anim.SetFloat("MoveY", faceDir.y);
        anim.SetFloat("Speed", input.sqrMagnitude > 0.001f ? 1f : 0f);

        // Flip horizontal del sprite si miramos a la izquierda en eje X.
        sr.flipX = (faceDir.y == 0f) && (faceDir.x < 0f);

        // Actualizamos la lógica de colisión con el jugador para no empujarle.
        HandleNoPush();
    }

    void FixedUpdate()
    {
        // Movimiento físico del enemigo según la dirección elegida e intensidad.
        rb.linearVelocity = input * moveSpeed;
    }

    // Devuelve una dirección cardinal que intenta esquivar obstáculos.
    Vector2 ObstacleAwareDirection(Vector2 desired)
    {
        if (desired == Vector2.zero) return Vector2.zero;

        // Redondeamos a -1, 0 o 1 por eje para tener direcciones limpias.
        Vector2 d = new Vector2(
            Mathf.Abs(desired.x) > 0.01f ? Mathf.Sign(desired.x) : 0f,
            Mathf.Abs(desired.y) > 0.01f ? Mathf.Sign(desired.y) : 0f
        );

        // Probamos primero la dirección completa.
        if (!Blocked(d)) return d;

        // Si está bloqueada, probamos ejes por separado según el eje dominante.
        if (Mathf.Abs(desired.x) >= Mathf.Abs(desired.y))
        {
            // Primero X, luego Y.
            if (!Blocked(new Vector2(d.x, 0f))) return new Vector2(d.x, 0f);
            if (!Blocked(new Vector2(0f, d.y))) return new Vector2(0f, d.y);
        }
        else
        {
            // Primero Y, luego X.
            if (!Blocked(new Vector2(0f, d.y))) return new Vector2(0f, d.y);
            if (!Blocked(new Vector2(d.x, 0f))) return new Vector2(d.x, 0f);
        }

        // Si todo falla, no nos movemos.
        return Vector2.zero;
    }

    // Devuelve true si hay pared justo delante del collider en la dirección "dir".
    bool Blocked(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;

        var b = bodyCol.bounds;

        // BoxCast corto delante del enemigo para detectar paredes.
        return Physics2D.BoxCast(
            b.center,
            b.size * 0.95f,
            0f,
            dir,
            0.1f,
            wallMask
        );
    }

    // Prepara y lanza el ataque del enemigo hacia la dirección del jugador.
    void TriggerAttack(Vector2 toP)
    {
        // Guardamos el instante de ataque para el cooldown.
        lastAttackTime = Time.time;

        // Dirección cardinal hacia el jugador.
        if (Mathf.Abs(toP.x) > Mathf.Abs(toP.y))
        {
            faceDir = new Vector2(Mathf.Sign(toP.x), 0f);
        }
        else
        {
            faceDir = new Vector2(0f, Mathf.Sign(toP.y));
        }

        // Disparamos el trigger de ataque del Animator.
        anim.SetTrigger("Attack");
    }

    // Evento de animación: se llama cuando la animación entra en la ventana de daño.
    public void EnableHitbox()
    {
        if (!hitbox) return;

        // Si el jugador ha muerto de camino, no armamos el golpe.
        if (playerHp && playerHp.IsDead) return;

        // Recalcular offsets por si ha cambiado escala.
        RecalcOffsets();

        // Actualizamos dirección hacia el jugador, si existe.
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

        // Colocamos la hitbox según la dirección actual.
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
                // Hacia la izquierda.
                hitTf.localPosition = new Vector2(-offsetSide.x, offsetSide.y);
            }
        }

        // Armamos EnemyMeleeDamage para este swing.
        var dmg = hitbox.GetComponent<EnemyMeleeDamage>();
        if (dmg) dmg.BeginSwing();

        // Encendemos el collider de la hitbox.
        hitbox.enabled = true;
    }

    // Evento de animación: se llama cuando termina la ventana de daño.
    public void DisableHitbox()
    {
        if (!hitbox) return;

        // Desarmamos EnemyMeleeDamage para este swing.
        var dmg = hitbox.GetComponent<EnemyMeleeDamage>();
        if (dmg) dmg.EndSwing();

        // Apagamos el collider de la hitbox.
        hitbox.enabled = false;
    }

    // Lógica para que enemigo y jugador no se empujen cuando están pegados.
    void HandleNoPush()
    {
        if (!playerBody || !myBody) return;

        float d = Vector2.Distance(transform.position, player.position);

        // Si están demasiado cerca y todavía hay colisión, la desactivamos.
        if (d < stickDist && !ignoringPush)
        {
            Physics2D.IgnoreCollision(myBody, playerBody, true);
            ignoringPush = true;
            rb.linearVelocity = Vector2.zero;
        }
        // Cuando se separan, reactivamos la colisión.
        else if (d > stickDist + 0.4f && ignoringPush)
        {
            Physics2D.IgnoreCollision(myBody, playerBody, false);
            ignoringPush = false;
        }
    }

    // Dibuja gizmos para depuración del rango de aggro/ataque y la hitbox.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroTiles);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRangeTiles);

        if (hitbox)
        {
            Gizmos.color = Color.cyan;
            var c = hitbox.transform.position;
            var e = WorldExtents(hitbox);
            Gizmos.DrawWireCube(c, e * 2f);
        }
    }
}
