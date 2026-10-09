using UnityEngine;

/// <summary>
/// Región circular del mapa: Aldea, Bosque o Camino.
/// </summary>
public class Zona : MonoBehaviour
{
    public TipoZona tipo = TipoZona.Camino;
    public float radio = 5f;

    /// <summary>
    /// Indica si la posición está dentro de esta zona.
    /// </summary>
    public bool Contiene(Vector3 posicion)
    {
        return Vector2.Distance(transform.position, posicion) <= radio;
    }

    /// <summary>
    /// Distancia desde el borde (negativa = dentro).
    /// </summary>
    public float DistanciaAlBorde(Vector3 posicion)
    {
        return Vector2.Distance(transform.position, posicion) - radio;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = ColorPorTipo();
        Gizmos.DrawWireSphere(transform.position, radio);
    }

    Color ColorPorTipo()
    {
        switch (tipo)
        {
            case TipoZona.Aldea: return new Color(0.2f, 0.8f, 0.3f, 0.9f);
            case TipoZona.Bosque: return new Color(0.15f, 0.45f, 0.15f, 0.9f);
            default: return new Color(0.7f, 0.65f, 0.4f, 0.9f);
        }
    }
}
