using System.Collections.Generic;
using UnityEngine;

// Estados del aldeano
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

// Habitante de la aldea: junta madera y huye de los lobos
public class Aldeano : Agent
{
    public EstadoAldeano estadoAldeano = EstadoAldeano.Descansando;
    public float maderaCargada = 0f;
    public float capacidadCarga = 10f;
    public float maderaPorTick = 1f;

    float temporizadorRefugio; // cuándo debe volver a refugiarse
    float tiempoEnRefugio;
    Arbol arbolObjetivo;
    Lobo loboAmenaza;

    // Recuerda zonas peligrosas (dinámica emergente)
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

    // Copia valores desde Simulate
    void AplicarConfig()
    {
        var cfg = ConfigSim.Actual;
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

    // Borra recuerdos viejos
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

    // Si ve un lobo cerca, huye (en la aldea está seguro)
    void DetectarPeligro()
    {
        if (Aldea.Instancia != null && Aldea.Instancia.EstaDentro(transform.position))
        {
            loboAmenaza = null;
            return;
        }

        loboAmenaza = BuscarMasCercano<Lobo>(visionRange);
        if (loboAmenaza != null && estadoAldeano != EstadoAldeano.Huyendo && estadoAldeano != EstadoAldeano.EnRefugio)
        {
            estadoAldeano = EstadoAldeano.Huyendo;
            RegistroEventos.Agregar($"{name} detectó a {loboAmenaza.name} y huye");
        }
    }

    // Decide si ya toca volver a la aldea
    void EvaluarEstado()
    {
        var cfg = ConfigSim.Actual;

        if (estadoAldeano == EstadoAldeano.Huyendo || estadoAldeano == EstadoAldeano.EnRefugio || estadoAldeano == EstadoAldeano.Muerto)
            return;

        temporizadorRefugio -= h;
        if (temporizadorRefugio <= 0f || maderaCargada >= capacidadCarga)
        {
            estadoAldeano = EstadoAldeano.Regresando;
            IrAAldea();
        }
    }

    // Hace lo que diga el estado actual
    void EjecutarEstado()
    {
        var cfg = ConfigSim.Actual;
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
                else if (maderaCargada > 0f)
                {
                    // No queda madera: vuelve con lo que lleva
                    estadoAldeano = EstadoAldeano.Regresando;
                    IrAAldea();
                }
                break;

            case EstadoAldeano.Recolectando:
                if (arbolObjetivo == null)
                {
                    if (maderaCargada > 0f)
                    {
                        estadoAldeano = EstadoAldeano.Regresando;
                        IrAAldea();
                    }
                    else
                    {
                        estadoAldeano = EstadoAldeano.YendoAlBosque;
                        ElegirArbol();
                    }
                    break;
                }

                float faltante = capacidadCarga - maderaCargada;
                float extraido = arbolObjetivo.Extraer(Mathf.Min(maderaPorTick * h, faltante));
                maderaCargada += extraido;

                // Si se acabó la madera, el objeto desaparece
                bool seAcabo = arbolObjetivo == null || !arbolObjetivo.TieneMaderaDisponible();
                if (seAcabo)
                    arbolObjetivo = null;

                if (maderaCargada >= capacidadCarga || seAcabo)
                {
                    if (maderaCargada > 0f)
                    {
                        estadoAldeano = EstadoAldeano.Regresando;
                        IrAAldea();
                    }
                    else
                    {
                        estadoAldeano = EstadoAldeano.YendoAlBosque;
                        ElegirArbol();
                    }
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
                // Corre a la aldea; si no hay, se aleja del lobo
                if (Aldea.Instancia != null)
                    IrAAldea();
                else if (loboAmenaza != null)
                {
                    Vector3 lejos = transform.position - loboAmenaza.transform.position;
                    if (lejos.sqrMagnitude < 0.01f) lejos = Vector3.left;
                    destination = transform.position + lejos.normalized * 5f;
                }

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

    // Deja la madera en el almacén y descansa un rato
    void DepositarYRefugiarse()
    {
        if (maderaCargada > 0f && Aldea.Instancia != null)
        {
            Aldea.Instancia.DepositarMadera(maderaCargada);
            RegistroEventos.Agregar($"{name} depositó {maderaCargada:0.#} de madera (almacén: {Aldea.Instancia.maderaEnAlmacen:0.#})");
            maderaCargada = 0f;
        }

        var cfg = ConfigSim.Actual;
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

    // Escoge la madera más cercana (castiga las zonas peligrosas)
    void ElegirArbol()
    {
        Arbol[] arboles = FindObjectsByType<Arbol>(FindObjectsSortMode.None);
        Arbol mejor = null;
        float mejorScore = float.MaxValue;
        var cfg = ConfigSim.Actual;
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

    // Guarda dónde lo atacaron para no volver tan fácil
    public void RegistrarPeligroAqui()
    {
        var cfg = ConfigSim.Actual;
        memorias.Add(new RecuerdoPeligro
        {
            posicion = transform.position,
            tiempoRestante = cfg != null ? cfg.memoriaPeligroDuracion : 40f
        });
    }

    public override void RecibirDanio(float danio, Agent atacante = null)
    {
        if (!isAlive) return;

        // Dentro de la aldea no le hacen daño
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
        var cfg = ConfigSim.Actual;
        float consumo = cfg != null ? cfg.aldeanoConsumoEnergiaPorTick : 0.5f;
        if (estadoAldeano != EstadoAldeano.EnRefugio)
            energy -= consumo * dt;

        if (energy <= 0f)
        {
            energy = 0f;
            // Sin energía vuelve a refugiarse (no muere de una)
            if (estadoAldeano != EstadoAldeano.EnRefugio && estadoAldeano != EstadoAldeano.Huyendo)
            {
                estadoAldeano = EstadoAldeano.Regresando;
                IrAAldea();
            }
        }
    }

    // Cambia el color según lo que esté haciendo (para verlo fácil)
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
