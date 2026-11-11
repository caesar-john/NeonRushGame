using UnityEngine;

public class Hazard : MonoBehaviour
{
    [Tooltip("Matikan jika collision karakter ditangani sendiri oleh PlayerController.")]
    [SerializeField] private bool endGameOnTouch = true;

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

        AudioManager.PlaySfx(SfxId.Crash);
        if (endGameOnTouch) GameManager.Instance.EndGame();
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
