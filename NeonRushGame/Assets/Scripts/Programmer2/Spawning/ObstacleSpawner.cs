using System;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Serializable]
    public class ObstacleType
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }

    [Header("Referensi")]
    [SerializeField] private Transform player;
    [SerializeField] private ObstacleType[] obstacleTypes = new ObstacleType[0];

    [Header("Jalur")]
    [SerializeField] private float laneWidth = 3f;
    [Range(1, 2)] [SerializeField] private int maxBlockedLanes = 2;

    [Header("Jarak Spawn")]
    [SerializeField] private float firstObstacleDistance = 35f;
    [SerializeField] private float spawnAheadDistance = 70f;
    [SerializeField] private float minGap = 16f;
    [SerializeField] private float maxGap = 30f;
    [SerializeField] private float despawnBehindDistance = 20f;

    private readonly List<GameObject> activeObstacles = new List<GameObject>();
    private readonly int[] laneBuffer = { 0, 1, 2 };
    private float nextSpawnZ;
    private bool initialized;

    private void Start()
    {
        if (player == null && GameManager.Instance != null) player = GameManager.Instance.Player;
        if (player == null)
        {
            Debug.LogError("[ObstacleSpawner] Player belum di-assign.", this);
            enabled = false;
            return;
        }
        if (obstacleTypes == null || obstacleTypes.Length == 0)
        {
            Debug.LogError("[ObstacleSpawner] Daftar prefab rintangan masih kosong.", this);
            enabled = false;
            return;
        }

        nextSpawnZ = player.position.z + firstObstacleDistance;
        initialized = true;
        FillAhead();
    }

    private void Update()
    {
        if (!initialized || player == null) return;

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            RecycleBehind();
            return;
        }

        FillAhead();
        RecycleBehind();
    }

    private void FillAhead()
    {
        float limit = player.position.z + spawnAheadDistance;
        int guard = 0;

        while (nextSpawnZ <= limit && guard++ < 100)
        {
            SpawnGroup(nextSpawnZ);
            nextSpawnZ += UnityEngine.Random.Range(minGap, maxGap);
        }
    }

    private void SpawnGroup(float z)
    {
        int blocked = UnityEngine.Random.Range(1, maxBlockedLanes + 1);

        laneBuffer[0] = 0;
        laneBuffer[1] = 1;
        laneBuffer[2] = 2;
        Shuffle(laneBuffer);

        for (int i = 0; i < blocked; i++)
        {
            GameObject prefab = PickPrefab();
            if (prefab == null) continue;

            float x = (laneBuffer[i] - 1) * laneWidth;
            GameObject obstacle = PoolManager.Spawn(
                prefab,
                new Vector3(x, 0f, z),
                Quaternion.identity,
                transform);

            if (obstacle != null) activeObstacles.Add(obstacle);
        }
    }

    private GameObject PickPrefab()
    {
        float total = 0f;
        for (int i = 0; i < obstacleTypes.Length; i++)
            total += Mathf.Max(0f, obstacleTypes[i].weight);

        if (total <= 0f) return obstacleTypes[0].prefab;

        float roll = UnityEngine.Random.Range(0f, total);
        float sum = 0f;

        for (int i = 0; i < obstacleTypes.Length; i++)
        {
            sum += Mathf.Max(0f, obstacleTypes[i].weight);
            if (roll <= sum) return obstacleTypes[i].prefab;
        }

        return obstacleTypes[obstacleTypes.Length - 1].prefab;
    }

    private void RecycleBehind()
    {
        float minZ = player.position.z - despawnBehindDistance;

        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject go = activeObstacles[i];

            if (go == null || !go.activeInHierarchy)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }

            if (go.transform.position.z < minZ)
            {
                PoolManager.Despawn(go);
                activeObstacles.RemoveAt(i);
            }
        }
    }

    private static void Shuffle(int[] values)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int temp = values[i];
            values[i] = values[j];
            values[j] = temp;
        }
    }
}
