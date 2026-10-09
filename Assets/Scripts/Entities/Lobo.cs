using UnityEngine;

public enum EstadoLobo
{
    Patrullando,
    Rastreando,
    Persiguiendo,
    Atacando,
    Descansando,
    Muerto
}

/// <summary>
/// Lobo: patrulla el bosque, caza aldeanos y NUNCA entra a la aldea.
/// </summary>
public class Lobo : Agent
{
    public EstadoLobo estadoLobo = EstadoLobo.Patrullando;
    public float hambre = 50f;
    public float hambreMaxima = 100f;
    public float danio = 25f;
    public float radioAtaque = 0.6f;

    Aldeano presa;
    float tiempoDescanso;
    float cooldownAtaque;

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
        ElegirPuntoPatrulla();
    }

    void AplicarConfig()
    {
        var cfg = global::Simulate.Instancia;
        if (cfg == null) return;

        vidaMaxima = cfg.loboVidaMaxima;
        vida = vidaMaxima;
        hambreMaxima = cfg.loboHambreMaxima;
        speed = cfg.loboVelocidad;
        visionRange = cfg.loboRadioDeteccion;
        danio = cfg.loboDanio;
        radioAtaque = cfg.loboRadioAtaque;
    }

    public override void Simulate(float h)
    {
        if (!isAlive || estadoLobo == EstadoLobo.Muerto) return;
        this.h = h;

        var cfg = global::Simulate.Instancia;
        hambre = Mathf.Min(hambreMaxima, hambre + (cfg != null ? cfg.loboHambrePorTick : 1.5f) * h);

        EvaluarEstado();
        EjecutarEstado();
        PintarPorEstado();
    }

    void EvaluarEstado()
    {
        if (estadoLobo == EstadoLobo.Descansando || estadoLobo == EstadoLobo.Muerto)
            return;

        presa = BuscarPresaValida();
        if (presa == null)
        {
            if (estadoLobo != EstadoLobo.Patrullando)
                estadoLobo = EstadoLobo.Patrullando;
            return;
        }

        float dist = Vector2.Distance(transform.position, presa.transform.position);
        if (dist <= radioAtaque)
            estadoLobo = EstadoLobo.Atacando;
        else if (dist <= visionRange * 0.6f)
            estadoLobo = EstadoLobo.Persiguiendo;
        else
            estadoLobo = EstadoLobo.Rastreando;
    }

    Aldeano BuscarPresaValida()
    {
        Aldeano candidata = BuscarMasCercano<Aldeano>(visionRange);
        if (candidata == null) return null;

        // No persigue si el aldeano está en la aldea
        if (Aldea.Instancia != null && Aldea.Instancia.EstaDentro(candidata.transform.position))
            return null;

        return candidata;
    }

    void EjecutarEstado()
    {
        var cfg = global::Simulate.Instancia;
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
                MoverConRestriccionAldea(estadoLobo == EstadoLobo.Persiguiendo ? velPersecucion : vel);
                break;

            case EstadoLobo.Atacando:
                if (presa == null || !presa.isAlive)
                {
                    estadoLobo = EstadoLobo.Patrullando;
                    break;
                }

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
                    cooldownAtaque = 0.7f;
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

    void ElegirPuntoPatrulla()
    {
        // Sesgo al bosque
        if (MapaZonas.Instancia != null && MapaZonas.Instancia.IntentarPuntoAleatorio(TipoZona.Bosque, out Vector3 puntoBosque))
        {
            destination = puntoBosque;
            return;
        }

        SelectNewDestination();
        CorregirDestinoFueraDeAldea();
    }

    void MoverConRestriccionAldea(float velocidad)
    {
        CorregirDestinoFueraDeAldea();
        Vector3 anterior = transform.position;
        MoverHaciaDestino(velocidad);

        // Si el movimiento entró a la aldea, revertir y elegir otro destino
        if (MapaZonas.Instancia != null && !MapaZonas.Instancia.PosicionPermitidaParaLobo(transform.position))
        {
            transform.position = anterior;
            ElegirPuntoPatrulla();
        }
    }

    void CorregirDestinoFueraDeAldea()
    {
        if (MapaZonas.Instancia != null && !MapaZonas.Instancia.PosicionPermitidaParaLobo(destination))
        {
            if (MapaZonas.Instancia.IntentarPuntoAleatorio(TipoZona.Bosque, out Vector3 p))
                destination = p;
            else if (MapaZonas.Instancia.IntentarPuntoAleatorio(TipoZona.Camino, out Vector3 c))
                destination = c;
        }
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
