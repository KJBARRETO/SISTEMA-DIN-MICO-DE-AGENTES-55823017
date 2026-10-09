using UnityEngine;
 
// Estados del lobo
public enum EstadoLobo
{
    Patrullando,
    Rastreando,
    Persiguiendo,
    Atacando,
    Descansando,
    Muerto
}
 
// Depredador: caza aldeanos en el bosque y no entra a la aldea
public class Lobo : Agent
{
    public EstadoLobo estadoLobo = EstadoLobo.Patrullando;
    public float hambre = 50f;
    public float hambreMaxima = 100f;
    public float danio = 25f;
    public float radioAtaque = 0.6f;
 
    Aldeano presa;
    float tiempoDescanso;
    float cooldownAtaque; // para no pegar todos los frames
 
    Vector3 ultimaPosicionSegura; // última posición fuera de la aldea
    bool tienePosicionSegura;
 
    static int contadorId;
    int id;
 
    protected override void Awake()
    {
        base.Awake();
        id = ++contadorId;
        name = $"Lobo_{id}";
    }
 
    protected override void Start()
    {
        base.Start();
        AplicarConfig();
        ExpulsarDeAldea(); // por si aparece dentro de la aldea
        ElegirPuntoPatrulla();
    }
 
    void AplicarConfig()
    {
        var cfg = ConfigSim.Actual;
        if (cfg == null) return;
 
        vidaMaxima = cfg.loboVidaMaxima;
        vida = vidaMaxima;
        hambreMaxima = cfg.loboHambreMaxima;
        // CAMBIO: empieza con hambre suficiente para cazar desde el inicio
        hambre = cfg.loboHambreMinimaParaCazar + 5f;
        speed = cfg.loboVelocidad;
        visionRange = cfg.loboRadioDeteccion;
        danio = cfg.loboDanio;
        radioAtaque = cfg.loboRadioAtaque;
    }
 
    public override void Simulate(float h)
    {
        if (!isAlive || estadoLobo == EstadoLobo.Muerto) return;
        this.h = h;
 
        // El hambre sube con el tiempo
        var cfg = ConfigSim.Actual;
        hambre = Mathf.Min(hambreMaxima, hambre + (cfg != null ? cfg.loboHambrePorTick : 1.5f) * h);
 
        EvaluarEstado();
        EjecutarEstado();
        PintarPorEstado();
    }
 
    // Decide si patrulla, persigue o ataca
    void EvaluarEstado()
    {
        if (estadoLobo == EstadoLobo.Descansando || estadoLobo == EstadoLobo.Muerto)
            return;
 
        // Con poca hambre solo camina (da tiempo a los aldeanos)
        var cfg = ConfigSim.Actual;
        float umbralHambre = cfg != null ? cfg.loboHambreMinimaParaCazar : 45f;
        if (hambre < umbralHambre)
        {
            presa = null;
            estadoLobo = EstadoLobo.Patrullando;
            return;
        }
 
        presa = BuscarPresaValida();
        if (presa == null)
        {
            estadoLobo = EstadoLobo.Patrullando;
            return;
        }
 
        float dist = Vector2.Distance(transform.position, presa.transform.position);
        if (dist <= radioAtaque)
            estadoLobo = EstadoLobo.Atacando;
        else if (dist <= visionRange * 0.55f)
            estadoLobo = EstadoLobo.Persiguiendo;
        else
            estadoLobo = EstadoLobo.Rastreando;
    }
 
    // Busca un aldeano que no esté en la aldea
    Aldeano BuscarPresaValida()
    {
        Aldeano candidata = BuscarMasCercano<Aldeano>(visionRange);
        if (candidata == null) return null;
 
        if (Aldea.Instancia != null && Aldea.Instancia.EstaDentro(candidata.transform.position))
            return null;
 
        return candidata;
    }
 
    void EjecutarEstado()
    {
        var cfg = ConfigSim.Actual;
        float vel = speed;
        float velPersecucion = cfg != null ? cfg.loboVelocidadPersiguiendo : speed * 1.3f;
 
        switch (estadoLobo)
        {
            case EstadoLobo.Patrullando:
                if (LlegoAlDestino(0.4f))
                    ElegirPuntoPatrulla();
                MoverConRestriccionAldea(vel);
                break;
 
            case EstadoLobo.Rastreando:
            case EstadoLobo.Persiguiendo:
                if (presa == null)
                {
                    estadoLobo = EstadoLobo.Patrullando;
                    break;
                }
                destination = presa.transform.position;
                // CAMBIO: siempre a velocidad de persecución cuando hay presa
                MoverConRestriccionAldea(velPersecucion);
                break;
 
            case EstadoLobo.Atacando:
                if (presa == null || !presa.isAlive)
                {
                    estadoLobo = EstadoLobo.Patrullando;
                    break;
                }
 
                // Si se metió a la aldea, lo suelta
                if (Aldea.Instancia != null && Aldea.Instancia.EstaDentro(presa.transform.position))
                {
                    presa = null;
                    estadoLobo = EstadoLobo.Patrullando;
                    break;
                }
 
                float dist = Vector2.Distance(transform.position, presa.transform.position);
                if (dist > radioAtaque)
                {
                    estadoLobo = EstadoLobo.Persiguiendo;
                    break;
                }
 
                cooldownAtaque -= h;
                if (cooldownAtaque <= 0f)
                {
                    presa.RecibirDanio(danio, this);
                    cooldownAtaque = 1.2f;
                }
 
                if (presa == null || !presa.isAlive)
                {
                    hambre = cfg != null ? cfg.loboHambreTrasCazar : 20f;
                    tiempoDescanso = cfg != null ? cfg.loboDuracionDescanso : 4f;
                    estadoLobo = EstadoLobo.Descansando;
                    presa = null;
                    RegistroEventos.Agregar($"{name} cazó y descansa");
                }
                break;
 
            case EstadoLobo.Descansando:
                tiempoDescanso -= h;
                if (tiempoDescanso <= 0f)
                {
                    estadoLobo = EstadoLobo.Patrullando;
                    ElegirPuntoPatrulla();
                }
                break;
        }
    }
 
    // Prefiere caminar por el bosque
    void ElegirPuntoPatrulla()
    {
        if (MapaZonas.Instancia != null && MapaZonas.Instancia.IntentarPuntoAleatorio(TipoZona.Bosque, out Vector3 puntoBosque))
        {
            destination = puntoBosque;
            return;
        }
 
        SelectNewDestination();
        CorregirDestinoFueraDeAldea();
    }
 
    // Se mueve, pero si toca la aldea se echa para atrás
    void MoverConRestriccionAldea(float velocidad)
    {
        CorregirDestinoFueraDeAldea();
        Vector3 anterior = transform.position;
        MoverHaciaDestino(velocidad);
 
        // CAMBIO: si el paso lo mete en la aldea, se cancela
        if (PosicionProhibida(transform.position))
        {
            transform.position = anterior;
            ElegirPuntoPatrulla();
        }
    }
 
    // Si el destino cae en la aldea, lo cambia
    void CorregirDestinoFueraDeAldea()
    {
        if (MapaZonas.Instancia != null && PosicionProhibida(destination))
        {
            if (MapaZonas.Instancia.IntentarPuntoAleatorio(TipoZona.Bosque, out Vector3 p))
                destination = p;
            else if (MapaZonas.Instancia.IntentarPuntoAleatorio(TipoZona.Camino, out Vector3 c))
                destination = c;
        }
    }
 
    // Aldea o zona no permitida para el lobo
    bool PosicionProhibida(Vector3 pos)
    {
        bool enAldea = Aldea.Instancia != null && Aldea.Instancia.EstaDentro(pos);
        bool zonaNoPermitida = MapaZonas.Instancia != null && !MapaZonas.Instancia.PosicionPermitidaParaLobo(pos);
        return enAldea || zonaNoPermitida;
    }
 
    // Seguro final: pase lo que pase en el frame, el lobo nunca termina dentro de la aldea
    void LateUpdate()
    {
        if (!isAlive) return;
 
        if (PosicionProhibida(transform.position))
        {
            if (tienePosicionSegura)
                transform.position = ultimaPosicionSegura;
            else
                ExpulsarDeAldea();
 
            ElegirPuntoPatrulla();
        }
        else
        {
            ultimaPosicionSegura = transform.position;
            tienePosicionSegura = true;
        }
    }
 
    // Saca al lobo de la aldea empujándolo hacia afuera desde el centro
    void ExpulsarDeAldea()
    {
        if (Aldea.Instancia == null || !PosicionProhibida(transform.position)) return;
 
        Vector3 dir = transform.position - Aldea.Instancia.PuntoRefugio();
        dir.z = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.right;
        dir.Normalize();
 
        Vector3 pos = transform.position;
        for (int i = 0; i < 100 && PosicionProhibida(pos); i++)
            pos += dir * 0.5f;
 
        transform.position = pos;
    }
 
    void PintarPorEstado()
    {
        if (spriteRenderer == null) return;
        switch (estadoLobo)
        {
            case EstadoLobo.Persiguiendo:
            case EstadoLobo.Atacando: spriteRenderer.color = new Color(1f, 0.5f, 0.5f); break;
            case EstadoLobo.Descansando: spriteRenderer.color = new Color(0.7f, 0.7f, 0.7f); break;
            default: spriteRenderer.color = Color.white; break;
        }
    }
}
 