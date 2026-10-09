using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject prefab;
    public float spawnInterval = 2f;
    public int maxCount = 20;

    [Header("Spawn Area (Rectangular)")]
    public Vector2 areaSize = new Vector2(20, 20);

    private float time = 0f;

    public void Simulate(float h)
    {
        if (prefab == null) return;

        time += h;

        if (time >= spawnInterval)
        {
            time = 0f;

            if (CountSpawned() < maxCount)
            {
                Spawn();
            }
        }
    }

    void Spawn()
    {
        Vector2 spawnPos = new Vector2(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            Random.Range(-areaSize.y / 2f, areaSize.y / 2f)
        );

        spawnPos += (Vector2)transform.position;

        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    int CountSpawned()
    {
        int count = 0;

        Agent[] agents = FindObjectsByType<Agent>(FindObjectsSortMode.InstanceID);
        foreach (Agent agent in agents)
        {
            if (agent != null && agent.name.StartsWith(prefab.name))
            {
                count++;
            }
        }

        Spot[] spots = FindObjectsByType<Spot>(FindObjectsSortMode.InstanceID);
        foreach (Spot spot in spots)
        {
            if (spot != null && spot.name.StartsWith(prefab.name))
            {
                count++;
            }
        }

        return count;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 1));
    }
}
