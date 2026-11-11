using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [SerializeField] private Transform poolRoot;

    private readonly Dictionary<GameObject, Queue<GameObject>> pools =
        new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (poolRoot == null) poolRoot = transform;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;
        if (Instance == null) return Instantiate(prefab, position, rotation, parent);
        return Instance.SpawnInternal(prefab, position, rotation, parent);
    }

    public static void Despawn(GameObject go)
    {
        if (go == null) return;
        if (Instance == null)
        {
            Destroy(go);
            return;
        }
        Instance.DespawnInternal(go);
    }

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools.Add(prefab, queue);
        }

        GameObject go = null;
        while (queue.Count > 0 && go == null) go = queue.Dequeue();

        if (go == null)
        {
            go = Instantiate(prefab);
            go.SetActive(false);
            PooledObject marker = go.GetComponent<PooledObject>();
            if (marker == null) marker = go.AddComponent<PooledObject>();
            marker.SourcePrefab = prefab;
        }

        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    private void DespawnInternal(GameObject go)
    {
        PooledObject marker = go.GetComponent<PooledObject>();
        if (marker == null || marker.SourcePrefab == null)
        {
            Destroy(go);
            return;
        }

        go.SetActive(false);
        go.transform.SetParent(poolRoot, false);

        if (!pools.TryGetValue(marker.SourcePrefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools.Add(marker.SourcePrefab, queue);
        }
        queue.Enqueue(go);
    }
}
