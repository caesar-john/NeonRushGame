using UnityEngine;

// Koin visual desainer dibuat bisa diambil tanpa mengandalkan trigger fisika.
// Posisi pemain dicek tiap frame, jadi koin tetap terambil walau collider
// pemain/koin berukuran tidak lazim atau trigger tidak sempat berbunyi.
[AddComponentMenu("Neon Rush/Integration/Coin Pickup")]
public class NeonCoinPickup : MonoBehaviour
{
    [SerializeField] private int coinValue = 1;
    [SerializeField] private float collectRadius = 2.5f;
    [SerializeField] private float rotateSpeed = 100f;

    private static Transform cachedPlayer;

    private bool collected;
    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private bool hasStartPose;

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

        float radius = collectRadius;
        if ((player.position - transform.position).sqrMagnitude <= radius * radius)
            Collect();
    }

    private void Collect()
    {
        collected = true;

        if (GameManager.Instance != null)
            GameManager.Instance.CollectCoin(coinValue);

        AudioManager.PlaySfx(SfxId.Coin);

        gameObject.SetActive(false);
    }

    private static Transform ResolvePlayer()
    {
        if (cachedPlayer != null) return cachedPlayer;

        GameManager manager = GameManager.Instance;
        if (manager != null && manager.Player != null)
            cachedPlayer = manager.Player;

        return cachedPlayer;
    }
}
