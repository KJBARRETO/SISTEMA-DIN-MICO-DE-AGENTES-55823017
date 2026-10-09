using System.Collections.Generic;
using UnityEngine;

public enum EstadoAldeano
{
    Descansando,
    YendoAlBosque,
    Recolectando,
    Regresando,
    EnRefugio,
    Huyendo,
    Muerto
}

/// <summary>
/// Aldeano: recolecta madera, se refugia en la aldea y huye de lobos.
/// Dinámica emergente: memoria de zonas peligrosas (evita donde lo atacaron).
/// </summary>
public class Aldeano : Agent
{
    public EstadoAldeano estadoAldeano = EstadoAldeano.Descansando;
    public float maderaCargada = 0f;
    public float capacidadCarga = 10f;
    public float maderaPorTick = 1f;

    float temporizadorRefugio;
    float tiempoEnRefugio;
    Arbol arbolObjetivo;
    Lobo loboAmenaza;

    // Memoria emergente: puntos peligrosos recientes
    struct RecuerdoPeligro
    {
        public Vector3 posicion;
        public float tiempoRestante;
    }

    readonly List<RecuerdoPeligro> memorias = new List<RecuerdoPeligro>();

    static int contadorId;
    int id;

    protected override void Awake()
    {
        base.Awake();
        id = ++contadorId;
        name = $"Aldeano_{id}";
    }

    protected override void Start()
    {
        base.Start();
        AplicarConfig();
        estadoAldeano = EstadoAldeano.YendoAlBosque;
        ElegirArbol();
    }

    void AplicarConfig()
    {
        var cfg = global::Simulate.Instancia;
        if (cfg == null) return;

        vidaMaxima = cfg.aldeanoVidaMaxima;
        vida = vidaMaxima;
        energy = cfg.aldeanoEnergiaMaxima;
        speed = cfg.aldeanoVelocidad;
        visionRange = cfg.aldeanoRadioVision;
        capacidadCarga = cfg.aldeanoCapacidadCarga;
        maderaPorTick = cfg.aldeanoMaderaPorTick;
        temporizadorRefugio = cfg.aldeanoTiempoEntreRefugios;
    }

    public override void Simulate(float h)
    {
        if (!isAlive || estadoAldeano == EstadoAldeano.Muerto) return;
        this.h = h;

        ActualizarMemorias(h);
        DetectarPeligro();
        EvaluarEstado();
        EjecutarEstado();
        ConsumirEnergia(h);
    }

    void ActualizarMemorias(float dt)
    {
        for (int i = memorias.Count - 1; i >= 0; i--)
        {
            RecuerdoPeligro r = memorias[i];
            r.tiempoRestante -= dt;
            if (r.tiempoRestante <= 0f)
                memorias.RemoveAt(i);
            else
                memorias[i] = r;
        }
    }

    void DetectarPeligro()
    {
        if (Aldea.Instancia != null && Aldea.Instancia.EstaDentro(transform.position))
        {
            loboAmenaza = null;
            return; // inmune dentro de la aldea
        }

        loboAmenaza = BuscarMasCercano<Lobo>(visionRange);
        if (loboAmenaza != null && estadoAldeano != EstadoAldeano.Huyendo && estadoAldeano != EstadoAldeano.EnRefugio)
        {
            estadoAldeano = EstadoAldeano.Huyendo;
            RegistroEventos.Agregar($"{name} detectó a {loboAmenaza.name} y huye");
        }
    }

    void EvaluarEstado()
    {
        var cfg = global::Simulate.Instancia;
        float tiempoEntre = cfg != null ? cfg.aldeanoTiempoEntreRefugios : 25f;

        if (estadoAldeano == EstadoAldeano.Huyendo || estadoAldeano == EstadoAldeano.EnRefugio || estadoAldeano == EstadoAldeano.Muerto)
            return;

        temporizadorRefugio -= h;
        if (temporizadorRefugio <= 0f || maderaCargada >= capacidadCarga)
        {
            estadoAldeano = EstadoAldeano.Regresando;
            IrAAldea();
        }
    }

    void EjecutarEstado()
    {
        var cfg = global::Simulate.Instancia;
        float vel = speed;
        float velHuida = cfg != null ? cfg.aldeanoVelocidadHuyendo : speed * 1.5f;

        switch (estadoAldeano)
        {
            case EstadoAldeano.Descansando:
            case EstadoAldeano.YendoAlBosque:
                if (arbolObjetivo == null || !arbolObjetivo.TieneMaderaDisponible())
                    ElegirArbol();
                if (arbolObjetivo != null)
                {
                    destination = arbolObjetivo.transform.position;
                    MoverHaciaDestino(vel);
                    if (LlegoAlDestino(0.5f))
                        estadoAldeano = EstadoAldeano.Recolectando;
                }
                break;

            case EstadoAldeano.Recolectando:
                if (arbolObjetivo == null || !arbolObjetivo.TieneMaderaDisponible())
                {
                    estadoAldeano = EstadoAldeano.YendoAlBosque;
                    ElegirArbol();
                    break;
                }

                float faltante = capacidadCarga - maderaCargada;
                float extraido = arbolObjetivo.Extraer(Mathf.Min(maderaPorTick * h, faltante));
                maderaCargada += extraido;

                if (maderaCargada >= capacidadCarga || !arbolObjetivo.TieneMaderaDisponible())
                {
                    estadoAldeano = EstadoAldeano.Regresando;
                    IrAAldea();
                }
                break;

            case EstadoAldeano.Regresando:
                IrAAldea();
                MoverHaciaDestino(vel);
                if (Aldea.Instancia != null && Aldea.Instancia.EstaDentro(transform.position))
                    DepositarYRefugiarse();
                break;

            case EstadoAldeano.EnRefugio:
                tiempoEnRefugio -= h;
                energy = Mathf.Min(energy + 5f * h, cfg != null ? cfg.aldeanoEnergiaMaxima : 100f);
                if (tiempoEnRefugio <= 0f)
                {
                    temporizadorRefugio = cfg != null ? cfg.aldeanoTiempoEntreRefugios : 25f;
                    estadoAldeano = EstadoAldeano.YendoAlBosque;
                    ElegirArbol();
                }
                break;

            case EstadoAldeano.Huyendo:
                IrAAldea();
                MoverHaciaDestino(velHuida);
                if (Aldea.Instancia != null && Aldea.Instancia.EstaDentro(transform.position))
                {
                    DepositarYRefugiarse();
                    RegistroEventos.Agregar($"{name} llegó a refugio");
                }
                break;
        }

        PintarPorEstado();
    }

    void DepositarYRefugiarse()
    {
        if (maderaCargada > 0f && Aldea.Instancia != null)
        {
            Aldea.Instancia.DepositarMadera(maderaCargada);
            RegistroEventos.Agregar($"{name} depositó {maderaCargada:0.#} de madera (almacén: {Aldea.Instancia.maderaEnAlmacen:0.#})");
            maderaCargada = 0f;
        }

        var cfg = global::Simulate.Instancia;
        tiempoEnRefugio = cfg != null ? cfg.aldeanoDuracionRefugio : 3f;
        estadoAldeano = EstadoAldeano.EnRefugio;
        arbolObjetivo = null;
        loboAmenaza = null;
    }

    void IrAAldea()
    {
        if (Aldea.Instancia != null)
            destination = Aldea.Instancia.PuntoRefugio();
    }

    void ElegirArbol()
    {
        Arbol[] arboles = FindObjectsByType<Arbol>(FindObjectsSortMode.None);
        Arbol mejor = null;
        float mejorScore = float.MaxValue;
        var cfg = global::Simulate.Instancia;
        float radioPeligro = cfg != null ? cfg.memoriaPeligroRadio : 3f;
        float penalizacion = cfg != null ? cfg.memoriaPeligroPenalizacion : 8f;

        foreach (Arbol a in arboles)
        {
            if (a == null || !a.TieneMaderaDisponible()) continue;

            float dist = Vector2.Distance(transform.position, a.transform.position);
            float score = dist + PenalizacionMemoria(a.transform.position, radioPeligro, penalizacion);

            if (score < mejorScore)
            {
                mejorScore = score;
                mejor = a;
            }
        }

        arbolObjetivo = mejor;
        if (arbolObjetivo != null)
        {
            destination = arbolObjetivo.transform.position;
            estadoAldeano = EstadoAldeano.YendoAlBosque;
        }
    }

    float PenalizacionMemoria(Vector3 punto, float radio, float pena)
    {
        float extra = 0f;
        foreach (RecuerdoPeligro r in memorias)
        {
            if (Vector2.Distance(punto, r.posicion) <= radio)
                extra += pena;
        }
        return extra;
    }

    public void RegistrarPeligroAqui()
    {
        var cfg = global::Simulate.Instancia;
        memorias.Add(new RecuerdoPeligro
        {
            posicion = transform.position,
            tiempoRestante = cfg != null ? cfg.memoriaPeligroDuracion : 40f
        });
    }

    public override void RecibirDanio(float danio, Agent atacante = null)
    {
        if (!isAlive) return;

        // Inmune en aldea
        if (Aldea.Instancia != null && Aldea.Instancia.EstaDentro(transform.position))
            return;

        RegistrarPeligroAqui();
        vida -= danio;
        estadoAldeano = EstadoAldeano.Huyendo;

        if (vida <= 0f)
            Morir(atacante);
    }

    public override void Morir(Agent causante = null)
    {
        if (!isAlive) return;
        isAlive = false;
        estadoAldeano = EstadoAldeano.Muerto;
        RegistrarPeligroAqui();
        RegistroEventos.Agregar($"{name} fue cazado" + (causante != null ? $" por {causante.name}" : ""));
        Destroy(gameObject);
    }

    void ConsumirEnergia(float dt)
    {
        var cfg = global::Simulate.Instancia;
        float consumo = cfg != null ? cfg.aldeanoConsumoEnergiaPorTick : 0.5f;
        if (estadoAldeano != EstadoAldeano.EnRefugio)
            energy -= consumo * dt;

        if (energy <= 0f)
        {
            energy = 0f;
            // Sin energía: vuelve a refugiarse en vez de morir al instante
            if (estadoAldeano != EstadoAldeano.EnRefugio && estadoAldeano != EstadoAldeano.Huyendo)
            {
                estadoAldeano = EstadoAldeano.Regresando;
                IrAAldea();
            }
        }
    }

    void PintarPorEstado()
    {
        if (spriteRenderer == null) return;
        switch (estadoAldeano)
        {
            case EstadoAldeano.Huyendo: spriteRenderer.color = new Color(1f, 0.45f, 0.45f); break;
            case EstadoAldeano.Recolectando: spriteRenderer.color = new Color(0.7f, 1f, 0.7f); break;
            case EstadoAldeano.EnRefugio: spriteRenderer.color = new Color(0.7f, 0.85f, 1f); break;
            case EstadoAldeano.Regresando: spriteRenderer.color = new Color(1f, 0.9f, 0.5f); break;
            default: spriteRenderer.color = Color.white; break;
        }
    }
}
