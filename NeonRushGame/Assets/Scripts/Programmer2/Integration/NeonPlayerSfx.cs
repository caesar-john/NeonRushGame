using UnityEngine;

// Efek suara kejadian pemain tanpa mengubah skrip gerak milik Programmer 1.
// Lompat dideteksi dari lonjakan kecepatan vertikal Rigidbody, jadi tidak perlu
// menambah input atau memanggil method dari PlayerJump.
[AddComponentMenu("Neon Rush/Integration/Player Sfx")]
public class NeonPlayerSfx : MonoBehaviour
{
    [SerializeField] private float jumpVelocityThreshold = 2f;
    [SerializeField] private float groundedVelocity = 0.5f;

    private Rigidbody body;
    private float previousVerticalSpeed;
    private bool ready;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (body == null) return;

        float vertical = body.linearVelocity.y;

        if (ready && previousVerticalSpeed <= groundedVelocity && vertical > jumpVelocityThreshold)
            AudioManager.PlaySfx(SfxId.Jump);

        previousVerticalSpeed = vertical;
        ready = true;
    }
}
