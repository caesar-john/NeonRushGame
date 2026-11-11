using System;
using System.Collections.Generic;
using UnityEngine;

public enum CoinPattern
{
    Straight,
    Zigzag,
    Arc
}

public class CoinSpawner : MonoBehaviour
{
    [Serializable]
    public class PatternSettings
    {
        public CoinPattern pattern = CoinPattern.Straight;
        [Min(0f)] public float weight = 1f;
    }

    [Header("Referensi")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject coinPrefab;
    [Tooltip("Layer tempat prefab rintangan berada, agar koin tidak menimpa rintangan.")]
    [SerializeField] private LayerMask obstacleLayerMask;

    [Header("Jalur")]
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private float coinHeight = 1.1f;

    [Header("Pola")]
    [SerializeField]
    private PatternSettings[] patterns =
    {
        new PatternSettings { pattern = CoinPattern.Straight, weight = 1f },
        new PatternSettings { pattern = CoinPattern.Zigzag, weight = 1f },
        new PatternSettings { pattern = CoinPattern.Arc, weight = 1f }
    };

    [SerializeField] private int minCoins = 5;
    [SerializeField] private int maxCoins = 8;
    [SerializeField] private float coinSpacing = 2.5f;
    [SerializeField] private float arcHeight = 1.4f;

    [Header("Jarak Spawn")]
    [SerializeField] private float firstPatternDistance = 25f;
    [SerializeField] private float spawnAheadDistance = 65f;
    [SerializeField] private float minPatternGap = 10f;
    [SerializeField] private float maxPatternGap = 18f;
    [SerializeField] private float despawnBehindDistance = 15f;
    [SerializeField] private float obstacleCheckRadius = 0.45f;

    private readonly List<GameObject> activeCoins = new List<GameObject>();
    private float nextPatternZ;
    private bool initialized;

    private void Start()
    {
        if (player == null && GameManager.Instance != null) player = GameManager.Instance.Player;
        if (player == null)
        {
            Debug.LogError("[CoinSpawner] Player belum di-assign.", this);
            enabled = false;
            return;
        }
        if (coinPrefab == null)
        {
            Debug.LogError("[CoinSpawner] Prefab koin masih kosong.", this);
            enabled = false;
            return;
        }

        nextPatternZ = player.position.z + firstPatternDistance;
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

        while (nextPatternZ <= limit && guard++ < 100)
        {
            SpawnPattern(nextPatternZ);
        }
    }

    private void SpawnPattern(float startZ)
    {
        CoinPattern pattern = PickPattern();
        int count = UnityEngine.Random.Range(minCoins, maxCoins + 1);
        int lane = UnityEngine.Random.Range(0, 3);
        int direction = UnityEngine.Random.value < 0.5f ? -1 : 1;

        for (int i = 0; i < count; i++)
        {
            float z = startZ + i * coinSpacing;
            float x = (lane - 1) * laneWidth;
            float y = coinHeight;

            switch (pattern)
            {
                case CoinPattern.Zigzag:
                    if (i > 0 && i % 2 == 0)
                    {
                        if (lane + direction < 0 || lane + direction > 2) direction = -direction;
                        lane += direction;
                        x = (lane - 1) * laneWidth;
                    }
                    break;

                case CoinPattern.Arc:
                    float t = count > 1 ? (float)i / (count - 1) : 0f;
                    y = coinHeight + Mathf.Sin(t * Mathf.PI) * arcHeight;
                    break;
            }

            Vector3 position = new Vector3(x, y, z);

            if (Physics.CheckSphere(position, obstacleCheckRadius, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                continue;

            GameObject coin = PoolManager.Spawn(coinPrefab, position, Quaternion.identity, transform);
            if (coin != null) activeCoins.Add(coin);
        }

        float patternLength = (count - 1) * coinSpacing;
        nextPatternZ = startZ + patternLength + UnityEngine.Random.Range(minPatternGap, maxPatternGap);
    }

    private CoinPattern PickPattern()
    {
        if (patterns == null || patterns.Length == 0) return CoinPattern.Straight;

        float total = 0f;
        for (int i = 0; i < patterns.Length; i++)
            total += Mathf.Max(0f, patterns[i].weight);

        if (total <= 0f) return patterns[0].pattern;

        float roll = UnityEngine.Random.Range(0f, total);
        float sum = 0f;

        for (int i = 0; i < patterns.Length; i++)
        {
            sum += Mathf.Max(0f, patterns[i].weight);
            if (roll <= sum) return patterns[i].pattern;
        }

        return patterns[patterns.Length - 1].pattern;
    }

    private void RecycleBehind()
    {
        float minZ = player.position.z - despawnBehindDistance;

        for (int i = activeCoins.Count - 1; i >= 0; i--)
        {
            GameObject coin = activeCoins[i];

            if (coin == null || !coin.activeInHierarchy)
            {
                activeCoins.RemoveAt(i);
                continue;
            }

            if (coin.transform.position.z < minZ)
            {
                PoolManager.Despawn(coin);
                activeCoins.RemoveAt(i);
            }
        }
    }
}
