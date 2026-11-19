using UnityEngine;

public class Hazard : MonoBehaviour
{
    [Tooltip("Matikan jika collision karakter ditangani sendiri oleh PlayerController.")]
    [SerializeField] private bool endGameOnTouch = true;

    [Tooltip("Jarak aman di atas rintangan. Pemain yang sudah melompat melewati puncak rintangan tidak dianggap menabrak.")]
    [SerializeField] private float jumpClearance = 0.25f;

    private Collider hazardCollider;
    private Collider HazardCollider => hazardCollider != null ? hazardCollider : (hazardCollider = GetComponent<Collider>());

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHit(collision.collider);
    }

    private void TryHit(Collider other)
    {
        if (other == null || !IsPlayer(other)) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        // Pemain yang sudah berada di atas puncak rintangan (sedang melompat)
        // tidak boleh dianggap menabrak, walaupun collider besarnya masih
        // bersinggungan secara horizontal.
        if (WasJumpedOver(other)) return;

        AudioManager.PlaySfx(SfxId.Crash);
        if (endGameOnTouch) GameManager.Instance.EndGame();
    }

    private bool WasJumpedOver(Collider other)
    {
        Collider own = HazardCollider;
        if (own == null) return false;

        float playerBottom = other.bounds.min.y;
        float hazardTop = own.bounds.max.y;

        return playerBottom >= hazardTop - jumpClearance;
    }

    private static bool IsPlayer(Collider other)
    {
        GameManager gameManager = GameManager.Instance;
        Transform player = gameManager != null ? gameManager.Player : null;

        if (player != null)
            return other.transform == player || other.transform.IsChildOf(player);

        return other.CompareTag("Player");
    }
}
