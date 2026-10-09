using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
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
            Simulate();
        }
    }

    void Simulate()
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
