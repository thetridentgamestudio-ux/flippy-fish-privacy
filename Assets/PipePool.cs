using UnityEngine;
using System.Collections.Generic;

/// Object pool for pipeGroups (pipeGroup shell + TopObstacle + BottomObstacle + ScoreTrigger).
/// No prefab needed — pools entire assembled hierarchies and reinitializes them on reuse.
/// Pool fills naturally as pipes scroll off screen; no prewarm needed.
public static class PipePool
{
    private static readonly Queue<GameObject> _pool = new Queue<GameObject>();

    // Maximum pooled pipes — 8 covers the max number ever on screen simultaneously
    private const int MAX_POOL_SIZE = 8;

    /// Returns a recycled pipeGroup (already SetActive(true)), or null if pool is empty.
    public static GameObject GetPipeGroup()
    {
        while (_pool.Count > 0)
        {
            GameObject go = _pool.Dequeue();
            if (go != null)
            {
                go.SetActive(true);
                return go;
            }
        }
        return null;
    }

    /// Returns a pipeGroup to the pool. Deactivates it but keeps its children intact for reuse.
    public static void ReturnPipeGroup(GameObject pipeGroup)
    {
        if (pipeGroup == null) return;
        if (_pool.Count >= MAX_POOL_SIZE)
        {
            Object.Destroy(pipeGroup);
            return;
        }
        pipeGroup.SetActive(false);
        _pool.Enqueue(pipeGroup);
    }

    /// Clears the pool (call on scene reload / quit).
    public static void Clear()
    {
        while (_pool.Count > 0)
        {
            var go = _pool.Dequeue();
            if (go != null) Object.Destroy(go);
        }
    }

    public static int PooledCount => _pool.Count;
}
