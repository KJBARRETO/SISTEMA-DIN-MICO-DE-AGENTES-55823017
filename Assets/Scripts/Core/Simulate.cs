using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mensajes solo en Consola.
/// </summary>
public static class RegistroEventos
{
    public static void Agregar(string mensaje) => Debug.Log($"[{Time.time:0.0}s] {mensaje}");
    public static void Limpiar() { }
}

/// <summary>
/// Orquestador + parámetros de la simulación (Caso 3).
/// Todas las entidades se actualizan solo desde aquí.
/// </summary>
public class Simulate : MonoBehaviour
{
    public static Simulate Instancia { get; private set; }

    [Header("General")]
    public int semillaAleatoria = 42;
    public float secondsPerIteration = 0.05f;
    public bool ended = false;
    public float tiempoSimulado = 0f;
    public string mensajeFinal = "";

    [Header("Aldeano")]
    public float aldeanoVidaMaxima = 100f;
    public float aldeanoEnergiaMaxima = 100f;
    public float aldeanoVelocidad = 2f;
    public float aldeanoVelocidadHuyendo = 3.2f;
    public float aldeanoRadioVision = 4f;
    public float aldeanoCapacidadCarga = 10f;
    public float aldeanoMaderaPorTick = 1f;
    public float aldeanoConsumoEnergiaPorTick = 0.5f;
    public float aldeanoTiempoEntreRefugios = 25f;
    public float aldeanoDuracionRefugio = 3f;

    [Header("Lobo")]
    public float loboVidaMaxima = 80f;
    public float loboHambreMaxima = 100f;
    public float loboHambrePorTick = 1.5f;
    public float loboVelocidad = 2.8f;
    public float loboVelocidadPersiguiendo = 3.5f;
    public float loboRadioDeteccion = 5f;
    public float loboRadioAtaque = 0.6f;
    public float loboDanio = 25f;
    public float loboDuracionDescanso = 4f;
    public float loboHambreTrasCazar = 20f;

    [Header("Árbol")]
    public float arbolMaderaMaxima = 20f;
    public float arbolTasaRegeneracion = 0.4f;
    public float arbolUmbralDisponible = 1f;

    [Header("Aldea")]
    public float aldeaRadio = 4f;
    public float aldeaMaderaInicial = 0f;

    [Header("Spawn lobos")]
    public GameObject prefabLobo;
    public float spawnIntervaloLobos = 8f;
    public int spawnMaxLobos = 6;
    public float spawnProbabilidadBase = 0.35f;

    [Header("Victoria / Fracaso")]
    public float maderaObjetivoVictoria = 40f;
    public float tiempoSupervivenciaVictoria = 120f;
    public float maderaMinimaSupervivencia = 5f;

    [Header("Memoria de peligro")]
    public float memoriaPeligroRadio = 3f;
    public float memoriaPeligroDuracion = 40f;
    public float memoriaPeligroPenalizacion = 8f;

    public List<Agent> agents = new List<Agent>();
    public List<Arbol> arboles = new List<Arbol>();

    float time;
    float tiempoSpawnLobos;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        if (semillaAleatoria >= 0)
            Random.InitState(semillaAleatoria);
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        RefrescarListas();
        RegistroEventos.Agregar("Simulación iniciada (Caso 3: Aldea, Lobos y Bosque)");
        ActualizarPoblacionAldea();
    }

    void Update()
    {
        if (ended) return;
        time += Time.deltaTime;
        if (time >= secondsPerIteration)
        {
            time = 0f;
            RunTick();
        }
    }

    void RunTick()
    {
        RefrescarListas();

        foreach (Arbol arbol in arboles)
            if (arbol != null) arbol.Simulate(secondsPerIteration);

        List<Agent> copia = new List<Agent>(agents);
        foreach (Agent agent in copia)
            if (agent != null && agent.isAlive)
                agent.Simulate(secondsPerIteration);

        SpawnLobosBosque(secondsPerIteration);

        tiempoSimulado += secondsPerIteration;
        ActualizarPoblacionAldea();
        EvaluarFin();
    }

    void RefrescarListas()
    {
        agents.Clear();
        foreach (Agent a in FindObjectsByType<Agent>(FindObjectsSortMode.InstanceID))
            if (a != null && a.isAlive) agents.Add(a);

        arboles.Clear();
        foreach (Arbol a in FindObjectsByType<Arbol>(FindObjectsSortMode.InstanceID))
            if (a != null) arboles.Add(a);
    }

    void SpawnLobosBosque(float h)
    {
        if (prefabLobo == null) return;

        tiempoSpawnLobos += h;
        if (tiempoSpawnLobos < spawnIntervaloLobos) return;
        tiempoSpawnLobos = 0f;

        if (ContarVivos<Lobo>() >= spawnMaxLobos) return;

        float bonus = Mathf.Clamp01(ContarVivos<Aldeano>() / 5f) * 0.25f;
        if (Random.value > spawnProbabilidadBase + bonus) return;

        if (MapaZonas.Instancia == null ||
            !MapaZonas.Instancia.IntentarPuntoAleatorio(TipoZona.Bosque, out Vector3 punto) ||
            !MapaZonas.Instancia.PosicionPermitidaParaLobo(punto))
            return;

        GameObject go = Instantiate(prefabLobo, punto, Quaternion.identity);
        RegistroEventos.Agregar($"Spawn de lobo en el bosque ({go.name})");
    }

    int ContarVivos<T>() where T : Agent
    {
        int n = 0;
        foreach (Agent a in agents)
            if (a is T && a != null && a.isAlive) n++;
        return n;
    }

    void ActualizarPoblacionAldea()
    {
        if (Aldea.Instancia != null)
            Aldea.Instancia.ActualizarPoblacion(ContarVivos<Aldeano>());
    }

    void EvaluarFin()
    {
        int aldeanos = ContarVivos<Aldeano>();
        float madera = Aldea.Instancia != null ? Aldea.Instancia.maderaEnAlmacen : 0f;

        if (madera >= maderaObjetivoVictoria)
        {
            Finalizar($"VICTORIA: almacén alcanzó {madera:0.#} de madera");
            return;
        }

        if (tiempoSimulado >= tiempoSupervivenciaVictoria && aldeanos > 0)
        {
            Finalizar($"VICTORIA: la aldea sobrevivió {tiempoSimulado:0.#}s");
            return;
        }

        if (aldeanos <= 0)
        {
            Finalizar(madera < maderaMinimaSupervivencia
                ? "FRACASO: no quedan aldeanos ni madera suficiente"
                : "FRACASO: todos los aldeanos murieron");
        }
    }

    void Finalizar(string mensaje)
    {
        mensajeFinal = mensaje;
        ended = true;
        RegistroEventos.Agregar(mensaje);
        Debug.LogWarning(mensaje);
    }

    public void End() => ended = true;
}
