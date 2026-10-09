using UnityEngine;

// Zona segura + almacén de madera compartido.
// Los lobos no pueden entrar aquí.
public class Aldea : MonoBehaviour
{
    public static Aldea Instancia { get; private set; }

    [Header("Almacén de madera")]
    public float maderaEnAlmacen = 0f;

    public float capacidadMaximaAlmacen = 10000f;

    [Header("Recolección de madera")]
    public float maderaPorAldeano = 50f;

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
            zona.radio = ConfigSim.Actual != null
                ? ConfigSim.Actual.aldeaRadio
                : 4f;
        }

        if (ConfigSim.Actual != null)
        {
            maderaEnAlmacen = ConfigSim.Actual.aldeaMaderaInicial;
        }

        // Evitar que la madera inicial supere la capacidad
        maderaEnAlmacen = Mathf.Clamp(
            maderaEnAlmacen,
            0f,
            capacidadMaximaAlmacen
        );
    }

    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }

    // El aldeano deposita la madera que trajo.
    // El almacén nunca supera su capacidad máxima.
    public void DepositarMadera(float cantidad)
    {
        if (cantidad <= 0f)
            return;

        float espacioDisponible =
            capacidadMaximaAlmacen - maderaEnAlmacen;

        float maderaAceptada =
            Mathf.Min(cantidad, espacioDisponible);

        if (maderaAceptada > 0f)
        {
            maderaEnAlmacen += maderaAceptada;
        }
    }

    // Cantidad de madera que puede recoger cada aldeano.
    public float ObtenerMaderaPorAldeano()
    {
        return Mathf.Max(0f, maderaPorAldeano);
    }

    // Espacio disponible en el almacén.
    public float ObtenerEspacioDisponible()
    {
        return Mathf.Max(
            0f,
            capacidadMaximaAlmacen - maderaEnAlmacen
        );
    }

    public bool EstaDentro(Vector3 posicion)
    {
        if (zona != null)
            return zona.Contiene(posicion);

        float radio = ConfigSim.Actual != null
            ? ConfigSim.Actual.aldeaRadio
            : 4f;

        return Vector2.Distance(transform.position, posicion) <= radio;
    }

    // Centro de la aldea (punto de refugio)
    public Vector3 PuntoRefugio()
    {
        return transform.position;
    }

    public void ActualizarPoblacion(int vivos)
    {
        aldeanosVivos = Mathf.Max(0, vivos);
    }
}