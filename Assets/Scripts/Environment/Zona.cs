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
