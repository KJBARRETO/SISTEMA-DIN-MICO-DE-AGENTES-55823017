using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Consulta qué zona corresponde a una posición.
/// Prioridad: Aldea > Bosque > Camino (si hay solapamiento).
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
        if (Instancia == this)
            Instancia = null;
    }

    /// <summary>
    /// Vuelve a buscar todas las zonas de la escena.
    /// </summary>
    public void RefrescarZonas()
    {
        Zona[] encontradas = FindObjectsByType<Zona>(FindObjectsSortMode.InstanceID);
        zonas = new List<Zona>(encontradas);
    }

    /// <summary>
    /// Devuelve la zona que contiene la posición, o null si no hay ninguna.
    /// </summary>
    public Zona ObtenerZona(Vector3 posicion)
    {
        Zona aldea = null;
        Zona bosque = null;
        Zona camino = null;

        foreach (Zona zona in zonas)
        {
            if (zona == null || !zona.Contiene(posicion))
                continue;

            switch (zona.tipo)
            {
                case TipoZona.Aldea:
                    aldea = zona;
                    break;
                case TipoZona.Bosque:
                    bosque = zona;
                    break;
                case TipoZona.Camino:
                    camino = zona;
                    break;
            }
        }

        if (aldea != null) return aldea;
        if (bosque != null) return bosque;
        return camino;
    }

    public TipoZona ObtenerTipoZona(Vector3 posicion)
    {
        Zona zona = ObtenerZona(posicion);
        return zona != null ? zona.tipo : TipoZona.Camino;
    }

    public bool EstaEnAldea(Vector3 posicion)
    {
        return ObtenerTipoZona(posicion) == TipoZona.Aldea;
    }

    public bool EstaEnBosque(Vector3 posicion)
    {
        return ObtenerTipoZona(posicion) == TipoZona.Bosque;
    }

    /// <summary>
    /// Los lobos no pueden entrar a la aldea.
    /// </summary>
    public bool PosicionPermitidaParaLobo(Vector3 posicion)
    {
        return !EstaEnAldea(posicion);
    }

    /// <summary>
    /// Primera zona del tipo indicado, o null.
    /// </summary>
    public Zona ObtenerPrimeraZona(TipoZona tipo)
    {
        foreach (Zona zona in zonas)
        {
            if (zona != null && zona.tipo == tipo)
                return zona;
        }

        return null;
    }

    /// <summary>
    /// Punto aleatorio dentro de una zona del tipo dado.
    /// </summary>
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
