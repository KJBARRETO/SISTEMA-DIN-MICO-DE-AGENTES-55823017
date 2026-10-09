using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orquestador central: todas las entidades se actualizan solo desde aquí.
/// </summary>
public class Simulate : MonoBehaviour
{
    public float secondsPerIteration = 1.0f;
    public bool ended = false;

    private float time = 0f;

    public List<Agent> agents = new List<Agent>();
    public Spawner spawner;

    void Start()
    {
        Agent[] foundAgents = FindObjectsByType<Agent>(FindObjectsSortMode.InstanceID);
        agents = new List<Agent>(foundAgents);

        spawner = FindFirstObjectByType<Spawner>();
    }

    void Update()
    {
        if (ended) return;

        time += Time.deltaTime;

        if (time >= secondsPerIteration)
        {
            time = 0f;
            RunTick();
        }
    }

    /// <summary>
    /// Un ciclo de simulación: llama Simulate() de cada entidad viva y del spawner.
    /// </summary>
    void RunTick()
    {
        foreach (Agent agent in agents)
        {
            if (agent != null && agent.isAlive)
            {
                agent.Simulate(secondsPerIteration);
            }
        }

        if (spawner != null) spawner.Simulate(secondsPerIteration);
    }

    public void End()
    {
        ended = true;
    }
}
