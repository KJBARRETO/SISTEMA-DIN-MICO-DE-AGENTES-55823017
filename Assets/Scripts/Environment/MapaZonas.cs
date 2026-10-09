using System.Collections.Generic;
using UnityEngine;

public enum TipoZona
{
    Camino,
    Bosque,
    Aldea
}

/// <summary>
/// Región circular: Aldea, Bosque o Camino.
/// </summary>
public class Zona : MonoBehaviour
{
    public TipoZona tipo = TipoZona.Camino;
    public float radio = 5f;

    public bool Contiene(Vector3 posicion)
    {
        return Vector2.Distance(transform.position, posicion) <= radio;
    }

    void OnDrawGizmosSelected()
    {
        switch (tipo)
        {
            case TipoZona.Aldea: Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.9f); break;
            case TipoZona.Bosque: Gizmos.color = new Color(0.15f, 0.45f, 0.15f, 0.9f); break;
            default: Gizmos.color = new Color(0.7f, 0.65f, 0.4f, 0.9f); break;
        }
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}

/// <summary>
/// Almacén de madera y zona segura. Va en el mismo objeto que la Zona Aldea.
/// </summary>
public class Aldea : MonoBehaviour
{
    public static Aldea Instancia { get; private set; }

    public float maderaEnAlmacen = 0f;
    public int aldeanosVivos = 0;
    Zona zona;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this);
            return;
        }

        Instancia = this;
        zona = GetComponent<Zona>();

        if (global::Simulate.Instancia != null)
            maderaEnAlmacen = global::Simulate.Instancia.aldeaMaderaInicial;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    public void DepositarMadera(float cantidad)
    {
        if (cantidad > 0f) maderaEnAlmacen += cantidad;
    }

    public bool EstaDentro(Vector3 posicion)
    {
        if (zona != null) return zona.Contiene(posicion);
        float radio = global::Simulate.Instancia != null ? global::Simulate.Instancia.aldeaRadio : 4f;
        return Vector2.Distance(transform.position, posicion) <= radio;
    }

    public Vector3 PuntoRefugio() => transform.position;

    public void ActualizarPoblacion(int vivos) => aldeanosVivos = Mathf.Max(0, vivos);
}

/// <summary>
/// Consulta de zonas. Prioridad: Aldea > Bosque > Camino.
/// </summary>
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

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

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
    public bool PosicionPermitidaParaLobo(Vector3 posicion) => !EstaEnAldea(posicion);

    public Zona ObtenerPrimeraZona(TipoZona tipo)
    {
        foreach (Zona zona in zonas)
            if (zona != null && zona.tipo == tipo) return zona;
        return null;
    }

    public bool IntentarPuntoAleatorio(TipoZona tipo, out Vector3 punto)
    {
        Zona zona = ObtenerPrimeraZona(tipo);
        if (zona == null)
        {
            punto = Vector3.zero;
            return false;
        }
        Vector2 offset = Random.insideUnitCircle * zona.radio * 0.9f;
        punto = zona.transform.position + new Vector3(offset.x, offset.y, 0f);
        return true;
    }
}
