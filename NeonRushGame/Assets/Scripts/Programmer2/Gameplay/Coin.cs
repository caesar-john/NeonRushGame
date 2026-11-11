using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 10;
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2.5f;

    private Vector3 basePosition;
    private bool baseCaptured;

    private void OnEnable()
    {
        basePosition = transform.position;
        baseCaptured = true;
    }

    private void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);

        if (!baseCaptured) return;
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(basePosition.x, basePosition.y + bob, basePosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || !IsPlayer(other)) return;
        Collect();
    }

    private static bool IsPlayer(Collider other)
    {
        GameManager gameManager = GameManager.Instance;
        Transform player = gameManager != null ? gameManager.Player : null;

        if (player != null)
            return other.transform == player || other.transform.IsChildOf(player);

        return other.CompareTag("Player");
    }

    public void Collect()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        GameManager.Instance.CollectCoin(value);
        AudioManager.PlaySfx(SfxId.Coin);

        if (PoolManager.Instance != null) PoolManager.Despawn(gameObject);
        else gameObject.SetActive(false);
    }
}
