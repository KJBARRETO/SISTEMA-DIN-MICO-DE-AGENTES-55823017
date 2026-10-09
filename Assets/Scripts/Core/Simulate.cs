using System.Collections.Generic;
using UnityEngine;

// Escribe mensajes en la Consola de Unity
public static class RegistroEventos
{
    public static void Agregar(string mensaje) => Debug.Log($"[{Time.time:0.0}s] {mensaje}");
    public static void Limpiar() { }
}

// Atajo para leer la config sin chocar con el método Simulate()
public static class ConfigSim
{
    public static Simulate Actual => Simulate.Instancia;
}

// Controla toda la simulación (Caso 3: Aldea, Lobos y Bosque).
// Desde aquí se llama Simulate() de cada entidad. Nadie se actualiza solo.
public class Simulate : MonoBehaviour
{
    public static Simulate Instancia { get; private set; }

    [Header("General")]
    public int semillaAleatoria = 42;          // para poder repetir la misma corrida
    public float secondsPerIteration = 0.05f;  // duración de cada tick
    public bool ended = false;
    public float tiempoSimulado = 0f;
    public string mensajeFinal = "";

    [Header("Aldeano")]
    public float aldeanoVidaMaxima = 100f;
    public float aldeanoEnergiaMaxima = 100f;
    public float aldeanoVelocidad = 2.4f;
    public float aldeanoVelocidadHuyendo = 4.8f; // más rápido que el lobo
    public float aldeanoRadioVision = 5f;
    public float aldeanoCapacidadCarga = 6f;
    public float aldeanoMaderaPorTick = 5f;
    public float aldeanoConsumoEnergiaPorTick = 0.25f;
    public float aldeanoTiempoEntreRefugios = 18f;
    public float aldeanoDuracionRefugio = 2f;

    [Header("Lobo")]
    public float loboVidaMaxima = 80f;
    public float loboHambreMaxima = 100f;
    public float loboHambrePorTick = 0.5f;
    public float loboVelocidad = 1.8f;
    public float loboVelocidadPersiguiendo = 2.6f;
    public float loboRadioDeteccion = 3f;
    public float loboRadioAtaque = 0.5f;
    public float loboDanio = 10f;
    public float loboDuracionDescanso = 8f;
    public float loboHambreTrasCazar = 10f;
    public float loboHambreMinimaParaCazar = 60f; // si tiene poca hambre, solo patrulla

    [Header("Árbol")]
    public float arbolMaderaMaxima = 20f;
    public float arbolTasaRegeneracion = 0.4f;
    public float arbolUmbralDisponible = 1f;

    [Header("Aldea")]
    public float aldeaRadio = 4f;
    public float aldeaMaderaInicial = 0f;

    [Header("Spawn lobos")]
    public GameObject prefabLobo;
    public float spawnIntervaloLobos = 12f;
    public int spawnMaxLobos = 4;
    public float spawnProbabilidadBase = 1f;

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
        // Solo una instancia de Simulate
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

        // Si no hay lobos al inicio, el primero sale más pronto
        if (ContarVivos<Lobo>() == 0)
            tiempoSpawnLobos = spawnIntervaloLobos - 8f;
    }

    void Update()
    {
        if (ended) return;

        // Cada cierto tiempo corre un tick de la simulación
        time += Time.deltaTime;
        if (time >= secondsPerIteration)
        {
            time = 0f;
            RunTick();
        }
    }

    // Un ciclo completo: recursos → aldeanos → lobos → spawn → reglas
    void RunTick()
    {
        RefrescarListas();

        foreach (Arbol arbol in arboles)
            if (arbol != null) arbol.Simulate(secondsPerIteration);

        // Primero aldeanos, luego lobos (así pueden huir en el mismo tick)
        List<Agent> copia = new List<Agent>(agents);
        foreach (Agent agent in copia)
            if (agent is Aldeano && agent != null && agent.isAlive)
                agent.Simulate(secondsPerIteration);

        foreach (Agent agent in copia)
            if (agent is Lobo && agent != null && agent.isAlive)
                agent.Simulate(secondsPerIteration);

        SpawnLobosBosque(secondsPerIteration);

        tiempoSimulado += secondsPerIteration;
        ActualizarPoblacionAldea();
        EvaluarFin();
    }

    // Vuelve a buscar quién sigue vivo en la escena
    void RefrescarListas()
    {
        agents.Clear();
        foreach (Agent a in FindObjectsByType<Agent>(FindObjectsSortMode.InstanceID))
            if (a != null && a.isAlive) agents.Add(a);

        arboles.Clear();
        foreach (Arbol a in FindObjectsByType<Arbol>(FindObjectsSortMode.InstanceID))
            if (a != null) arboles.Add(a);
    }

    // Crea lobos en el bosque de vez en cuando
    void SpawnLobosBosque(float h)
    {
        if (prefabLobo == null) return;

        tiempoSpawnLobos += h;
        if (tiempoSpawnLobos < spawnIntervaloLobos) return;

        if (ContarVivos<Lobo>() >= spawnMaxLobos)
        {
            tiempoSpawnLobos = 0f;
            return;
        }

        if (Random.value > spawnProbabilidadBase)
            return;

        // Intenta spawnear en el bosque; si no hay zona, usa un punto a la derecha
        Vector3 punto = new Vector3(Random.Range(5f, 9f), Random.Range(-3f, 3f), 0f);
        if (MapaZonas.Instancia != null)
            MapaZonas.Instancia.IntentarPuntoAleatorio(TipoZona.Bosque, out punto);

        // Nunca dentro de la aldea
        if (MapaZonas.Instancia != null && !MapaZonas.Instancia.PosicionPermitidaParaLobo(punto))
            return;

        tiempoSpawnLobos = 0f;
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

    // Revisa si ya ganamos o perdimos
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
