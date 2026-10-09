using UnityEngine;

public enum EstadoArbol
{
    Disponible,
    Agotado
}

// Punto de madera. Se queda donde lo pongas;
// cuando el aldeano la termina de juntar, desaparece.
public class Arbol : MonoBehaviour
{
    public float radius = 1f;
    public bool isOpen = true;
    public float amount = 20f;      // cuánta madera queda
    public EstadoArbol estado = EstadoArbol.Disponible;
    public float maderaMaxima = 20f;

    void Awake()
    {
        AplicarConfig();
    }

    void Start()
    {
        AplicarConfig();
    }

    void AplicarConfig()
    {
        var cfg = ConfigSim.Actual;
        if (cfg != null) maderaMaxima = cfg.arbolMaderaMaxima;
        if (amount <= 0f) amount = maderaMaxima;
        isOpen = amount > 0f;
        estado = isOpen ? EstadoArbol.Disponible : EstadoArbol.Agotado;
    }

    // No regenera: la madera se consume y listo
    public void Simulate(float h) { }

    // Saca madera. Si llega a 0, el objeto se destruye
    public float Extraer(float cantidad)
    {
        if (cantidad <= 0f || amount <= 0f || !isOpen) return 0f;

        float extraido = Mathf.Min(cantidad, amount);
        amount -= extraido;

        if (amount <= 0.01f)
        {
            amount = 0f;
            isOpen = false;
            estado = EstadoArbol.Agotado;
            Destroy(gameObject); // desaparece del mapa
        }

        return extraido;
    }

    public bool TieneMaderaDisponible() => isOpen && amount > 0.01f && this != null;
}
