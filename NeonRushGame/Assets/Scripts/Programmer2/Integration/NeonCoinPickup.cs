using UnityEngine;

// Koin visual desainer dibuat bisa diambil tanpa mengandalkan trigger fisika.
// Posisi pemain dicek tiap frame, jadi koin tetap terambil walau collider
// pemain/koin berukuran tidak lazim atau trigger tidak sempat berbunyi.
[AddComponentMenu("Neon Rush/Integration/Coin Pickup")]
public class NeonCoinPickup : MonoBehaviour
{
    [SerializeField] private int coinValue = 1;
    [SerializeField] private float collectRadius = 5f;
    [SerializeField] private float boundsReach = 1.5f;
    [SerializeField] private float rotateSpeed = 100f;

    private static Transform cachedPlayer;

    private bool collected;
    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private bool hasStartPose;

    public int CoinValue
    {
        get { return coinValue; }
        set { coinValue = value; }
    }

    /// <summary>
    /// Atur jarak ambil koin dari luar script (mis. adapter scene).
    /// </summary>
    public void Configure(float radius, float extraBoundsReach = 1.5f)
    {
        if (radius > 0f) collectRadius = radius;
        if (extraBoundsReach > 0f) boundsReach = extraBoundsReach;
    }

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
        hasStartPose = true;
    }

    private void OnEnable()
    {
        collected = false;
        if (hasStartPose)
        {
            transform.localPosition = startLocalPosition;
            transform.localRotation = startLocalRotation;
        }
    }

    private void Update()
    {
        if (collected) return;

        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);

        Transform player = ResolvePlayer();
        if (player == null) return;

        if (IsInReach(player))
            Collect();
    }

    // Koin harus tetap terambil walau posisi Y pemain dan koin berbeda
    // (mis. koin digantung lebih tinggi atau pemain sedang melompat),
    // jadi jarak dihitung dengan sumbu Y yang ditekan.
    private bool IsInReach(Transform player)
    {
        Vector3 delta = player.position - transform.position;
        delta.y *= 0.35f;
        if (delta.sqrMagnitude <= collectRadius * collectRadius) return true;

        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider != null)
        {
            float sqrDistance = playerCollider.bounds.SqrDistance(transform.position);
            if (sqrDistance <= boundsReach * boundsReach) return true;
        }

        return false;
    }

    private void Collect()
    {
        collected = true;

        if (GameManager.Instance != null)
            GameManager.Instance.CollectCoin(coinValue);
        else
            Debug.LogWarning("[NeonCoinPickup] GameManager tidak ditemukan, koin tidak dihitung.", this);

        AudioManager.PlaySfx(SfxId.Coin);

        gameObject.SetActive(false);
    }

    private static Transform ResolvePlayer()
    {
        if (cachedPlayer != null) return cachedPlayer;

        GameManager manager = GameManager.Instance;
        if (manager != null && manager.Player != null)
            cachedPlayer = manager.Player;

        if (cachedPlayer == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) cachedPlayer = found.transform;
        }

        return cachedPlayer;
    }
}
