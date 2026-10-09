using UnityEngine;

// Zona segura + almacén de madera compartido.
// Los lobos no pueden entrar aquí.
public class Aldea : MonoBehaviour
{
    public static Aldea Instancia { get; private set; }

    public float maderaEnAlmacen = 0f;
    public int aldeanosVivos = 0;
    Zona zona;

    void Awake()
    {
        // Solo una aldea activa
        if (Instancia != null && Instancia != this)
        {
            Destroy(this);
            return;
        }

        Instancia = this;
        zona = GetComponent<Zona>();

        // Si no tiene Zona, se la crea
        if (zona == null)
        {
            zona = gameObject.AddComponent<Zona>();
            zona.tipo = TipoZona.Aldea;
            zona.radio = ConfigSim.Actual != null ? ConfigSim.Actual.aldeaRadio : 4f;
        }

        if (ConfigSim.Actual != null)
            maderaEnAlmacen = ConfigSim.Actual.aldeaMaderaInicial;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // El aldeano deja aquí la madera que trajo
    public void DepositarMadera(float cantidad)
    {
        if (cantidad > 0f) maderaEnAlmacen += cantidad;
    }

    public bool EstaDentro(Vector3 posicion)
    {
        if (zona != null) return zona.Contiene(posicion);
        float radio = ConfigSim.Actual != null ? ConfigSim.Actual.aldeaRadio : 4f;
        return Vector2.Distance(transform.position, posicion) <= radio;
    }

    // Centro de la aldea (punto de refugio)
    public Vector3 PuntoRefugio() => transform.position;

    public void ActualizarPoblacion(int vivos) => aldeanosVivos = Mathf.Max(0, vivos);
}
