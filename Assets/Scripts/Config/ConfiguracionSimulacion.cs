using UnityEngine;

/// <summary>
/// Parámetros globales de la simulación (Caso 3: Aldea, Lobos y Bosque).
/// Asigna una instancia en la escena o usa el Singleton en tiempo de ejecución.
/// </summary>
public class ConfiguracionSimulacion : MonoBehaviour
{
    public static ConfiguracionSimulacion Instancia { get; private set; }

    [Header("General")]
    [Tooltip("Semilla del Random. Si es < 0, no se fija (no determinista).")]
    public int semillaAleatoria = 42;

    [Tooltip("Duración de cada tick de simulación en segundos.")]
    public float segundosPorIteracion = 0.05f;

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

    [Header("Árbol / Bosque")]
    public float arbolMaderaMaxima = 20f;
    public float arbolTasaRegeneracion = 0.4f;
    public float arbolUmbralDisponible = 1f;

    [Header("Aldea")]
    public float aldeaRadio = 4f;
    public float aldeaMaderaInicial = 0f;

    [Header("Spawn de lobos")]
    public float spawnIntervaloLobos = 8f;
    public int spawnMaxLobos = 6;
    public float spawnProbabilidadBase = 0.35f;

    [Header("Victoria / Fracaso")]
    [Tooltip("Madera en almacén necesaria para ganar.")]
    public float maderaObjetivoVictoria = 80f;

    [Tooltip("Tiempo de supervivencia (segundos de simulación) para ganar.")]
    public float tiempoSupervivenciaVictoria = 180f;

    [Tooltip("Madera mínima considerada 'suficiente' si no quedan aldeanos.")]
    public float maderaMinimaSupervivencia = 5f;

    [Header("Memoria de peligro (emergente)")]
    public float memoriaPeligroRadio = 3f;
    public float memoriaPeligroDuracion = 40f;
    public float memoriaPeligroPenalizacion = 8f;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        AplicarSemilla();
    }

    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }

    /// <summary>
    /// Fija la semilla si semillaAleatoria >= 0 (reproducibilidad).
    /// </summary>
    public void AplicarSemilla()
    {
        if (semillaAleatoria >= 0)
            Random.InitState(semillaAleatoria);
    }
}
