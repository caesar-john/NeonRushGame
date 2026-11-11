using System.Collections.Generic;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private Transform player;
    [Tooltip("Prefab jalan. Pivot harus di tengah dan panjangnya harus sama dengan Tile Length.")]
    [SerializeField] private GameObject[] tilePrefabs;
    [SerializeField] private Transform tileParent;

    [Header("Ukuran Jalan")]
    [SerializeField] private float tileLength = 30f;
    [SerializeField] private int tilesAhead = 5;
    [SerializeField] private int tilesBehind = 2;

    private readonly List<GameObject> activeTiles = new List<GameObject>();
    private readonly Queue<GameObject> freeTiles = new Queue<GameObject>();
    private float nextTileStartZ;

    private void Start()
    {
        if (player == null && GameManager.Instance != null) player = GameManager.Instance.Player;
        if (player == null)
        {
            Debug.LogError("[TileSpawner] Player belum di-assign dan GameManager tidak menemukan tag Player.", this);
            enabled = false;
            return;
        }
        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogError("[TileSpawner] Daftar prefab tile masih kosong.", this);
            enabled = false;
            return;
        }
        if (tileParent == null) tileParent = transform;

        nextTileStartZ = player.position.z - tilesBehind * tileLength;
        FillAhead();
    }

    private void Update()
    {
        if (player == null) return;
        FillAhead();
        RecycleBehind();
    }

    private void FillAhead()
    {
        float targetZ = player.position.z + tilesAhead * tileLength;
        int guard = 0;

        while (nextTileStartZ <= targetZ && guard++ < 1000)
        {
            SpawnTile(nextTileStartZ);
            nextTileStartZ += tileLength;
        }
    }

    private void SpawnTile(float startZ)
    {
        GameObject tile = GetTile();
        if (tile == null) return;

        tile.transform.SetParent(tileParent, false);
        tile.transform.position = new Vector3(0f, 0f, startZ + tileLength * 0.5f);
        tile.transform.rotation = Quaternion.identity;
        tile.SetActive(true);
        activeTiles.Add(tile);
    }

    private GameObject GetTile()
    {
        while (freeTiles.Count > 0)
        {
            GameObject pooled = freeTiles.Dequeue();
            if (pooled != null) return pooled;
        }

        int index = UnityEngine.Random.Range(0, tilePrefabs.Length);
        return Instantiate(tilePrefabs[index], tileParent);
    }

    private void RecycleBehind()
    {
        float minZ = player.position.z - tilesBehind * tileLength;

        while (activeTiles.Count > 0)
        {
            GameObject oldest = activeTiles[0];
            if (oldest == null)
            {
                activeTiles.RemoveAt(0);
                continue;
            }

            float endZ = oldest.transform.position.z + tileLength * 0.5f;
            if (endZ >= minZ) break;

            activeTiles.RemoveAt(0);
            oldest.SetActive(false);
            freeTiles.Enqueue(oldest);
        }
    }
}
