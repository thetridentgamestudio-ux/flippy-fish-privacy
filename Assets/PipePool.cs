using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Object pool for pipes to reduce GC pressure during high-spawn periods
/// Spawns ~1 pipe per 1-1.5 seconds = ~40-60 pipes per minute during long sessions
/// </summary>
public class PipePool : MonoBehaviour
{
    private static PipePool instance;
    private Queue<GameObject> pooledPipes = new Queue<GameObject>();
    private int initialPoolSize = 20; // pre-warm with 20 pipes
    private GameObject prefab;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public static void Initialize(GameObject pipePrefab, int poolSize = 20)
    {
        if (instance == null)
        {
            GameObject poolObj = new GameObject("PipePool");
            instance = poolObj.AddComponent<PipePool>();
            DontDestroyOnLoad(poolObj);
        }
        instance.prefab = pipePrefab;
        instance.initialPoolSize = poolSize;
        instance.WarmPool();
    }

    void WarmPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject pipe = Instantiate(prefab);
            pipe.SetActive(false);
            pooledPipes.Enqueue(pipe);
        }
        Debug.Log($"PipePool: Warmed with {initialPoolSize} pipes");
    }

    public static GameObject GetPipe()
    {
        if (instance.pooledPipes.Count > 0)
        {
            GameObject pipe = instance.pooledPipes.Dequeue();
            pipe.SetActive(true);
            return pipe;
        }
        else
        {
            // Pool exhausted — create new (should rarely happen)
            GameObject pipe = Instantiate(instance.prefab);
            return pipe;
        }
    }

    public static void ReturnPipe(GameObject pipe)
    {
        pipe.SetActive(false);
        instance.pooledPipes.Enqueue(pipe);
    }
}