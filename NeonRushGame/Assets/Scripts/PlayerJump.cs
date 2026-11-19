using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    [SerializeField] private float jumpForce = 15;

    /// <summary>
    /// Kekuatan lompat. Bisa diatur dari script lain (mis. adapter scene)
    /// tanpa harus mengubah nilai di Inspector scene.
    /// </summary>
    public float JumpForce
    {
        get { return jumpForce; }
        set { jumpForce = value; }
    }

    // Nama state di Animator Controller (Base Layer).
    private const string RunningState = "Running";
    private const string JumpState = "Jump";

    private Rigidbody rb;
    private bool isGrounded = false;

    //animator
    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody tidak ditemukan pada Player!");
        }

        if (animator == null)
        {
            Debug.LogError("Animator tidak ditemukan pada player");
        }

        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            isGrounded = false;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            PlayState(JumpState);
        }

        // Animator Controller belum punya transisi antar-state, jadi perpindahan
        // state dilakukan langsung dari script. Setelah mendarat, kembali ke
        // Running supaya animasi tidak berhenti di frame terakhir lompat.
        if (isGrounded && IsCurrentState(JumpState))
        {
            PlayState(RunningState);
        }

        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private void FixedUpdate()
    {
        // Saat menapak, buang sisa kecepatan jatuh supaya karakter tidak
        // memantul kecil-kecil di permukaan jalan.
        if (isGrounded && rb != null && rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }
    }

    private void PlayState(string stateName)
    {
        if (animator == null) return;
        animator.Play(stateName, 0, 0f);
    }

    private bool IsCurrentState(string stateName)
    {
        return animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    private void OnCollisionStay(Collision collision)
    {
        // Hanya menganggap permukaan dari bawah sebagai tempat mendarat
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f && rb.linearVelocity.y <= 0.1f)
            {
                isGrounded = true;
                return;
            }
        }
    }
}
