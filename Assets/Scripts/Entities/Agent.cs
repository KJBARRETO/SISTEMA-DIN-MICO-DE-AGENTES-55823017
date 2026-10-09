using UnityEngine;

// Estados genéricos del esqueleto (las subclases usan los suyos)
public enum AgentState
{
    Exploring,
    Seeking,
    Acting,
    Fleeing
}

// Clase base: lo que comparten aldeano y lobo (moverse, ver, vida, morir)
public class Agent : MonoBehaviour
{
    [Header("Agent Settings")]
    public float energy = 100f;
    public float age = 0f;
    public float maxAge = 9999f;
    public float speed = 1f;
    public float visionRange = 5f;
    public float vida = 100f;
    public float vidaMaxima = 100f;

    [Header("Agent States")]
    public bool isAlive = true;
    public AgentState currentState = AgentState.Exploring;

    protected Vector3 destination; // hacia dónde se mueve
    protected float h;             // duración del tick actual
    protected SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        destination = transform.position;
    }

    protected virtual void Start()
    {
        destination = transform.position;
    }

    // Cada entidad redefine esto; Simulate.cs lo llama en cada tick
    public virtual void Simulate(float h)
    {
        if (!isAlive) return;
        this.h = h;
    }

    // Camina hacia destination
    protected void MoverHaciaDestino(float velocidadActual)
    {
        Vector3 siguiente = Vector3.MoveTowards(transform.position, destination, velocidadActual * h);

        Vector2 dir = (siguiente - transform.position);
        if (dir.sqrMagnitude > 0.0001f)
        {
            // Si hay una pared, cambia de rumbo
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir.normalized, velocidadActual * h + 0.1f, LayerMask.GetMask("Obstacles"));
            if (hit.collider != null)
            {
                SelectNewDestination();
                return;
            }
        }

        transform.position = siguiente;

        // Voltea el sprite según si va a la izquierda o derecha
        if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
            spriteRenderer.flipX = dir.x < 0f;
    }

    // Escoge un punto aleatorio cerca (para patrullar)
    protected void SelectNewDestination()
    {
        Vector3 direction = new Vector3(
            Random.Range(-visionRange, visionRange),
            Random.Range(-visionRange, visionRange),
            0f
        );

        Vector3 targetPoint = transform.position + direction;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, visionRange, LayerMask.GetMask("Obstacles"));

        if (hit.collider != null)
        {
            float offset = transform.localScale.magnitude * 0.5f;
            destination = (Vector3)hit.point - direction.normalized * offset;
        }
        else
        {
            destination = targetPoint;
        }
    }

    protected bool LlegoAlDestino(float umbral = 0.2f)
    {
        return Vector2.Distance(transform.position, destination) <= umbral;
    }

    // Busca el objeto más cercano de un tipo (ej. Lobo, Aldeano)
    protected T BuscarMasCercano<T>(float rango) where T : Component
    {
        T[] todos = FindObjectsByType<T>(FindObjectsSortMode.None);
        T mejor = null;
        float minDist = float.MaxValue;

        foreach (T candidato in todos)
        {
            if (candidato == null || candidato.gameObject == gameObject) continue;

            Agent otro = candidato as Agent;
            if (otro != null && !otro.isAlive) continue;

            float dist = Vector2.Distance(transform.position, candidato.transform.position);
            if (dist <= rango && dist < minDist)
            {
                minDist = dist;
                mejor = candidato;
            }
        }

        return mejor;
    }

    protected Collider2D FindNearest(string layerName)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange, LayerMask.GetMask(layerName));
        Collider2D nearest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = hit;
            }
        }

        return nearest;
    }

    public virtual void RecibirDanio(float danio, Agent atacante = null)
    {
        if (!isAlive) return;
        vida -= danio;
        if (vida <= 0f)
            Morir(atacante);
    }

    public virtual void Morir(Agent causante = null)
    {
        if (!isAlive) return;
        isAlive = false;
        vida = 0f;
        RegistroEventos.Agregar($"{name} murió" + (causante != null ? $" por {causante.name}" : ""));
        Destroy(gameObject);
    }

    // Solo se ve en el editor al seleccionar el objeto
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(destination, 0.15f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, destination);
    }
}
