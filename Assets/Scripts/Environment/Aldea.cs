using UnityEngine;

/// <summary>
/// Zona segura + almacén de madera. Los lobos no pueden entrar.
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
        if (zona == null)
        {
            zona = gameObject.AddComponent<Zona>();
            zona.tipo = TipoZona.Aldea;
            zona.radio = global::Simulate.Instancia != null ? global::Simulate.Instancia.aldeaRadio : 4f;
        }

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
