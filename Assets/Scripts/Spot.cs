using UnityEngine;

// Punto del mapa: celda, casa, bosque, cofre, estacion, hierba, torre o combustible.
public class Spot : MonoBehaviour
{
    public float radius = 2f;
    public bool isOpen = true;
    public float amount = 0f;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
