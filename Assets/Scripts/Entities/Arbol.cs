using UnityEngine;

public enum EstadoArbol
{
    Disponible,
    Agotado,
    Regenerando
}

/// <summary>
/// Recurso de madera (prefab Madera). El fondo Bosque es solo visual.
/// </summary>
public class Arbol : MonoBehaviour
{
    public float radius = 1f;
    public bool isOpen = true;
    public float amount = 20f;
    public EstadoArbol estado = EstadoArbol.Disponible;
    public float maderaMaxima = 20f;

    SpriteRenderer spriteRenderer;
    Color colorOriginal = Color.white;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) colorOriginal = spriteRenderer.color;
        AplicarConfig();
    }

    void Start()
    {
        AplicarConfig();
        ActualizarApariencia();
    }

    void AplicarConfig()
    {
        var cfg = global::Simulate.Instancia;
        if (cfg != null) maderaMaxima = cfg.arbolMaderaMaxima;
        if (amount <= 0f) amount = maderaMaxima;
        isOpen = amount > Umbral();
        estado = isOpen ? EstadoArbol.Disponible : EstadoArbol.Agotado;
    }

    float Umbral() => global::Simulate.Instancia != null ? global::Simulate.Instancia.arbolUmbralDisponible : 1f;
    float Tasa() => global::Simulate.Instancia != null ? global::Simulate.Instancia.arbolTasaRegeneracion : 0.4f;

    public void Simulate(float h)
    {
        if (estado == EstadoArbol.Disponible) return;

        amount = Mathf.Min(maderaMaxima, amount + Tasa() * h);
        if (amount >= Umbral())
        {
            estado = EstadoArbol.Disponible;
            isOpen = true;
        }
        else
        {
            estado = EstadoArbol.Regenerando;
            isOpen = false;
        }
        ActualizarApariencia();
    }

    public float Extraer(float cantidad)
    {
        if (cantidad <= 0f || amount <= 0f) return 0f;
        float extraido = Mathf.Min(cantidad, amount);
        amount -= extraido;
        if (amount < Umbral())
        {
            estado = amount <= 0f ? EstadoArbol.Agotado : EstadoArbol.Regenerando;
            isOpen = false;
        }
        ActualizarApariencia();
        return extraido;
    }

    public bool TieneMaderaDisponible() => isOpen && amount >= Umbral();

    void ActualizarApariencia()
    {
        if (spriteRenderer == null) return;
        switch (estado)
        {
            case EstadoArbol.Agotado:
                spriteRenderer.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, 0.35f);
                break;
            case EstadoArbol.Regenerando:
                spriteRenderer.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
                break;
            default:
                spriteRenderer.color = colorOriginal;
                break;
        }
    }
}
