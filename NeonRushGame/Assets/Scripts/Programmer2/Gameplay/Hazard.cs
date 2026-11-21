using UnityEngine;

public class Hazard : MonoBehaviour
{
    [Tooltip("Matikan jika collision karakter ditangani sendiri oleh PlayerController.")]
    [SerializeField] private bool endGameOnTouch = true;

    [Tooltip("Jarak aman di atas rintangan. Pemain yang sudah melompat melewati puncak rintangan tidak dianggap menabrak.")]
    [SerializeField] private float jumpClearance = 0.25f;

    private Collider hazardCollider;
    private Collider HazardCollider => hazardCollider != null ? hazardCollider : (hazardCollider = GetComponent<Collider>());

    private Animator playerAnimator;
    private bool isProcessingHit;

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

        Debug.Log("HAZARD TERDETEKSI: " + other.name);

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        // Abaikan tabrakan jika pemain berhasil melompati rintangan.
        if (WasJumpedOver(other)) return;

        // Mencegah tabrakan diproses berulang kali.
        if (isProcessingHit) return;

        isProcessingHit = true;

        // Putar suara tabrakan.
        AudioManager.PlaySfx(SfxId.Crash);

        // Ambil Animator dari child Player.
        playerAnimator = other.GetComponentInChildren<Animator>();

        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();

        if (movement != null)
        {
            movement.StopMovement();
        }

        // Jalankan animasi Falling.
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Hit");
        }

        // Game Over akan dipanggil setelah animasi selesai.
        StartCoroutine(EndGameAfterFalling());

        
    }

    private System.Collections.IEnumerator EndGameAfterFalling()
    {
        if (playerAnimator != null)
        {
            yield return new WaitUntil(() =>
                playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Falling")
                && playerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
            );
        }

        if (endGameOnTouch && GameManager.Instance != null)
        {
            GameManager.Instance.EndGame();
        }
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
