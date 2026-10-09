using System.Collections.Generic;
using UnityEngine;

// Ayuda a saber en qué zona está cada cosa.
// Si se solapan: gana Aldea, luego Bosque, luego Camino.
public class MapaZonas : MonoBehaviour
{
    public static MapaZonas Instancia { get; private set; }
    public List<Zona> zonas = new List<Zona>();

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        RefrescarZonas();
    }

    void Start()
    {
        // Por si alguna zona se creó después del Awake
        RefrescarZonas();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // Busca todas las Zona de la escena
    public void RefrescarZonas()
    {
        zonas = new List<Zona>(FindObjectsByType<Zona>(FindObjectsSortMode.InstanceID));
    }

    public Zona ObtenerZona(Vector3 posicion)
    {
        Zona aldea = null, bosque = null, camino = null;
        foreach (Zona zona in zonas)
        {
            if (zona == null || !zona.Contiene(posicion)) continue;
            if (zona.tipo == TipoZona.Aldea) aldea = zona;
            else if (zona.tipo == TipoZona.Bosque) bosque = zona;
            else camino = zona;
        }
        return aldea != null ? aldea : (bosque != null ? bosque : camino);
    }

    public TipoZona ObtenerTipoZona(Vector3 posicion)
    {
        Zona z = ObtenerZona(posicion);
        return z != null ? z.tipo : TipoZona.Camino;
    }

    public bool EstaEnAldea(Vector3 posicion) => ObtenerTipoZona(posicion) == TipoZona.Aldea;

    // Los lobos usan esto para no meterse a la aldea
    public bool PosicionPermitidaParaLobo(Vector3 posicion) => !EstaEnAldea(posicion);

    public Zona ObtenerPrimeraZona(TipoZona tipo)
    {
        foreach (Zona zona in zonas)
            if (zona != null && zona.tipo == tipo) return zona;
        return null;
    }

    // Punto random dentro de una zona (para spawn / patrulla)
    public bool IntentarPuntoAleatorio(TipoZona tipo, out Vector3 punto)
    {
        Zona zona = ObtenerPrimeraZona(tipo);
        if (zona == null)
        {
            punto = Vector3.zero;
            return false;
        }
        Vector2 offset = Random.insideUnitCircle * zona.radio * 0.85f;
        punto = zona.transform.position + new Vector3(offset.x, offset.y, 0f);
        return true;
    }
}
